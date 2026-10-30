# IIoT 通訊故障診斷實驗報告

> 最後核對日期：2026-10-30（Asia/Taipei）<br>
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
| MQTT Authentication Simulator | MQTT 認證失敗情境 | 隔離於 `127.0.0.1:1884` 的 Mosquitto；僅允許研究用帳號登入。 |

正式實驗開始前，`tools/Start-Experiment.ps1` 會只啟用當次目標 Device，避免其他 Collector 來源混入資料。完整操作流程見 [ExperimentCollectionGuide.md](ExperimentCollectionGuide.md)。

## 3. 實驗設計與品質控制

- 每個正式情境的 `repeatCount` 為 100，且本報告只採計狀態為 Completed 的 Run。
- 先執行 1 次小型驗證實驗，確認外部故障條件與實際回傳的 `FaultType` 相符，再開始 100 次正式收集。
- MQTT `PayloadInvalid` 收集期間會停止正常 JSON Publisher；完成後停止無效 Payload Publisher 並恢復正常 Publisher，避免混入不同資料型態。
- MQTT `AuthenticationFailed` 使用獨立 Broker 與錯誤密碼；完成後停止該 Broker，並將目標 Device 還原至正常 MQTT Broker。
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
| OPC UA | 正常 | `OPCUA_NORMAL_100RUN_20261028` | 100 | 400 | 400 | 0 | 0 | `None (0)`：400 筆 |
| OPC UA | Invalid Endpoint | `OPCUA_INVALID_ENDPOINT_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `InvalidEndpoint (12)`：100 筆 |
| OPC UA | Timeout | `OPCUA_TIMEOUT_100RUN_20261028` | 100 | 100 | 0 | 0 | 100 | `Timeout (4)`：100 筆 |
| OPC UA | Invalid NodeId | `OPCUA_INVALID_NODE_ID_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `InvalidNodeId (13)`：100 筆 |
| OPC UA | Certificate Error | `OPCUA_CERTIFICATE_ERROR_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `CertificateError (14)`：100 筆 |
| MQTT | 正常 JSON | `MQTT_NORMAL_100RUN_20261028` | 100 | 700 | 700 | 0 | 0 | `None (0)`：700 筆 |
| MQTT | Broker Unavailable | `MQTT_BROKER_UNAVAILABLE_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `BrokerUnavailable (16)`：100 筆 |
| MQTT | Payload Invalid | `MQTT_PAYLOAD_INVALID_100RUN_20261028` | 100 | 100 | 0 | 100 | 0 | `PayloadInvalid (18)`：100 筆 |
| MQTT | Authentication Failed | `MQTT_AUTHENTICATION_FAILED_100RUN_20261029` | 100 | 100 | 0 | 100 | 0 | `AuthenticationFailed (6)`：100 筆 |
| **合計** | **13 種情境** |  | **1,300** | **3,400** | **2,100** | **1,000** | **300** | **所有正式紀錄皆與對應 Ground Truth 一致** |

### 4.2 可追溯的 Experiment ID 與錯誤碼

| 情境 | Experiment ID | 主要錯誤碼／回應 |
| --- | --- | --- |
| Modbus TCP 正常 | `041e9b97-47cc-41eb-8b8f-0d67b48835bd` | 無錯誤碼。 |
| Modbus TCP Connection Refused | `b3b9d96f-99e1-4068-b11b-19334f97bd8b` | `ConnectionRefused`。 |
| Modbus TCP Timeout | `db384e41-5f6f-419e-b8a3-a6eee732bee0` | `ReadTimeout`。 |
| Modbus TCP Illegal Address | `03dfc0ac-afae-4b07-89e5-e8c1015b7a46` | `ModbusExceptionCode02`。 |
| OPC UA 正常 | `6a764e7b-72ee-44c6-ba7f-c32e4fd1ddd2` | 無錯誤碼。 |
| OPC UA Invalid Endpoint | `1fb37472-4ac6-4a4b-b27e-47382db7290d` | `ServiceResultException`（Server 回傳 `BadTcpEndpointUrlInvalid`）。 |
| OPC UA Timeout | `19ac5f4a-2a24-45eb-bbbf-9831781b2225` | `ServiceResultException`（Simulator 不回覆，Client 回傳 `BadRequestTimeout`）。 |
| OPC UA Invalid NodeId | `03ac8c47-2836-41cd-bd54-d3223ed09b5f` | `ServiceResultException`（Prosys 回傳 `BadNodeIdUnknown`）。 |
| OPC UA Certificate Error | `917dd2ae-84d0-473a-ae44-8759c5c375a0` | `ServiceResultException`（Prosys 回傳 `BadSecurityChecksFailed`）。 |
| MQTT 正常 JSON | `cd71f7be-3994-4f45-996e-95d8c1050884` | 無錯誤碼。 |
| MQTT Broker Unavailable | `56fc4694-becd-43d4-be0c-30772b2174b1` | `MqttCommunicationException`。 |
| MQTT Payload Invalid | `a02c06c5-5346-42c5-a6d9-02227cbafda7` | `JsonReaderException`。 |
| MQTT Authentication Failed | `cb92eac5-0588-4914-9b8b-ef203174150f` | `NotAuthorized`。 |

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
- **Authentication Failed**：在隔離的帳密 Broker 建立研究用帳號，並讓 Device 使用錯誤密碼連線。100 筆資料皆收到 `NotAuthorized`，並正規化為 `AuthenticationFailed`；收集完成後已停止隔離 Broker、移除帳密設定，並還原至正常 MQTT Broker。

#### OPC UA

- **正常**：啟動本機 Prosys OPC UA Simulation Server，使用 `opc.tcp://localhost:53530/OPCUA/SimulationServer` 與預設 NodeId `i=2258`。100 個 Run 共取得 400 筆成功讀取，皆為 `None`，未出現 Session 中斷、逾時或憑證錯誤。
- **Invalid Endpoint**：Server 維持運作，但 Collector 指向不存在的 `opc.tcp://localhost:53530/invalid`。100 筆資料均收到 `BadTcpEndpointUrlInvalid`，並正規化為 `InvalidEndpoint`／`ServiceResultException`。收集完成後，目標 Device 已恢復為正常 endpoint。
- **Timeout**：Docker 內的 `opcua-timeout-simulator` 接受 TCP 連線但不回覆 OPC UA 封包。100 筆資料皆在預設逾時與重試後回傳 `BadRequestTimeout`，並正規化為 `Timeout`／`ServiceResultException`。每個 Run 約需 43 秒；收集完成後已停止 Simulator，目標 Device 已恢復為正常 Prosys endpoint。
- **Invalid NodeId**：透過 Device protocol settings 將 NodeId 設為不存在的 `i=999999`。100 筆資料皆回傳 `BadNodeIdUnknown`，並正規化為 `InvalidNodeId`／`ServiceResultException`；收集完成後已恢復預設 NodeId `i=2258`。
- **Certificate Error**：透過 Device protocol settings 啟用 OPC UA 安全連線，但不將 Client 憑證加入 Prosys 信任清單。100 筆資料皆回傳 `BadSecurityChecksFailed`，並正規化為 `CertificateError`／`ServiceResultException`；收集完成後已恢復不使用安全連線的設定。

## 5. 初步 ML 與診斷工作

Modbus 的四種情境已另行建立第一版受控環境 ML baseline：400 個 Run、1,600 筆紀錄，以 Experiment Run 為單位切分訓練、驗證與測試資料。Logistic Regression、Random Forest 與 SVM 在此受控資料上的 weighted F1 均為 1.000。詳細方法、輸出與限制見 [ModbusMlBaseline.md](ModbusMlBaseline.md)。

此數值只證明資料管線、標註與模型訓練流程能運作；因 Simulator 條件具高度可分性、測試 Run 數有限，**不可描述為現場工業環境的泛化效能**。MQTT 的 400 個正式 Run 目前尚未加入跨協定 ML 成績。

Rule-based 與 RAG 診斷功能已在專案中實作；後續可使用這些正式 Experiment ID 對照 Ground Truth，評估 Top-1／Top-3 Accuracy、診斷時間與解釋品質。

## 6. 已知限制

1. 本報告以本機 Docker 與外部模擬服務的受控條件為主，尚未使用真實 PLC、工業網路延遲或現場設備資料。
2. 正式資料目前包含 Modbus TCP 四種、MQTT 四種，以及 OPC UA 五種受控情境；尚未建立可重複產生並保存為 `SessionDisconnected` 的正式資料集。
3. Device protocol settings 已可設定 Modbus Slave／Register、OPC UA NodeId／安全連線與 MQTT 帳密等條件；但 MQTT `TopicMismatch` 仍需要 Broker ACL 或其他可明確拒絕訂閱的受控模擬器，才能形成語意正確的正式資料。
4. 某些情境的每 Run 紀錄數不同，原因是輪詢頻率、訊息到達頻率與重試機制不同；比較資料量時應同時報告 Run 數與紀錄數。
5. 實驗資料位於 Docker PostgreSQL Volume；Git commit 保存程式、Compose 與文件，**不會自動保存資料庫內容**。重要研究資料應另外匯出 CSV 並備份。

## 7. 下一步

1. 針對 OPC UA Server 重啟或 Endpoint 不可用情境，設計可控的 100 次 Run `SessionDisconnected` 故障實驗。
2. 建置具 ACL 的 MQTT Simulator，收集語意正確的 `TopicMismatch` 正式資料。
3. 匯出完成的正式資料集，將 MQTT 與未來 OPC UA 資料納入跨協定或分協定的 ML 比較。
4. 以相同 Ground Truth 進行 Rule-based、ML 與 RAG 的公平評估，並記錄正確率、延遲、可解釋性與錯誤案例。

## 8. 重現與查詢

啟動本機服務與 API 後，可使用下列端點查核某個 Experiment：

```powershell
$id = "cb92eac5-0588-4914-9b8b-ef203174150f"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/$id"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/$id/runs"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/$id/records"
```

各情境的建立與 Simulator 切換命令請參閱 [ExperimentCollectionGuide.md](ExperimentCollectionGuide.md)。
