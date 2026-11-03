# 正式 Dataset 快照：2026-11-03

## 範圍

本快照只匯出 [ExperimentReport.md](ExperimentReport.md) 所列的 15 個正式 Experiment，並透過 `experimentIds` query parameter 限定來源。小型驗證、Stopped Experiment 與手動裝置測試均未納入。

## 產物

| 檔案 | 用途 |
| --- | --- |
| `artifacts/datasets/formal-20261103/iiot-fault-dataset.csv` | 正式 Dataset CSV 快照。 |
| `artifacts/datasets/formal-20261103/coverage.json` | API 回傳的覆蓋率快照。 |

`artifacts/` 依 `.gitignore` 排除，避免將資料庫匯出資料提交至 Git；使用者應另行備份該資料夾。

## 驗證結果

| 項目 | 結果 |
| --- | ---: |
| 指定正式 Experiment | 15 |
| 完整 Experiment Run | 1,500 |
| CSV 解析後紀錄數 | 3,800 |
| Coverage API 紀錄數 | 3,800 |
| 缺少 Experiment Run ID 的 CSV 紀錄 | 0 |
| 未達 100 Run 的 Protocol／FaultType 組合 | 0 |

CSV 的原始錯誤訊息欄位可能包含換行字元，因此資料列數以 CSV parser 驗證，不以文字檔行數計算。

## 覆蓋率

| 協定 | Ground Truth | Run | 紀錄 |
| --- | --- | ---: | ---: |
| Modbus TCP | None | 100 | 1,000 |
| Modbus TCP | ConnectionRefused | 100 | 100 |
| Modbus TCP | Timeout | 100 | 200 |
| Modbus TCP | IllegalAddress | 100 | 300 |
| OPC UA | None | 100 | 400 |
| OPC UA | Timeout | 100 | 100 |
| OPC UA | InvalidEndpoint | 100 | 100 |
| OPC UA | InvalidNodeId | 100 | 100 |
| OPC UA | CertificateError | 100 | 100 |
| OPC UA | SessionDisconnected | 100 | 100 |
| MQTT | None | 100 | 700 |
| MQTT | AuthenticationFailed | 100 | 100 |
| MQTT | BrokerUnavailable | 100 | 100 |
| MQTT | TopicMismatch | 100 | 300 |
| MQTT | PayloadInvalid | 100 | 100 |

## 完整性

CSV 的 SHA-256：

```text
37ED4D97466F5686907E3D7F981599598EFB9005B8E710DAF326C31522C88BA1
```

若重新匯出後 hash 不同，應同時保存新的 coverage JSON、輸出時間與所選 Experiment ID，再進行 ML 或診斷評估，避免將不同資料快照的結果混為一談。
