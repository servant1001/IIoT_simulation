# Phase 9 Machine Learning Pipeline

本目錄提供可重複執行的分類流程，輸入為 Phase 8 的正式 Dataset CSV，標籤固定使用 `actual_fault_type`。既有的 `predicted_fault_type` 不會作為特徵或標籤，避免將診斷結果洩漏到模型訓練。

## 執行環境

Python 不必安裝在 Windows。本專案透過 Docker 提供 Python 3.12、JupyterLab、pandas、scikit-learn 與 XGBoost：

```powershell
docker compose --profile ml build ml-lab
docker compose --profile ml up ml-lab
```

開啟 <http://127.0.0.1:8888/lab/tree/ml/notebooks/phase9_pipeline.ipynb> 即可使用 Jupyter Notebook。此服務只綁定本機 `127.0.0.1`；停止時執行 `docker compose --profile ml stop ml-lab`。

## 匯出並訓練

先啟動 API，將 Phase 8 正式 Dataset 匯出至本機：

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
Invoke-WebRequest http://127.0.0.1:5080/api/datasets/communication-records/export -OutFile artifacts/iiot-fault-dataset.csv
```

完成每個 Fault 至少 100 個 runs 後，執行訓練：

```powershell
docker compose --profile ml run --rm ml-lab python ml/train.py --input artifacts/iiot-fault-dataset.csv --output ml/outputs/phase9
```

流程依 `experiment_run_id` 分割資料，任何一個 Run 只會出現在 train、validation、test 的其中一個集合。每個 Fault 至少需要 3 個不同 runs 才能建立三個集合；預設仍要求每類 100 runs。

當資料尚未達到 100 runs 時，只有在確認用途為管線驗證的情況下，才可執行：

```powershell
docker compose --profile ml run --rm ml-lab python ml/train.py --input artifacts/iiot-fault-dataset.csv --output ml/outputs/exploratory --allow-small-dataset --minimum-runs-per-class 3
```

這會在 `metrics.json` 中標示 `exploratory: true`，不能作為正式研究結果。

## 輸出

每次訓練會在輸出目錄建立：

- `leaderboard.csv`：Logistic Regression、Random Forest、SVM 的 validation F1 與 test metrics 比較。若執行環境另行安裝 XGBoost，程式會自動將 XGBoost 納入比較。
- `metrics.json`：Accuracy、weighted Precision、Recall、F1 與分類報告。
- `{model}_confusion_matrix.png`：每個模型的 Test confusion matrix。
- `{model}.joblib`：訓練後的 sklearn pipeline。

特徵使用 `protocol`、`status`、`observed_fault_type`、`error_code`、`error_message`、`response_time_ms`、`retry_count`。實驗、設備與紀錄識別碼不會作為模型特徵。
