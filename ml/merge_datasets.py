"""Merge explicitly selected CSV snapshots into a reproducible training Dataset."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import pandas as pd


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", action="append", required=True, type=Path, help="Input Dataset CSV; repeat for each snapshot.")
    parser.add_argument("--output", required=True, type=Path, help="Merged CSV output path.")
    parser.add_argument("--manifest", type=Path, help="Optional provenance JSON output path.")
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main() -> int:
    arguments = parse_args()
    datasets: list[pd.DataFrame] = []
    source_details: list[dict[str, object]] = []
    expected_columns: list[str] | None = None

    for path in arguments.input:
        if not path.is_file():
            raise ValueError(f"Input Dataset was not found: {path}")

        dataset = pd.read_csv(path, keep_default_na=False)
        if expected_columns is None:
            expected_columns = dataset.columns.tolist()
        elif dataset.columns.tolist() != expected_columns:
            raise ValueError(f"Input Dataset columns do not match the first snapshot: {path}")

        required = {"dataset_record_id", "experiment_id", "experiment_run_id", "actual_fault_type"}
        missing = required.difference(dataset.columns)
        if missing:
            raise ValueError(f"Input Dataset is missing required columns: {', '.join(sorted(missing))}")

        source_details.append(
            {
                "path": str(path),
                "sha256": sha256(path),
                "record_count": len(dataset),
                "experiment_ids": sorted(dataset["experiment_id"].astype(str).unique().tolist()),
                "run_count": int(dataset["experiment_run_id"].nunique()),
                "actual_fault_type_counts": dataset["actual_fault_type"].value_counts().sort_index().to_dict(),
            }
        )
        datasets.append(dataset)

    merged = pd.concat(datasets, ignore_index=True)
    if merged["dataset_record_id"].duplicated().any():
        raise ValueError("Input snapshots contain duplicate dataset_record_id values.")
    if merged["experiment_run_id"].duplicated().any() and len(datasets) > 1:
        run_sources = merged.groupby("experiment_run_id")["experiment_id"].nunique()
        if (run_sources > 1).any():
            raise ValueError("Input snapshots contain an experiment_run_id shared by different experiments.")

    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    merged.to_csv(arguments.output, index=False)

    manifest_path = arguments.manifest or arguments.output.with_suffix(".manifest.json")
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest = {
        "sources": source_details,
        "output": str(arguments.output),
        "output_sha256": sha256(arguments.output),
        "record_count": len(merged),
        "run_count": int(merged["experiment_run_id"].nunique()),
        "actual_fault_type_counts": merged["actual_fault_type"].value_counts().sort_index().to_dict(),
    }
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Merged {len(datasets)} snapshots into {arguments.output} ({len(merged)} records).")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValueError as error:
        print(f"Dataset merge failed: {error}")
        raise SystemExit(2)
