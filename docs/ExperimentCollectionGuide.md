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
| `NetworkDelay` | 19 |

## 可直接收集的情境

| 協定 | Ground Truth | Simulator 條件 | 腳本範例 |
| --- | --- | --- | --- |
| Modbus | `None` | `modbus-simulator` 在 `127.0.0.1:1502`、Slave ID 1、Registers 0–5。 | `-ProtocolType 1 -ActualFaultType 0 -TargetHost 127.0.0.1 -Port 1502` |
| Modbus | `ConnectionRefused` | 停止 `modbus-simulator`，仍連到 port 1502。 | `-ProtocolType 1 -ActualFaultType 1 -TargetHost 127.0.0.1 -Port 1502` |
| Modbus | `Timeout` | 啟動 `modbus-timeout-simulator`；它接受 TCP 連線但不回覆 Modbus request。 | `-ProtocolType 1 -ActualFaultType 4 -TargetHost 127.0.0.1 -Port 1503` |
| Modbus | `IllegalAddress` | 啟動 `modbus-illegal-address-simulator`；它會回覆 Modbus Exception Code `02`。 | `-ProtocolType 1 -ActualFaultType 8 -TargetHost 127.0.0.1 -Port 1504` |
| Modbus | `NetworkDelay`（泛化驗證） | 啟動 `modbus-delay-simulator`；它轉送正常 Modbus 回覆，但依序延遲 150、300、450、600、750 ms。 | `-ProtocolType 1 -ActualFaultType 19 -TargetHost 127.0.0.1 -Port 1505` |
| OPC UA | `None` | Prosys Server 開啟，使用預設 endpoint 與 `i=2258`。 | `-ProtocolType 2 -ActualFaultType 0 -TargetHost "opc.tcp://127.0.0.1:53530/OPCUA/SimulationServer" -Port 53530` |
| OPC UA | `InvalidEndpoint` | 設定為不存在的 endpoint path 或已關閉的 endpoint。 | `-ProtocolType 2 -ActualFaultType 12 -TargetHost "opc.tcp://127.0.0.1:53530/invalid" -Port 53530` |
| OPC UA | `SessionDisconnected` | 啟動 Prosys 與 Session Disconnect Proxy；Proxy 在 Session 建立後關閉 transport。 | `-ProtocolType 2 -ActualFaultType 15 -TargetHost "opc.tcp://127.0.0.1:53532/OPCUA/SimulationServer" -Port 53532 -ProtocolSettingsJson $sessionSettings` |
| OPC UA | `Timeout` | 啟動 `opcua-timeout-simulator`；它接受 TCP 連線但不回覆 OPC UA 封包。 | `-ProtocolType 2 -ActualFaultType 4 -TargetHost "opc.tcp://127.0.0.1:53531" -Port 53531` |
| MQTT | `None` | Mosquitto 運作；持續發布合法 JSON 至 `factory/machine01/data`。 | `-ProtocolType 3 -ActualFaultType 0 -TargetHost 127.0.0.1 -Port 1883` |
| MQTT | `BrokerUnavailable` | 停止 Mosquitto，仍連到 port 1883。 | `-ProtocolType 3 -ActualFaultType 16 -TargetHost 127.0.0.1 -Port 1883` |
| MQTT | `PayloadInvalid` | Mosquitto 運作；持續發布非 JSON 字串至預設 topic。 | `-ProtocolType 3 -ActualFaultType 18 -TargetHost 127.0.0.1 -Port 1883` |
| MQTT | `AuthenticationFailed` | 啟動隔離的帳密 Broker，並讓目標 Device 使用錯誤密碼。 | `-ProtocolType 3 -ActualFaultType 6 -TargetHost 127.0.0.1 -Port 1884` |
| MQTT | `TopicMismatch` | 啟動 MQTT 5 Topic Rejection Simulator；它接受連線、拒絕訂閱。 | `-ProtocolType 3 -ActualFaultType 17 -TargetHost 127.0.0.1 -Port 1885` |

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

收集 MQTT `TopicMismatch` 時，啟動 Topic Rejection Simulator。它會對所有訂閱回覆 MQTT 5 `NotAuthorized` SUBACK，讓 Adapter 將訂閱遭拒正規化為 `TopicMismatch`；它不是一般 Publisher 未送訊息的 Timeout 情境。

```powershell
docker compose --profile fault-simulators up -d --build mqtt-topic-reject-simulator

# 實驗完成後：停止 Simulator 並還原 Device 至 127.0.0.1:1883
docker compose --profile fault-simulators stop mqtt-topic-reject-simulator
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
  modbus-timeout-simulator modbus-illegal-address-simulator modbus-delay-simulator
```

`modbus-delay-simulator` 用於未見網路條件驗證：通訊能成功、Adapter 的 `observed_fault_type` 仍為 `None`，但 Experiment 的 Ground Truth 為 `NetworkDelay`。因此它不應混入 Phase 8 的 15 種正式訓練快照；應獨立保留為泛化評估資料，檢驗診斷方法是否能處理尚未納入規則、知識庫或訓練資料的延遲型態。

啟動 OPC UA Timeout Simulator：

```powershell
docker compose --profile fault-simulators up -d --build opcua-timeout-simulator
```

收集 OPC UA `SessionDisconnected` 時，先保持 Prosys Server 開啟，再啟動 TCP Proxy。此情境用讀取前延遲確保 Session 已建立，用關閉當次自動重連確保 Collector 保存 transport 中斷事件；完成後要還原正常 Device 設定。

```powershell
docker compose --profile fault-simulators up -d --build opcua-session-disconnect-proxy

$sessionSettings = @{
  opcUaNodeIds = @('i=2258')
  opcUaReadDelayMs = 2000
  opcUaReconnectOnSessionDisconnect = $false
} | ConvertTo-Json -Depth 3 -Compress

.\tools\Start-Experiment.ps1 `
  -Name "OPCUA_SESSION_DISCONNECTED_100RUN" `
  -ProtocolType 2 `
  -ActualFaultType 15 `
  -TargetHost "opc.tcp://127.0.0.1:53532/OPCUA/SimulationServer" `
  -Port 53532 `
  -ProtocolSettingsJson $sessionSettings

# 實驗完成後：停止 Proxy，並將 Device 還原為正常設定。
docker compose --profile fault-simulators stop opcua-session-disconnect-proxy
```

## 目前需要補強的正式情境

Device protocol settings 已可覆寫 Modbus Slave／Register、OPC UA NodeId／安全連線與 MQTT topic／帳密；`InvalidNodeId`、`CertificateError`、`AuthenticationFailed` 與 `TopicMismatch` 均已完成正式 100 Run 實驗。`WrongSlaveId` 亦可透過 Device settings 收集，但仍需搭配可辨識的 Modbus Simulator 條件。

不要將單次 `/test`、`/test-opc-ua`、`/test-mqtt` 的紀錄混入正式 Dataset。

## 實驗結束後

1. 確認所有 Run 的狀態與通訊 FaultType 符合 Ground Truth。
2. 對需要比較的紀錄執行 Rule-based 與 RAG diagnosis。
3. 查詢 Dataset coverage：

```powershell
Invoke-RestMethod "http://127.0.0.1:5080/api/datasets/communication-records/summary?minimumRunsPerFault=100"
```

4. 僅在 `remainingRunCount` 為 0 的組合匯出正式 Dataset。
