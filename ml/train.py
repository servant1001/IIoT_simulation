"""Train reproducible Phase 9 fault classifiers from a Phase 8 Dataset CSV."""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

import joblib
import matplotlib.pyplot as plt
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.base import BaseEstimator, ClassifierMixin
from sklearn.ensemble import RandomForestClassifier
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.impute import SimpleImputer
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import LabelEncoder
from sklearn.metrics import (
    accuracy_score,
    classification_report,
    confusion_matrix,
    precision_recall_fscore_support,
)
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder
from sklearn.svm import SVC

TARGET_COLUMN = "actual_fault_type"
GROUP_COLUMN = "experiment_run_id"
NUMERIC_COLUMNS = ["response_time_ms", "retry_count"]
CATEGORICAL_COLUMNS = ["protocol", "status", "observed_fault_type", "error_code"]
TEXT_COLUMN = "error_message"
REQUIRED_COLUMNS = {
    TARGET_COLUMN,
    GROUP_COLUMN,
    *NUMERIC_COLUMNS,
    *CATEGORICAL_COLUMNS,
    TEXT_COLUMN,
}


@dataclass(frozen=True)
class Split:
    train: pd.DataFrame
    validation: pd.DataFrame
    test: pd.DataFrame


class XGBoostFaultClassifier(BaseEstimator, ClassifierMixin):
    """Make XGBoost compatible with the string fault labels used by the Dataset."""

    def __init__(self, random_state: int = 42):
        self.random_state = random_state

    def fit(self, features: object, labels: pd.Series) -> "XGBoostFaultClassifier":
        from xgboost import XGBClassifier

        self.label_encoder_ = LabelEncoder()
        encoded_labels = self.label_encoder_.fit_transform(labels)
        self.classes_ = self.label_encoder_.classes_
        self.model_ = XGBClassifier(
            n_estimators=300,
            max_depth=6,
            learning_rate=0.05,
            objective="multi:softprob",
            eval_metric="mlogloss",
            num_class=len(self.classes_),
            random_state=self.random_state,
            n_jobs=1,
        )
        self.model_.fit(features, encoded_labels)
        return self

    def predict(self, features: object) -> pd.Series:
        encoded_prediction = self.model_.predict(features).astype(int)
        return self.label_encoder_.inverse_transform(encoded_prediction)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path, help="Phase 8 Dataset CSV path")
    parser.add_argument("--output", type=Path, default=Path("ml/outputs"), help="Directory for models and metrics")
    parser.add_argument("--random-state", type=int, default=42)
    parser.add_argument("--minimum-runs-per-class", type=int, default=100)
    parser.add_argument(
        "--allow-small-dataset",
        action="store_true",
        help="Allow an exploratory run below the requested minimum. Outputs are marked exploratory.",
    )
    return parser.parse_args()


def load_dataset(path: Path) -> pd.DataFrame:
    if not path.is_file():
        raise ValueError(f"Dataset file was not found: {path}")

    dataset = pd.read_csv(path, dtype={"error_code": "string", TEXT_COLUMN: "string"}, keep_default_na=False)
    missing = REQUIRED_COLUMNS.difference(dataset.columns)
    if missing:
        raise ValueError(f"Dataset is missing required columns: {', '.join(sorted(missing))}")

    dataset = dataset.dropna(subset=[TARGET_COLUMN, GROUP_COLUMN]).copy()
    dataset = dataset[(dataset[TARGET_COLUMN] != "") & (dataset[GROUP_COLUMN] != "")].copy()
    dataset[TARGET_COLUMN] = dataset[TARGET_COLUMN].astype(str)
    dataset[GROUP_COLUMN] = dataset[GROUP_COLUMN].astype(str)
    dataset[TEXT_COLUMN] = dataset[TEXT_COLUMN].fillna("")
    return dataset


def validate_ground_truth(dataset: pd.DataFrame, minimum_runs_per_class: int, allow_small_dataset: bool) -> pd.Series:
    run_labels = dataset.groupby(GROUP_COLUMN)[TARGET_COLUMN].nunique()
    inconsistent = run_labels[run_labels > 1]
    if not inconsistent.empty:
        raise ValueError("Each experiment_run_id must have exactly one actual_fault_type.")

    run_counts = dataset.groupby(TARGET_COLUMN)[GROUP_COLUMN].nunique().sort_index()
    if (run_counts < 3).any():
        failed = ", ".join(f"{label}={count}" for label, count in run_counts[run_counts < 3].items())
        raise ValueError(f"At least 3 distinct runs per class are required for train/validation/test: {failed}")
    if not allow_small_dataset and (run_counts < minimum_runs_per_class).any():
        failed = ", ".join(f"{label}={count}" for label, count in run_counts[run_counts < minimum_runs_per_class].items())
        raise ValueError(
            f"Dataset is below the required {minimum_runs_per_class} runs per class: {failed}. "
            "Collect more runs or use --allow-small-dataset only for exploratory verification."
        )
    return run_counts


def split_by_run(dataset: pd.DataFrame, random_state: int) -> Split:
    """Allocate whole runs per class, preventing records from the same run leaking across splits."""
    groups = dataset[[GROUP_COLUMN, TARGET_COLUMN]].drop_duplicates().sort_values([TARGET_COLUMN, GROUP_COLUMN])
    train_groups: list[str] = []
    validation_groups: list[str] = []
    test_groups: list[str] = []

    for index, (_, label_groups) in enumerate(groups.groupby(TARGET_COLUMN, sort=True)):
        shuffled = label_groups.sample(frac=1, random_state=random_state + index)[GROUP_COLUMN].tolist()
        test_groups.append(shuffled[0])
        validation_groups.append(shuffled[1])
        train_groups.extend(shuffled[2:])

    def subset(group_ids: Iterable[str]) -> pd.DataFrame:
        return dataset[dataset[GROUP_COLUMN].isin(group_ids)].copy()

    split = Split(subset(train_groups), subset(validation_groups), subset(test_groups))
    if split.train.empty or split.validation.empty or split.test.empty:
        raise ValueError("Could not create non-empty train, validation and test partitions.")
    return split


def make_preprocessor() -> ColumnTransformer:
    numeric = Pipeline([("imputer", SimpleImputer(strategy="median"))])
    categorical = Pipeline(
        [
            ("imputer", SimpleImputer(strategy="most_frequent")),
            ("one_hot", OneHotEncoder(handle_unknown="ignore")),
        ]
    )
    return ColumnTransformer(
        [
            ("numeric", numeric, NUMERIC_COLUMNS),
            ("categorical", categorical, CATEGORICAL_COLUMNS),
            ("error_message", TfidfVectorizer(ngram_range=(1, 2), min_df=1), TEXT_COLUMN),
        ]
    )


def build_models(random_state: int) -> dict[str, object]:
    models: dict[str, object] = {
        "logistic_regression": LogisticRegression(
            max_iter=3000, class_weight="balanced", random_state=random_state, solver="liblinear"
        ),
        "random_forest": RandomForestClassifier(
            n_estimators=400, class_weight="balanced", random_state=random_state, n_jobs=-1
        ),
        "svm": SVC(class_weight="balanced", random_state=random_state),
    }
    try:
        import xgboost  # noqa: F401

        models["xgboost"] = XGBoostFaultClassifier(random_state=random_state)
    except ImportError:
        pass
    return models


def score(model: Pipeline, features: pd.DataFrame, labels: pd.Series) -> dict[str, object]:
    prediction = model.predict(features)
    precision, recall, f1, _ = precision_recall_fscore_support(labels, prediction, average="weighted", zero_division=0)
    return {
        "accuracy": accuracy_score(labels, prediction),
        "precision_weighted": precision,
        "recall_weighted": recall,
        "f1_weighted": f1,
        "classification_report": classification_report(labels, prediction, output_dict=True, zero_division=0),
        "predictions": prediction,
    }


def save_confusion_matrix(labels: pd.Series, predictions: Iterable[str], title: str, path: Path) -> None:
    classes = sorted(labels.unique())
    matrix = confusion_matrix(labels, predictions, labels=classes)
    figure, axis = plt.subplots(figsize=(max(6, len(classes)), max(5, len(classes))))
    image = axis.imshow(matrix, interpolation="nearest", cmap="Blues")
    figure.colorbar(image, ax=axis)
    axis.set(title=title, xlabel="Predicted fault type", ylabel="Actual fault type")
    axis.set_xticks(range(len(classes)), classes, rotation=45, ha="right")
    axis.set_yticks(range(len(classes)), classes)
    for row in range(matrix.shape[0]):
        for column in range(matrix.shape[1]):
            axis.text(column, row, str(matrix[row, column]), ha="center", va="center")
    figure.tight_layout()
    figure.savefig(path, dpi=160)
    plt.close(figure)


def main() -> int:
    arguments = parse_args()
    dataset = load_dataset(arguments.input)
    run_counts = validate_ground_truth(dataset, arguments.minimum_runs_per_class, arguments.allow_small_dataset)
    split = split_by_run(dataset, arguments.random_state)
    arguments.output.mkdir(parents=True, exist_ok=True)

    feature_columns = NUMERIC_COLUMNS + CATEGORICAL_COLUMNS + [TEXT_COLUMN]
    leaderboard: list[dict[str, object]] = []
    metrics: dict[str, object] = {
        "dataset": {
            "input": str(arguments.input),
            "record_count": len(dataset),
            "run_count_by_actual_fault_type": run_counts.to_dict(),
            "exploratory": arguments.allow_small_dataset,
        },
        "split": {"train_records": len(split.train), "validation_records": len(split.validation), "test_records": len(split.test)},
        "models": {},
    }

    for name, classifier in build_models(arguments.random_state).items():
        pipeline = Pipeline([("preprocess", make_preprocessor()), ("classifier", classifier)])
        pipeline.fit(split.train[feature_columns], split.train[TARGET_COLUMN])
        validation = score(pipeline, split.validation[feature_columns], split.validation[TARGET_COLUMN])
        test = score(pipeline, split.test[feature_columns], split.test[TARGET_COLUMN])
        save_confusion_matrix(split.test[TARGET_COLUMN], test.pop("predictions"), f"{name} test confusion matrix", arguments.output / f"{name}_confusion_matrix.png")
        validation.pop("predictions")
        metrics["models"][name] = {"validation": validation, "test": test}
        leaderboard.append(
            {
                "model": name,
                "validation_f1_weighted": validation["f1_weighted"],
                "test_accuracy": test["accuracy"],
                "test_precision_weighted": test["precision_weighted"],
                "test_recall_weighted": test["recall_weighted"],
                "test_f1_weighted": test["f1_weighted"],
            }
        )
        joblib.dump(pipeline, arguments.output / f"{name}.joblib")

    pd.DataFrame(leaderboard).sort_values("validation_f1_weighted", ascending=False).to_csv(arguments.output / "leaderboard.csv", index=False)
    (arguments.output / "metrics.json").write_text(json.dumps(metrics, indent=2, default=str), encoding="utf-8")
    print(f"Trained {len(leaderboard)} models. Results: {arguments.output.resolve()}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValueError as error:
        print(f"Dataset validation failed: {error}", file=sys.stderr)
        raise SystemExit(2)
