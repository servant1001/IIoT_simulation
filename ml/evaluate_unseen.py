"""Evaluate a trained fault classifier on a held-out or unseen-condition Dataset CSV."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import joblib
import pandas as pd
from sklearn.metrics import accuracy_score, classification_report

from train import CATEGORICAL_COLUMNS, NUMERIC_COLUMNS, TARGET_COLUMN, TEXT_COLUMN, load_dataset


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path, help="Held-out Dataset CSV path")
    parser.add_argument("--model", required=True, type=Path, help="Existing joblib model path; this command never retrains it")
    parser.add_argument("--output", required=True, type=Path, help="Directory for predictions and metrics")
    return parser.parse_args()


def main() -> int:
    arguments = parse_args()
    dataset = load_dataset(arguments.input)
    if not arguments.model.is_file():
        raise ValueError(f"Model file was not found: {arguments.model}")

    feature_columns = NUMERIC_COLUMNS + CATEGORICAL_COLUMNS + [TEXT_COLUMN]
    pipeline = joblib.load(arguments.model)
    predictions = pipeline.predict(dataset[feature_columns])
    output = dataset.copy()
    output["ml_predicted_fault_type"] = predictions
    output["is_correct"] = output[TARGET_COLUMN] == output["ml_predicted_fault_type"]

    arguments.output.mkdir(parents=True, exist_ok=True)
    output.to_csv(arguments.output / "predictions.csv", index=False)
    confusion = pd.crosstab(
        output[TARGET_COLUMN],
        output["ml_predicted_fault_type"],
        rownames=["actual_fault_type"],
        colnames=["ml_predicted_fault_type"],
    )
    confusion.to_csv(arguments.output / "confusion_matrix.csv")

    known_classes = set(getattr(pipeline, "classes_", []))
    actual_classes = set(output[TARGET_COLUMN].unique())
    metrics = {
        "input": str(arguments.input),
        "model": str(arguments.model),
        "record_count": len(output),
        "accuracy": accuracy_score(output[TARGET_COLUMN], predictions),
        "actual_fault_types": sorted(actual_classes),
        "model_classes": sorted(known_classes),
        "unseen_actual_fault_types": sorted(actual_classes.difference(known_classes)),
        "classification_report": classification_report(output[TARGET_COLUMN], predictions, output_dict=True, zero_division=0),
    }
    (arguments.output / "metrics.json").write_text(json.dumps(metrics, indent=2, default=str), encoding="utf-8")
    print(f"Evaluated {len(output)} records. Results: {arguments.output.resolve()}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValueError as error:
        print(f"Evaluation failed: {error}")
        raise SystemExit(2)
