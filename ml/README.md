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

## 未見條件評估

對不屬於正式訓練快照的資料，必須載入既有模型進行 inference，不可先將該資料加入訓練。以下命令會輸出 prediction、confusion matrix 與未知實際類別清單：

```powershell
docker compose --profile ml run --rm ml-lab python ml/evaluate_unseen.py `
  --input artifacts/generalization/modbus-network-delay.csv `
  --model ml/outputs/formal-20261103/logistic_regression.joblib `
  --output ml/outputs/generalization/modbus-network-delay/logistic_regression
```

若 `metrics.json` 的 `unseen_actual_fault_types` 包含 `NetworkDelay`，表示該 Ground Truth 未曾參與模型訓練；此時 Accuracy 與 confusion matrix 應被解讀為未知情境辨識能力，而非重新訓練後的效能。

## G-03：以 G-01 訓練、以 G-02 獨立測試

`NetworkDelay` 的 G-01 資料可作為新增類別的訓練來源；G-02 必須永久排除於合併、分割與模型選擇，僅在新版模型完成後做一次獨立測試。先建立含來源 SHA-256 的合併快照：

```powershell
docker compose --profile ml run --rm -e PYTHONDONTWRITEBYTECODE=1 ml-lab python ml/merge_datasets.py `
  --input artifacts/datasets/formal-20261103/iiot-fault-dataset.csv `
  --input artifacts/generalization/modbus-network-delay-20261108/dataset.csv `
  --output artifacts/training/networkdelay-v1-20261108/iiot-fault-dataset.csv
```

接著以合併快照訓練；每一類皆保有至少 100 個 Run，故此訓練不是 exploratory run：

```powershell
docker compose --profile ml run --rm -e PYTHONDONTWRITEBYTECODE=1 ml-lab python ml/train.py `
  --input artifacts/training/networkdelay-v1-20261108/iiot-fault-dataset.csv `
  --output ml/outputs/networkdelay-v1-20261108
```

最後只對 G-02 執行 `evaluate_unseen.py`。此時 `NetworkDelay` 已是模型類別，G-02 評估的是新增標註資料後的獨立泛化，而不是未見類別能力。

G-04 是後續的近門檻獨立測試資料，僅能對已凍結的 G-03 模型做 inference；不可回填至上述訓練快照或用來調整模型與延遲門檻。其實際結果與限制保存在 `docs/GeneralizationExperiments.md`。

## Open-set Unknown／人工確認評估

`evaluate_open_set.py` 不會重訓模型；它只用既有訓練快照重建 Run-aware validation split，從已知資料校準信心門檻。若 Adapter 已輸出 `Unknown`，或模型信心低於該門檻，輸出會改為 `Unknown` 並標記 `review_required`：

```powershell
docker compose --profile ml run --rm -e PYTHONDONTWRITEBYTECODE=1 ml-lab python ml/evaluate_open_set.py `
  --calibration-input artifacts/training/networkdelay-v1-20261108/iiot-fault-dataset.csv `
  --input artifacts/generalization/modbus-unknown-exception-g05-20261108/dataset.csv `
  --model ml/outputs/networkdelay-v1-20261108/random_forest.joblib `
  --output ml/outputs/generalization/modbus-unknown-exception-g05-open/random_forest
```

此機制應被描述為安全護欄，不應聲稱僅靠信心分數就能辨識所有未知故障；應另行評估低信心與 Adapter `Unknown` 兩種觸發來源。
