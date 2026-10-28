# IIoT 通訊故障診斷實驗報告

> 最後核對日期：2026-10-28（Asia/Taipei）  
> 資料來源：本機 API 與 PostgreSQL 中的正式 Experiment Run／Communication Record。  
> 範圍：僅列入已完成 100 次 Run 的受控正式實驗；不含單次驗證、手動測試、半途停止或開發期間的紀錄。

## 1. 研究目的

本專案建立可重複執行的 IIoT 通訊故障診斷實驗環境，驗證系統能否：

1. 透過實際 Modbus TCP 與 MQTT 網路通訊蒐集資料。
2. 在故障發生時，將通訊結果正規化為一致的 `FaultType` 與錯誤碼。
3. 將實際故障條件（Ground Truth）與系統後續的規則式、ML、RAG 診斷預測分開保存。
4. 提供可追溯至 Experiment、Run 與單筆通訊紀錄的資料集。

`actualFaultType` 是實驗的 Ground Truth 標籤；只有在外部模擬器或服務狀態確實製造相符的故障時，該筆正式資料才納入本報告。

## 2. 受控實驗環境

| 元件 | 用途 | 本次使用方式 |
| --- | --- | --- |
| Docker Compose | 啟動可重現的本機服務 | 管理 PostgreSQL、Mosquitto、Modbus Simulator 與相關發送器。 |
| PostgreSQL | 實驗資料保存 | 保存 Experiment、Run、Communication Record 與 Ground Truth。 |
| 自建 Modbus TCP Simulator | 正常與 Modbus 故障通訊 | 以 Python／Pymodbus 提供正常 Register；另以 TCP 模擬器提供 Timeout 與 Modbus Exception 回應。 |
| Eclipse Mosquitto | MQTT Broker | 監聽本機 `127.0.0.1:1883`。 |
| MQTT JSON Publisher | MQTT 正常情境 | 每 0.5 秒對 `factory/machine01/data` 發送 QoS 1 合法 JSON。 |
| MQTT Invalid Payload Publisher | MQTT 無效資料情境 | 對相同 topic 持續發送純文字 `not-json-payload`。 |

正式實驗開始前，`tools/Start-Experiment.ps1` 會只啟用當次目標 Device，避免其他 Collector 來源混入資料。完整操作流程見 [ExperimentCollectionGuide.md](ExperimentCollectionGuide.md)。

## 3. 實驗設計與品質控制

- 每個正式情境的 `repeatCount` 為 100，且本報告只採計狀態為 Completed 的 Run。
- 先執行 1 次小型驗證實驗，確認外部故障條件與實際回傳的 `FaultType` 相符，再開始 100 次正式收集。
- MQTT `PayloadInvalid` 收集期間會停止正常 JSON Publisher；完成後停止無效 Payload Publisher 並恢復正常 Publisher，避免混入不同資料型態。
- 同一 Run 可能產生多筆 Communication Record：正常輪詢可取得多筆成功資料；部分錯誤則可能因重試而產生多筆 Timeout／Failure。因此，Run 數與紀錄數不必相同。
- 統計由 API 的 Experiment、Run、Records 端點於報告日期重新核對，而非由手動估算。

## 4. 正式實驗結果

### 4.1 結果總覽

| 協定 | 情境 | Experiment 名稱 | 完成 Run | 通訊紀錄 | 成功 | Failed | Timeout | 實際 FaultType 驗證 |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| Modbus TCP | 正常 | `MODBUS_NORMAL_100RUN_CLEAN_20261026` | 100 | 1,000 | 1,000 | 0 | 0 | `None (0)`：1,000 筆 |
| Modbus TCP | Connection Refused | `MODBUS_CONNECTION_REFUSED_100RUN_20261026` | 100 | 100 | 0 | 100 | 0 | `ConnectionRefused (1)`：100 筆 |
| Modbus TCP | Timeout | `MODBUS_TIMEOUT_100RUN_20261027` | 100 | 200 | 0 | 0 | 200 | `Timeout (4)`：200 筆 |
| Modbus TCP | Illegal Address | `MODBUS_ILLEGAL_ADDRESS_100RUN_20261027` | 100 | 300 | 0 | 300 | 0 | `IllegalAddress (8)`：300 筆 |
| MQTT | 正常 JSON | `MQTT_NORMAL_100RUN_20261028` | 100 | 700 | 700 | 0 | 0 | `None (0)`：700 筆 |
| MQTT | Broker Unavailable | `MQTT_BROKER_UNAVAILABLE_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `BrokerUnavailable (16)`：100 筆 |
| MQTT | Payload Invalid | `MQTT_PAYLOAD_INVALID_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `PayloadInvalid (18)`：100 筆 |
| **合計** | **7 種情境** |  | **700** | **2,500** | **1,700** | **600** | **200** | **所有正式紀錄皆與對應 Ground Truth 一致** |

### 4.2 可追溯的 Experiment ID 與錯誤碼

| 情境 | Experiment ID | 主要錯誤碼／回應 |
| --- | --- | --- |
| Modbus TCP 正常 | `041e9b97-47cc-41eb-8b8f-0d67b48835bd` | 無錯誤碼。 |
| Modbus TCP Connection Refused | `b3b9d96f-99e1-4068-b11b-19334f97bd8b` | `ConnectionRefused`。 |
| Modbus TCP Timeout | `db384e41-5f6f-419e-b8a3-a6eee732bee0` | `ReadTimeout`。 |
| Modbus TCP Illegal Address | `03dfc0ac-afae-4b07-89e5-e8c1015b7a46` | `ModbusExceptionCode02`。 |
| MQTT 正常 JSON | `cd71f7be-3994-4f45-996e-95d8c1050884` | 無錯誤碼。 |
| MQTT Broker Unavailable | `56fc4694-becd-43d4-be0c-30772b2174b1` | `MqttCommunicationException`。 |
| MQTT Payload Invalid | `a02c06c5-5346-42c5-a6d9-02227cbafda7` | `JsonReaderException`。 |

### 4.3 情境說明

#### Modbus TCP

- **正常**：連線至自建 Modbus Simulator，讀取 Slave ID 1 的正常 Holding Registers。
- **Connection Refused**：對未開放的目標連接埠建立連線，系統回傳 `ConnectionRefused`。
- **Timeout**：Timeout Simulator 接收 TCP 連線與請求但不回覆，讓 Client 端產生 `ReadTimeout` 並正規化為 `Timeout`。
- **Illegal Address**：Illegal Address Simulator 對 Modbus 查詢回覆 exception code `02`，系統正規化為 `IllegalAddress`。

#### MQTT

- **正常 JSON**：Mosquitto 運作，JSON Publisher 以 QoS 1 持續傳送合法 JSON 至預設 topic。
- **Broker Unavailable**：停止 Mosquitto 與 JSON Publisher，Collector 連線至 `127.0.0.1:1883` 時產生 `MqttCommunicationException`，並正規化為 `BrokerUnavailable`；收集完成後服務已恢復。
- **Payload Invalid**：Mosquitto 保持運作，但停止 JSON Publisher、改用 Invalid Payload Publisher 對同一 topic 發送純文字。100 筆資料均被 JSON Parser 拒絕並正規化為 `PayloadInvalid`；收集完成後已恢復正常 JSON Publisher。

## 5. 初步 ML 與診斷工作

Modbus 的四種情境已另行建立第一版受控環境 ML baseline：400 個 Run、1,600 筆紀錄，以 Experiment Run 為單位切分訓練、驗證與測試資料。Logistic Regression、Random Forest 與 SVM 在此受控資料上的 weighted F1 均為 1.000。詳細方法、輸出與限制見 [ModbusMlBaseline.md](ModbusMlBaseline.md)。

此數值只證明資料管線、標註與模型訓練流程能運作；因 Simulator 條件具高度可分性、測試 Run 數有限，**不可描述為現場工業環境的泛化效能**。MQTT 的 300 個正式 Run 目前尚未加入跨協定 ML 成績。

Rule-based 與 RAG 診斷功能已在專案中實作；後續可使用這些正式 Experiment ID 對照 Ground Truth，評估 Top-1／Top-3 Accuracy、診斷時間與解釋品質。

## 6. 已知限制

1. 本報告以本機 Docker 與外部模擬服務的受控條件為主，尚未使用真實 PLC、工業網路延遲或現場設備資料。
2. 正式資料目前涵蓋 Modbus TCP 與 MQTT；OPC UA 雖已完成連線與多種錯誤的功能驗證，尚未完成 100 次 Run 的正式資料集。
3. Collector 的正式 Experiment 仍使用固定協定預設值：Modbus Slave ID 1／Register 0–5、OPC UA NodeId `i=2258`／無安全連線、MQTT 匿名連線／固定 topic。因此 Modbus `WrongSlaveId`、OPC UA `InvalidNodeId` 與 `CertificateError`、MQTT `AuthenticationFailed` 與 `TopicMismatch` 尚不能直接形成正式 Dataset。
4. 某些情境的每 Run 紀錄數不同，原因是輪詢頻率、訊息到達頻率與重試機制不同；比較資料量時應同時報告 Run 數與紀錄數。
5. 實驗資料位於 Docker PostgreSQL Volume；Git commit 保存程式、Compose 與文件，**不會自動保存資料庫內容**。重要研究資料應另外匯出 CSV 並備份。

## 7. 下一步

1. 使用已安裝的 Prosys OPC UA Simulation Server，先完成 OPC UA 正常情境的 100 次正式 Experiment Runs。
2. 依序收集可由目前設定重現的 OPC UA 故障情境，並補強 Device／Experiment 的協定設定欄位，使 `InvalidNodeId`、`CertificateError`、MQTT 認證與 topic mismatch 可以成為正式資料。
3. 匯出完成的正式資料集，將 MQTT 與未來 OPC UA 資料納入跨協定或分協定的 ML 比較。
4. 以相同 Ground Truth 進行 Rule-based、ML 與 RAG 的公平評估，並記錄正確率、延遲、可解釋性與錯誤案例。

## 8. 重現與查詢

啟動本機服務與 API 後，可使用下列端點查核某個 Experiment：

```powershell
$id = "a02c06c5-5346-42c5-a6d9-02227cbafda7"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/$id"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/$id/runs"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/$id/records"
```

各情境的建立與 Simulator 切換命令請參閱 [ExperimentCollectionGuide.md](ExperimentCollectionGuide.md)。
