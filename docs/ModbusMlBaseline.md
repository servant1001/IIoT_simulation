# Modbus 初步 Machine Learning Baseline

## 目的

使用已完成的四組正式 Modbus Experiment，驗證從 Dataset 匯出、依 Experiment Run 切分、模型訓練到評估輸出的完整流程。本結果為受控模擬環境的初步 baseline，不應直接推論至所有實際工業現場。

## 資料來源

資料集僅保留下列四個完成 100 Run 的正式 Experiment，不包含單次驗證、先前部分失敗實驗或整合測試資料。

| Ground Truth | Run 數 | 通訊紀錄數 |
| --- | ---: | ---: |
| `None` | 100 | 1,000 |
| `ConnectionRefused` | 100 | 100 |
| `Timeout` | 100 | 200 |
| `IllegalAddress` | 100 | 300 |
| **合計** | **400** | **1,600** |

訓練資料位於 `artifacts/modbus-formal-baseline-20261028.csv`。標籤固定使用 `actual_fault_type`；Experiment、Device 與 Record ID 均未作為特徵。

## 實驗方法

- 模型：Logistic Regression、Random Forest、SVM。
- 類別平衡：三種模型皆採用 `class_weight="balanced"`。
- 切分方式：以完整 `experiment_run_id` 切分，避免同一 Run 的資料同時出現在訓練、驗證與測試集合。
- 每個類別保留 1 個 Run 至驗證集、1 個 Run 至測試集，其餘 98 個 Run 用於訓練。

## 結果

| 模型 | Validation weighted F1 | Test Accuracy | Test weighted F1 |
| --- | ---: | ---: | ---: |
| Logistic Regression | 1.000 | 1.000 | 1.000 |
| Random Forest | 1.000 | 1.000 | 1.000 |
| SVM | 1.000 | 1.000 | 1.000 |

輸出產物在 `ml/outputs/modbus-baseline-20261028/`：

- `leaderboard.csv`：模型比較結果。
- `metrics.json`：切分、類別覆蓋率與完整分類指標。
- `*_confusion_matrix.png`：各模型的測試混淆矩陣。
- `*.joblib`：可重用的訓練後模型。

## 結果判讀與限制

所有模型達到 1.000 指標，是因為目前四個故障情境具有高度明確、可重現的協定錯誤特徵；例如 `ReadTimeout`、TCP `ConnectionRefused` 與 Modbus Exception Code `02`。測試集僅包含 4 個完整 Run、16 筆紀錄，且資料來自固定模擬器設定，不能視為模型對不同 PLC、網路品質、資料量或實際現場雜訊的泛化證明。

後續應加入 OPC UA、MQTT 與更多 Modbus 故障情境，並改變延遲、重試、設備負載與網路條件，擴大獨立測試資料後再比較模型的實務可用性。
