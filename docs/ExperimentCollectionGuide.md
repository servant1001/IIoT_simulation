# 正式 Experiment 資料收集指南

本指南用於蒐集 Phase 8 的正式 Dataset。每次實驗都必須先安排好外部 Simulator 的狀態，再建立 Experiment；`actualFaultType` 是研究者對該次情境設定的 Ground Truth，不能在實驗完成後修改。

## 開始前

1. 啟動 Docker Desktop，執行 `docker compose up -d`，確認 PostgreSQL、Seq、Mosquitto、`modbus-simulator` 均為 `Up`。`modbus-simulator` 由本專案 Docker Compose 提供，不需要另行安裝 Windows GUI 軟體。
2. 啟動 API：`dotnet run --project src/IIoT.FaultDiagnosis.Api -- --urls http://127.0.0.1:5080`。
3. 正式 Experiment 執行期間不要同時啟動 Collector Worker，避免產生不屬於 Experiment 的額外通訊紀錄。
4. 每次只保留一個啟用中的 Device。`tools/Start-Experiment.ps1` 會自動停用其他 Device。
5. 用小型驗證實驗（1 run）確認故障類型正確後，再執行 100 runs 正式實驗。

## 啟動腳本

腳本會建立或更新目標 Device、停用其他 Device、建立 Experiment 並立即開始。

```powershell
.\tools\Start-Experiment.ps1 `
  -Name "MODBUS_NORMAL_001" `
  -ProtocolType 1 `
  -ActualFaultType 0 `
  -TargetHost "127.0.0.1" `
  -Port 502
```

成功後會顯示 `ExperimentId`、`RunsUrl` 與 `RecordsUrl`。查詢完成狀態：

```powershell
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/{ExperimentId}/runs"
Invoke-RestMethod "http://127.0.0.1:5080/api/experiments/{ExperimentId}/records"
```

## FaultType 數值

| FaultType | 值 |
| --- | ---: |
| `None` | 0 |
| `ConnectionRefused` | 1 |
| `Timeout` | 4 |
| `AuthenticationFailed` | 6 |
| `IllegalAddress` | 8 |
| `InvalidEndpoint` | 12 |
| `InvalidNodeId` | 13 |
| `CertificateError` | 14 |
| `SessionDisconnected` | 15 |
| `BrokerUnavailable` | 16 |
| `TopicMismatch` | 17 |
| `PayloadInvalid` | 18 |

## 可直接收集的情境

| 協定 | Ground Truth | Simulator 條件 | 腳本範例 |
| --- | --- | --- | --- |
| Modbus | `None` | `modbus-simulator` 在 `127.0.0.1:1502`、Slave ID 1、Registers 0–5。 | `-ProtocolType 1 -ActualFaultType 0 -TargetHost 127.0.0.1 -Port 1502` |
| Modbus | `ConnectionRefused` | 停止 `modbus-simulator`，仍連到 port 1502。 | `-ProtocolType 1 -ActualFaultType 1 -TargetHost 127.0.0.1 -Port 1502` |
| Modbus | `Timeout` | 啟動 `modbus-timeout-simulator`；它接受 TCP 連線但不回覆 Modbus request。 | `-ProtocolType 1 -ActualFaultType 4 -TargetHost 127.0.0.1 -Port 1503` |
| Modbus | `IllegalAddress` | 啟動 `modbus-illegal-address-simulator`；它會回覆 Modbus Exception Code `02`。 | `-ProtocolType 1 -ActualFaultType 8 -TargetHost 127.0.0.1 -Port 1504` |
| OPC UA | `None` | Prosys Server 開啟，使用預設 endpoint 與 `i=2258`。 | `-ProtocolType 2 -ActualFaultType 0 -TargetHost "opc.tcp://127.0.0.1:53530/OPCUA/SimulationServer" -Port 53530` |
| OPC UA | `InvalidEndpoint` | 設定為不存在的 endpoint path 或已關閉的 endpoint。 | `-ProtocolType 2 -ActualFaultType 12 -TargetHost "opc.tcp://127.0.0.1:53530/invalid" -Port 53530` |
| OPC UA | `SessionDisconnected` | 實驗開始後重啟 Prosys Server。 | 目前僅適合功能驗證：Adapter 會自動重連，重啟後的 Run 可能仍保存為成功 `None`，不可直接視為正式 `SessionDisconnected` Dataset。 |
| OPC UA | `Timeout` | 啟動 `opcua-timeout-simulator`；它接受 TCP 連線但不回覆 OPC UA 封包。 | `-ProtocolType 2 -ActualFaultType 4 -TargetHost "opc.tcp://127.0.0.1:53531" -Port 53531` |
| MQTT | `None` | Mosquitto 運作；持續發布合法 JSON 至 `factory/machine01/data`。 | `-ProtocolType 3 -ActualFaultType 0 -TargetHost 127.0.0.1 -Port 1883` |
| MQTT | `BrokerUnavailable` | 停止 Mosquitto，仍連到 port 1883。 | `-ProtocolType 3 -ActualFaultType 16 -TargetHost 127.0.0.1 -Port 1883` |
| MQTT | `PayloadInvalid` | Mosquitto 運作；持續發布非 JSON 字串至預設 topic。 | `-ProtocolType 3 -ActualFaultType 18 -TargetHost 127.0.0.1 -Port 1883` |
| MQTT | `AuthenticationFailed` | 啟動隔離的帳密 Broker，並讓目標 Device 使用錯誤密碼。 | `-ProtocolType 3 -ActualFaultType 6 -TargetHost 127.0.0.1 -Port 1884` |

MQTT Experiment 使用預設 topic `factory/machine01/data`、QoS 1、JSON payload 驗證。正常情境需要 Publisher 在整個 Experiment 期間持續發布，例如每 0.5 至 1 秒一筆。Payload Invalid 情境則對同一 topic 發送純文字。

啟動 MQTT 正常情境 Publisher：

```powershell
docker compose --profile mqtt-simulators up -d mqtt-json-publisher
```

收集 MQTT `AuthenticationFailed` 時，先啟動隔離的帳密 Broker。`mosquitto-auth-simulator` 的預設研究用帳號是 `iiot-lab`；將目標 Device 的 MQTT username 設為該帳號、password 設為錯誤值，再啟動正式 Experiment。此帳密僅用於本機模擬，不能用於正式環境。

```powershell
docker compose --profile fault-simulators up -d mosquitto-auth-simulator

# 完成後：停止隔離 Broker，移除 Device 帳密並還原至 127.0.0.1:1883
docker compose --profile fault-simulators stop mosquitto-auth-simulator
```

啟動 MQTT `PayloadInvalid` 情境 Publisher 前，先停止正常 JSON Publisher，避免兩種 payload 混入同一個 Experiment；實驗完成後再恢復正常 Publisher：

```powershell
docker compose --profile mqtt-simulators stop mqtt-json-publisher
docker compose --profile mqtt-simulators up -d mqtt-invalid-payload-publisher

# 實驗完成後
docker compose --profile mqtt-simulators stop mqtt-invalid-payload-publisher
docker compose --profile mqtt-simulators up -d mqtt-json-publisher
```

啟動 Modbus Timeout 與 Illegal Address Simulator：

```powershell
docker compose --profile fault-simulators up -d --build `
  modbus-timeout-simulator modbus-illegal-address-simulator
```

啟動 OPC UA Timeout Simulator：

```powershell
docker compose --profile fault-simulators up -d --build opcua-timeout-simulator
```

## 目前需要補強的正式情境

Device protocol settings 已可覆寫 Modbus Slave／Register、OPC UA NodeId／安全連線與 MQTT topic／帳密；`InvalidNodeId`、`CertificateError` 與 `AuthenticationFailed` 均已完成正式 100 Run 實驗。`WrongSlaveId` 亦可透過 Device settings 收集，但仍需搭配可辨識的 Modbus Simulator 條件。

`TopicMismatch` 仍不應只以「沒有收到訊息」標示：需要 Broker ACL 或可控 Broker 回應，明確拒絕訂閱或 topic 存取，才可形成語意正確的正式資料。

另外，OPC UA `SessionDisconnected` 雖可透過重啟 Prosys Server 驗證 Adapter 的重連行為，但目前 Collector 會在同一 Run 內重連並回傳成功資料。正式蒐集前需要可控制、可重複產生 Session／Secure Channel 中斷的 Simulator，或讓 Experiment 保存重連前的中斷事件。

不要將單次 `/test`、`/test-opc-ua`、`/test-mqtt` 的紀錄混入正式 Dataset。

## 實驗結束後

1. 確認所有 Run 的狀態與通訊 FaultType 符合 Ground Truth。
2. 對需要比較的紀錄執行 Rule-based 與 RAG diagnosis。
3. 查詢 Dataset coverage：

```powershell
Invoke-RestMethod "http://127.0.0.1:5080/api/datasets/communication-records/summary?minimumRunsPerFault=100"
```

4. 僅在 `remainingRunCount` 為 0 的組合匯出正式 Dataset。
