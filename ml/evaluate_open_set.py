"""Evaluate a fixed classifier with confidence and adapter-unknown review guards."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import joblib
import numpy as np
import pandas as pd
from sklearn.metrics import accuracy_score, classification_report

from train import (
    CATEGORICAL_COLUMNS,
    NUMERIC_COLUMNS,
    TARGET_COLUMN,
    TEXT_COLUMN,
    load_dataset,
    split_by_run,
)


UNKNOWN_LABEL = "Unknown"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--calibration-input", required=True, type=Path, help="Known-class training snapshot used only to recreate its validation split.")
    parser.add_argument("--input", required=True, type=Path, help="Independent evaluation Dataset CSV.")
    parser.add_argument("--model", required=True, type=Path, help="Fixed joblib model path; this command never retrains it.")
    parser.add_argument("--output", required=True, type=Path, help="Directory for predictions and metrics.")
    parser.add_argument("--known-coverage", type=float, default=0.95, help="Minimum validation share retained as known by the confidence guard.")
    parser.add_argument("--random-state", type=int, default=42, help="Run-aware split seed used when the model was trained.")
    return parser.parse_args()


def confidence_scores(pipeline: object, features: pd.DataFrame) -> np.ndarray:
    preprocess = pipeline.named_steps["preprocess"]
    classifier = pipeline.named_steps["classifier"]
    transformed = preprocess.transform(features)
    if hasattr(classifier, "predict_proba"):
        probabilities = classifier.predict_proba(transformed)
        return np.max(probabilities, axis=1)

    decision = np.asarray(classifier.decision_function(transformed))
    if decision.ndim == 1:
        return np.abs(decision)
    ordered = np.sort(decision, axis=1)
    return ordered[:, -1] - ordered[:, -2]


def save_confusion(output: pd.DataFrame, prediction_column: str, path: Path) -> None:
    confusion = pd.crosstab(
        output[TARGET_COLUMN],
        output[prediction_column],
        rownames=["actual_fault_type"],
        colnames=[prediction_column],
    )
    confusion.to_csv(path)


def main() -> int:
    arguments = parse_args()
    if not 0 < arguments.known_coverage < 1:
        raise ValueError("known-coverage must be greater than 0 and less than 1.")
    if not arguments.model.is_file():
        raise ValueError(f"Model file was not found: {arguments.model}")

    feature_columns = NUMERIC_COLUMNS + CATEGORICAL_COLUMNS + [TEXT_COLUMN]
    calibration_dataset = load_dataset(arguments.calibration_input)
    evaluation_dataset = load_dataset(arguments.input)
    pipeline = joblib.load(arguments.model)
    known_classes = set(getattr(pipeline, "classes_", []))

    calibration_split = split_by_run(calibration_dataset, arguments.random_state)
    calibration_confidence = confidence_scores(pipeline, calibration_split.validation[feature_columns])
    threshold = float(np.quantile(calibration_confidence, 1 - arguments.known_coverage))

    closed_prediction = pipeline.predict(evaluation_dataset[feature_columns])
    confidence = confidence_scores(pipeline, evaluation_dataset[feature_columns])
    adapter_unknown = evaluation_dataset["observed_fault_type"].astype(str).eq(UNKNOWN_LABEL).to_numpy()
    low_confidence = confidence < threshold
    review_required = adapter_unknown | low_confidence
    unknown_reason = np.where(adapter_unknown, "adapter_unknown", np.where(low_confidence, "low_confidence", ""))
    open_prediction = np.where(review_required, UNKNOWN_LABEL, closed_prediction)

    output = evaluation_dataset.copy()
    output["closed_set_prediction"] = closed_prediction
    output["confidence"] = confidence
    output["review_required"] = review_required
    output["unknown_reason"] = unknown_reason
    output["open_set_prediction"] = open_prediction
    output["closed_set_is_correct"] = output[TARGET_COLUMN] == output["closed_set_prediction"]
    output["open_set_is_correct"] = output[TARGET_COLUMN] == output["open_set_prediction"]

    expected_unknown = output[TARGET_COLUMN].astype(str).eq(UNKNOWN_LABEL) | ~output[TARGET_COLUMN].isin(known_classes)
    output["open_set_expected_label"] = np.where(expected_unknown, UNKNOWN_LABEL, output[TARGET_COLUMN])
    true_positive = int((review_required & expected_unknown.to_numpy()).sum())
    false_positive = int((review_required & ~expected_unknown.to_numpy()).sum())
    false_negative = int((~review_required & expected_unknown.to_numpy()).sum())
    precision = true_positive / (true_positive + false_positive) if true_positive + false_positive else 0.0
    recall = true_positive / (true_positive + false_negative) if true_positive + false_negative else 0.0

    arguments.output.mkdir(parents=True, exist_ok=True)
    output.to_csv(arguments.output / "predictions.csv", index=False)
    save_confusion(output, "closed_set_prediction", arguments.output / "closed_set_confusion_matrix.csv")
    save_confusion(output, "open_set_prediction", arguments.output / "open_set_confusion_matrix.csv")
    metrics = {
        "calibration_input": str(arguments.calibration_input),
        "input": str(arguments.input),
        "model": str(arguments.model),
        "record_count": len(output),
        "known_coverage_target": arguments.known_coverage,
        "confidence_threshold": threshold,
        "calibration_record_count": len(calibration_split.validation),
        "model_classes": sorted(known_classes),
        "closed_set_accuracy": accuracy_score(output[TARGET_COLUMN], closed_prediction),
        "open_set_accuracy": accuracy_score(output["open_set_expected_label"], open_prediction),
        "review_required_count": int(review_required.sum()),
        "unknown_reason_counts": output["unknown_reason"].value_counts().sort_index().to_dict(),
        "unknown_detection": {
            "true_positive": true_positive,
            "false_positive": false_positive,
            "false_negative": false_negative,
            "precision": precision,
            "recall": recall,
        },
        "closed_set_report": classification_report(output[TARGET_COLUMN], closed_prediction, output_dict=True, zero_division=0),
        "open_set_report": classification_report(output["open_set_expected_label"], open_prediction, output_dict=True, zero_division=0),
    }
    (arguments.output / "metrics.json").write_text(json.dumps(metrics, indent=2, default=str), encoding="utf-8")
    print(f"Open-set evaluated {len(output)} records. Results: {arguments.output.resolve()}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValueError as error:
        print(f"Open-set evaluation failed: {error}")
        raise SystemExit(2)
