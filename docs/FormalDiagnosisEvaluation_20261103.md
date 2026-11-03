# 15 種 IIoT 通訊情境的診斷評估：2026-11-03

## 1. 目的與範圍

本評估以 [FormalDatasetSnapshot_20261103.md](FormalDatasetSnapshot_20261103.md) 的固定快照為唯一資料來源，對 15 個已完成的正式 Experiment 執行 Rule-based、Machine Learning（ML）與本機 RAG 診斷評估。

| 項目 | 值 |
| --- | ---: |
| 正式情境 | 15 |
| 完成 Experiment Run | 1,500 |
| 通訊紀錄 | 3,800 |
| CSV SHA-256 | `37ED4D97466F5686907E3D7F981599598EFB9005B8E710DAF326C31522C88BA1` |

情境包含 Modbus TCP 4 種、OPC UA 6 種、MQTT 5 種；其完整 Run 與紀錄覆蓋率見前述快照。Ground Truth 取自 Experiment 的 `actualFaultType`，不使用診斷輸出回填。

## 2. 評估方法

| 方法 | 執行方式 | 指標 | 評估資料 |
| --- | --- | --- | --- |
| Rule-based | 逐筆呼叫 `/api/communication-records/{id}/diagnose`，保存 `RuleBased` 診斷結果 | Accuracy | 全部 3,800 筆 |
| RAG | 逐筆呼叫 `/api/communication-records/{id}/rag-diagnose`，保存候選故障與來源 trace | Top-1、Top-3 Accuracy、平均時間 | 全部 3,800 筆 |
| ML | `ml/train.py` 以 `experiment_run_id` 分割訓練、驗證、測試 Run | Accuracy、weighted Precision／Recall／F1 | 保留測試集 26 筆 |

Rule-based 評估查詢只計算 `DiagnosisMethod.RuleBased`，避免混入 RAG 診斷結果。ML 的輸入欄位為 `protocol`、`status`、`observed_fault_type`、`error_code`、`error_message`、`response_time_ms`、`retry_count`；不使用 Experiment ID、Device ID 或預測結果。

ML 的標籤為 FaultType，而非 15 個「協定 × 情境」組合；因此三個正常情境皆為 `None`，兩個逾時情境皆為 `Timeout`，共形成 12 個 FaultType 類別。

## 3. 結果

### 3.1 整體結果

| 方法 | 正確筆數／評估筆數 | 主要結果 | 補充 |
| --- | ---: | --- | --- |
| Rule-based | 700 / 3,800 | Accuracy **18.42%** | 僅辨識 Modbus `ConnectionRefused`、`Timeout`、`IllegalAddress`。 |
| RAG | 1,600 / 3,800 | Top-1 **42.11%**；Top-3 **42.11%** | 平均診斷時間 **2.09 ms**。 |
| Logistic Regression | 26 / 26 | Test Accuracy／weighted F1 **100.00%** | Validation weighted F1 100.00%。 |
| Random Forest | 26 / 26 | Test Accuracy／weighted F1 **100.00%** | Validation weighted F1 100.00%。 |
| SVM | 19 / 26 | Test Accuracy **73.08%**；weighted F1 **70.97%** | Validation weighted F1 83.07%。 |

### 3.2 Rule-based 與 RAG 的逐情境結果

| 協定 | 情境 | 紀錄數 | Rule-based 正確 | RAG Top-1 | RAG Top-3 |
| --- | --- | ---: | ---: | ---: | ---: |
| Modbus TCP | Normal | 1,000 | 0 / 1,000 | 0 / 1,000 | 0 / 1,000 |
| Modbus TCP | Connection Refused | 100 | 100 / 100 | 100 / 100 | 100 / 100 |
| Modbus TCP | Timeout | 200 | 200 / 200 | 200 / 200 | 200 / 200 |
| Modbus TCP | Illegal Address | 300 | 300 / 300 | 300 / 300 | 300 / 300 |
| OPC UA | Normal | 400 | 0 / 400 | 0 / 400 | 0 / 400 |
| OPC UA | Invalid Endpoint | 100 | 0 / 100 | 100 / 100 | 100 / 100 |
| OPC UA | Timeout | 100 | 100 / 100 | 100 / 100 | 100 / 100 |
| OPC UA | Invalid NodeId | 100 | 0 / 100 | 100 / 100 | 100 / 100 |
| OPC UA | Certificate Error | 100 | 0 / 100 | 100 / 100 | 100 / 100 |
| OPC UA | Session Disconnected | 100 | 0 / 100 | 0 / 100 | 0 / 100 |
| MQTT | Normal JSON | 700 | 0 / 700 | 0 / 700 | 0 / 700 |
| MQTT | Authentication Failed | 100 | 0 / 100 | 100 / 100 | 100 / 100 |
| MQTT | Broker Unavailable | 100 | 0 / 100 | 100 / 100 | 100 / 100 |
| MQTT | Topic Mismatch | 300 | 0 / 300 | 300 / 300 | 300 / 300 |
| MQTT | Payload Invalid | 100 | 0 / 100 | 100 / 100 | 100 / 100 |

Rule-based 的 700 筆正確結果來自 3 個 Modbus 情境；OPC UA Timeout 仍未由 Rule-based 規則辨識。RAG 的 Top-3 與 Top-1 相同，表示目前的候選集合沒有在 Top-2 或 Top-3 補回 Top-1 失誤。RAG 漏判所有正常情境與 OPC UA `SessionDisconnected`，反映目前知識庫與候選規則仍不完整。

### 3.3 ML 輸出

ML 使用以 Run 為界的切分，以降低同一 Run 的多筆紀錄同時出現在訓練與測試資料的風險：訓練集 3,749 筆、驗證集 25 筆、測試集 26 筆。

| 模型 | Validation weighted F1 | Test Accuracy | Test weighted Precision | Test weighted Recall | Test weighted F1 |
| --- | ---: | ---: | ---: | ---: | ---: |
| Logistic Regression | 100.00% | 100.00% | 100.00% | 100.00% | 100.00% |
| Random Forest | 100.00% | 100.00% | 100.00% | 100.00% | 100.00% |
| SVM | 83.07% | 73.08% | 75.64% | 73.08% | 70.97% |

訓練輸出保存在未納入 Git 的 `ml/outputs/formal-20261103/`：`leaderboard.csv`、`metrics.json`、三個模型檔與 confusion matrix。這些檔案應和正式 CSV 快照一併備份。

### 3.4 混淆矩陣摘要

混淆矩陣的列為實際 FaultType、欄為預測 FaultType；PNG 與同名 CSV 均已輸出至 `ml/outputs/formal-20261103/`。CSV 使數值可被重新檢查，PNG 適合作為書審附件或口試投影片。

| 模型 | 對角線正確分類 | 非對角線誤判 | 解讀 |
| --- | ---: | ---: | --- |
| Logistic Regression | 26 / 26 | 0 | 12 類皆落在對角線。 |
| Random Forest | 26 / 26 | 0 | 12 類皆落在對角線。 |
| SVM | 19 / 26 | 7 | 錯誤集中在少量異常類別，而非正常類別。 |

SVM 測試混淆矩陣的所有非零非對角線項目如下；未列出的項目均為 0。

| 實際 FaultType | 預測 FaultType | 筆數 |
| --- | --- | ---: |
| `CertificateError` | `InvalidEndpoint` | 1 |
| `InvalidNodeId` | `CertificateError` | 1 |
| `IllegalAddress` | `AuthenticationFailed` | 2 |
| `TopicMismatch` | `AuthenticationFailed` | 3 |

`svm_confusion_matrix.csv` 同時顯示 `IllegalAddress` 有 1 筆、`TopicMismatch` 有 0 筆正確分類；其餘類別的測試樣本皆被 SVM 正確分類。Logistic Regression 與 Random Forest 的矩陣各有 26 筆對角線值、無任何非對角線值。

## 4. 解讀與限制

1. **這不是三者完全對等的排名。** Rule-based 與 RAG 的數字來自全量逐筆線上診斷；ML 是對未參與訓練的 Run 保留集評估，且只有 26 筆測試紀錄。因此，不能以 100.00% 對 42.11% 直接斷言 ML 在任何情境都優於 RAG。
2. **ML 成績屬受控資料基準。** Simulator 的錯誤碼、錯誤文字與觀測 FaultType 高度穩定且可區分；再加上小型保留測試集，Logistic Regression 和 Random Forest 的滿分只證明資料、標註、切分與訓練流程可運作，不能宣稱為真實產線的泛化效能。
3. **正常類別是目前 Rule/RAG 的明顯缺口。** 現有規則與 RAG 知識來源沒有把 `None` 建模為可輸出的正常診斷，造成三個正常情境共 2,100 筆均未命中。
4. **RAG 仍需人工評閱。** 本次已量測故障分類與診斷時間，但尚未以人工標註量測 Explanation Quality、來源充分性或 Hallucination Rate。
5. **需擴大驗證。** 後續應增加不同設備、網路延遲／抖動、錯誤訊息變體與跨日期資料，並採多個保留 Run 或交叉驗證，取得較穩定的 ML 泛化估計。

## 5. 實驗結論

1. **平台與資料流程已被正式驗證。** 三種協定的 15 種受控情境均有 100 個完成 Run，並能從實際網路通訊、Ground Truth、資料庫紀錄、CSV 快照一路追溯至三種診斷方法的評估。
2. **現有 Rule-based 規則覆蓋不足。** 18.42% 的全量 Accuracy 顯示規則目前只涵蓋部分 Modbus 異常；它缺少正常狀態、OPC UA 多數錯誤與 MQTT 錯誤的規則。
3. **RAG 已擴大異常覆蓋，但知識庫仍有明確缺口。** RAG 能正確辨識 10 種異常情境，然而正常狀態與 `SessionDisconnected` 不在目前候選知識中，故 Top-1 與 Top-3 同為 42.11%。
4. **ML 在受控資料上可有效區分故障特徵。** Logistic Regression 與 Random Forest 在 Run-aware 保留測試達 100.00%，而 SVM 的混淆矩陣呈現它對少數異常類別仍有混淆。這支持 ML pipeline 作為研究基準，但不等同於已證明產線泛化能力。
5. **下一輪研究應著重外部效度與解釋品質。** 補齊正常／SessionDisconnected 的規則與 RAG 知識、增加不同設備和網路條件的資料、擴大保留測試集，並由人工量測 RAG 的建議正確性、來源充分性與 Hallucination Rate。

## 6. 可重現步驟

```powershell
# 匯出固定資料快照後，以 Run-aware split 重新訓練與評估
docker compose --profile ml run --rm ml-lab python ml/train.py `
  --input artifacts/datasets/formal-20261103/iiot-fault-dataset.csv `
  --output ml/outputs/formal-20261103
```

Rule-based 與 RAG 的逐筆 API 呼叫結果已保存至 PostgreSQL 的診斷結果與 RAG trace；可分別經由每個 Experiment 的 `diagnosis-evaluation` 與 `rag-evaluation` API 查詢。重跑前應保留新的 CSV hash、輸出目錄與評估日期，避免混合不同快照的結果。
