# IIoT Communication Fault Diagnosis Lab
## Codex 實作計畫書（Implementation Plan）

> 目的：建立一套可在單機 Windows 環境執行的「工業物聯網通訊異常研究平台」，以虛擬設備取代實體 PLC / Sensor，支援 Modbus TCP、OPC UA、MQTT，蒐集通訊成功與異常資料，作為後續 Rule-based、Machine Learning、LLM / RAG 故障診斷研究之基礎。

---

# 1. 專案背景

本專案預計作為資訊工程碩士在職專班論文的實驗平台。

研究情境聚焦於智慧製造 / IIoT 系統中常見的設備通訊問題，例如：

- Modbus TCP 無法連線
- Connection Refused
- Timeout
- Slave ID 錯誤
- Register Address 錯誤
- Data Type 設定錯誤
- 回傳資料長度異常
- OPC UA Endpoint 錯誤
- OPC UA NodeId 錯誤
- OPC UA Session Disconnect
- MQTT Broker Down
- MQTT Authentication Failed
- MQTT Topic 錯誤
- MQTT Client Disconnect
- Network Delay
- Packet Loss
- Server Restart
- 資料格式錯誤

研究平台需可以「可重複地製造異常」，並完整紀錄：

1. 輸入設定
2. 通訊協定
3. 測試案例
4. 發生時間
5. Response Time
6. 是否成功
7. Error Message
8. Error Type
9. Retry Count
10. CPU / Memory
11. 原始 Payload / Response
12. 診斷結果

---

# 2. 研究目標

第一階段先完成可穩定執行的 IIoT Communication Test Platform。

平台最終需支援：

```text
Virtual Device / Simulator
        |
        v
Communication Adapter
        |
        v
Collector Service
        |
        +------> Database
        |
        +------> Structured Log
        |
        +------> Seq
        |
        v
Fault Diagnosis Engine
        |
        +------> Rule-based
        +------> Machine Learning
        +------> LLM / RAG（後續階段）
```

主要研究問題可包含：

1. 不同工業通訊協定在異常情境下的錯誤特徵是否具有可辨識差異？
2. Rule-based 方法能否有效分類常見 IIoT 通訊故障？
3. Machine Learning 是否能提升故障分類正確率？
4. LLM / RAG 是否能提升故障原因說明與排查建議品質？
5. 不同方法在 Accuracy、F1、Top-N Accuracy、診斷時間上的差異為何？

---

# 3. 技術選型

## 3.1 開發平台

- OS：Windows 10 / Windows 11
- Language：C#
- Runtime：.NET 8
- IDE：Visual Studio / VS Code / Codex
- Architecture：Clean Architecture + Modular Monolith
- Dependency Injection：Microsoft.Extensions.DependencyInjection
- Logging：Serilog
- Structured Log Server：Seq
- Database：PostgreSQL
- ORM：Entity Framework Core
- Configuration：appsettings.json
- API：ASP.NET Core Web API

---

## 3.2 工業通訊

### Modbus TCP

優先考慮套件：

- NModbus

用途：

- 建立 Modbus TCP Client
- 讀取 Holding Registers
- 讀取 Input Registers
- 讀取 Coils
- 讀取 Discrete Inputs

第一階段只需要 Client，不需要自己實作 Server。

Server 使用外部 Modbus Simulator。

---

### OPC UA

優先考慮：

- OPCFoundation.NetStandard.Opc.Ua

用途：

- 建立 OPC UA Client
- Connect / Disconnect
- Read Node
- Subscription
- Session Monitor
- Reconnect

Server 使用：

- Prosys OPC UA Simulation Server

---

### MQTT

優先考慮：

- MQTTnet

Broker：

- Eclipse Mosquitto

測試 Client / Publisher：

- MQTTX

用途：

- Connect
- Subscribe
- Receive Message
- Reconnect
- QoS Test
- Authentication Test

---

# 4. 外部模擬工具

本專案不依賴實際工廠設備。

建議使用：

## Modbus

可使用任何支援 Modbus TCP Server / Slave 的模擬工具。

初始設定：

```text
Host: 127.0.0.1
Port: 502
Slave ID: 1
```

Registers：

```text
Holding Register 0 = Temperature
Holding Register 1 = Pressure
Holding Register 2 = RPM
Holding Register 3 = Current
Holding Register 4 = ProductionCount
Holding Register 5 = MachineStatus
```

---

## OPC UA

使用：

```text
Prosys OPC UA Simulation Server
```

建議建立：

```text
Factory
 ├─ Machine01
 │   ├─ Temperature
 │   ├─ Pressure
 │   ├─ RPM
 │   ├─ Current
 │   └─ Status
 ├─ Machine02
 └─ Machine03
```

---

## MQTT

Broker：

```text
Eclipse Mosquitto
```

預設：

```text
Host: localhost
Port: 1883
```

Topic：

```text
factory/machine01/data
factory/machine02/data
factory/machine03/data
```

Payload：

```json
{
  "machineId": "MC001",
  "temperature": 56.2,
  "pressure": 80.5,
  "rpm": 2230,
  "current": 26.1,
  "status": "RUN",
  "timestamp": "2026-09-10T14:00:00+08:00"
}
```

---

# 5. Solution 架構

建立：

```text
IIoT.FaultDiagnosis.sln
```

建議 Project：

```text
src/
├─ IIoT.FaultDiagnosis.Api
├─ IIoT.FaultDiagnosis.Application
├─ IIoT.FaultDiagnosis.Domain
├─ IIoT.FaultDiagnosis.Infrastructure
├─ IIoT.FaultDiagnosis.Protocols
│  ├─ Modbus
│  ├─ OpcUa
│  └─ Mqtt
└─ IIoT.FaultDiagnosis.Worker

tests/
├─ IIoT.FaultDiagnosis.UnitTests
└─ IIoT.FaultDiagnosis.IntegrationTests

docs/
├─ architecture.md
├─ experiment-design.md
├─ fault-catalog.md
└─ database-schema.md
```

---

# 6. 各 Project 職責

## IIoT.FaultDiagnosis.Domain

只放核心 Domain Model。

不可依賴：

- EF Core
- MQTTnet
- NModbus
- OPC UA SDK
- ASP.NET Core

主要 Entity：

```text
Device
ProtocolConfiguration
Experiment
ExperimentRun
CommunicationRecord
FaultDefinition
DiagnosisResult
SystemMetric
```

Enums：

```text
ProtocolType
FaultType
ExperimentStatus
CommunicationStatus
DiagnosisMethod
```

---

## IIoT.FaultDiagnosis.Application

負責 Use Case。

Services：

```text
IDeviceService
IExperimentService
ICollectorService
IDiagnosisService
IMetricService
```

Commands / Queries：

```text
CreateDevice
StartExperiment
StopExperiment
RunSingleCollection
GetExperimentResult
GetCommunicationRecords
DiagnoseFault
```

---

## IIoT.FaultDiagnosis.Protocols

定義統一 Protocol Adapter。

Interface：

```csharp
public interface IProtocolAdapter
{
    ProtocolType ProtocolType { get; }

    Task<ConnectionResult> ConnectAsync(
        Device device,
        CancellationToken cancellationToken);

    Task<CollectionResult> CollectAsync(
        Device device,
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        Device device,
        CancellationToken cancellationToken);
}
```

實作：

```text
ModbusProtocolAdapter
OpcUaProtocolAdapter
MqttProtocolAdapter
```

每個 Adapter 必須將各協定原始 Exception 正規化為統一 FaultType。

例如：

```text
SocketException
        ↓
ConnectionRefused

TimeoutException
        ↓
Timeout

Modbus Exception Code 02
        ↓
IllegalAddress
```

---

# 7. Domain Model

## Device

```text
Id
Name
ProtocolType
Host
Port
IsEnabled
CreatedAt
UpdatedAt
```

---

## ProtocolConfiguration

可以用 JSON 儲存不同協定的設定。

例如 Modbus：

```json
{
  "slaveId": 1,
  "functionCode": 3,
  "startAddress": 0,
  "length": 6,
  "pollingIntervalMs": 1000
}
```

OPC UA：

```json
{
  "endpoint": "opc.tcp://127.0.0.1:53530/OPCUA/SimulationServer",
  "securityMode": "None",
  "nodeIds": [
    "ns=3;s=Machine01.Temperature"
  ],
  "pollingIntervalMs": 1000
}
```

MQTT：

```json
{
  "clientId": "iiot-test-client",
  "topic": "factory/machine01/data",
  "username": "",
  "qos": 1
}
```

---

# 8. CommunicationRecord

這是整篇研究最重要的資料表之一。

欄位：

```text
Id
ExperimentId
ExperimentRunId
DeviceId
ProtocolType
StartedAt
CompletedAt
ResponseTimeMs
Status
FaultType
ErrorCode
ErrorMessage
RetryCount
RawRequest
RawResponse
ParsedValue
CreatedAt
```

Status：

```text
Success
Failed
Timeout
Cancelled
```

---

# 9. FaultType

第一版先定義：

```text
None

ConnectionRefused
ConnectionReset
ConnectionLost
Timeout

WrongPort
AuthenticationFailed

WrongSlaveId
IllegalAddress
IllegalFunction
InvalidResponseLength
InvalidDataType

InvalidEndpoint
InvalidNodeId
CertificateError
SessionDisconnected

BrokerUnavailable
TopicMismatch
PayloadInvalid

NetworkDelay
PacketLoss
ServerRestart

Unknown
```

注意：

不要只依賴 ErrorMessage 字串做分類。

Protocol Adapter 應優先根據：

1. Exception Type
2. Protocol Error Code
3. Socket Error Code
4. Response內容

最後才 fallback 到文字比對。

---

# 10. Experiment

Experiment 代表一次研究設計。

例如：

```text
Experiment Name:
MODBUS_TIMEOUT_001

Protocol:
ModbusTCP

Fault:
Timeout

Device Count:
10

Tag Count:
6

Polling Interval:
1000 ms

Duration:
10 minutes

Repeat:
10
```

Entity：

```text
Id
Name
ProtocolType
FaultType
DeviceCount
TagCount
PollingIntervalMs
DurationSeconds
RepeatCount
Status
CreatedAt
```

---

# 11. ExperimentRun

每次真正執行產生一筆 Run。

```text
Id
ExperimentId
RunNumber
StartedAt
CompletedAt
SuccessCount
FailureCount
AverageResponseTimeMs
MaxResponseTimeMs
MinResponseTimeMs
CpuAverage
MemoryAverageMb
Status
```

---

# 12. SystemMetric

每 1~5 秒收集一次。

欄位：

```text
Id
ExperimentRunId
Timestamp
CpuPercent
MemoryMb
ThreadCount
GcHeapMb
HandleCount
```

第一版只要求：

```text
CPU
Memory
```

---

# 13. Logging

使用 Serilog。

Console + File + Seq。

Log Format 必須 Structured Logging。

禁止：

```csharp
_log.LogError("設備出錯 " + ex.Message);
```

使用：

```csharp
_log.LogError(
    ex,
    "Communication failed. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}",
    device.Id,
    device.ProtocolType,
    faultType);
```

Seq properties 至少包含：

```text
ExperimentId
ExperimentRunId
DeviceId
Protocol
FaultType
ResponseTimeMs
```

---

# 14. 第一階段 MVP

第一階段只做：

```text
Modbus TCP
```

不要立即做 OPC UA / MQTT。

MVP Flow：

```text
Modbus Simulator
      |
      v
ModbusProtocolAdapter
      |
      v
CollectorService
      |
      +------> PostgreSQL
      |
      +------> Serilog
      |
      +------> Seq
```

---

# 15. 第一階段 API

## Device

```http
POST /api/devices
GET  /api/devices
GET  /api/devices/{id}
PUT  /api/devices/{id}
DELETE /api/devices/{id}
```

---

## Test Connection

```http
POST /api/devices/{id}/test
```

Response：

```json
{
  "success": true,
  "responseTimeMs": 12,
  "faultType": "None",
  "message": "Connection successful"
}
```

---

## Experiment

```http
POST /api/experiments
POST /api/experiments/{id}/start
POST /api/experiments/{id}/stop
GET  /api/experiments/{id}
GET  /api/experiments/{id}/runs
GET  /api/experiments/{id}/records
```

---

# 16. Modbus MVP 測試情境

至少完成：

## NORMAL

```text
Host = 127.0.0.1
Port = 502
SlaveId = 1
Address = 0
```

預期：

```text
Success
```

---

## CONNECTION_REFUSED

方法：

```text
停止 Modbus Simulator
```

預期：

```text
FaultType = ConnectionRefused
```

---

## WRONG_PORT

設定：

```text
Port = 503
```

預期：

```text
ConnectionRefused
或 Timeout
```

依 OS / 網路結果記錄實際 Fault。

---

## WRONG_SLAVE_ID

```text
SlaveId = 2
```

預期：

```text
WrongSlaveId / Timeout
```

注意：

不同 Simulator 行為可能不同，因此研究資料需保存原始 Exception。

---

## ILLEGAL_ADDRESS

讀取不存在的：

```text
StartAddress = 50000
```

預期：

```text
FaultType = IllegalAddress
```

---

## TIMEOUT

透過：

```text
Firewall
Network Emulator
或 Server 不回應
```

製造 Timeout。

預期：

```text
FaultType = Timeout
```

---

# 17. Collector Worker

Worker 必須支援：

```text
Start
Stop
CancellationToken
Polling Interval
Retry
```

Pseudo：

```text
while (!cancellationToken.IsCancellationRequested)
{
    foreach (var device in enabledDevices)
    {
        result = await adapter.CollectAsync(device);

        save result;
        write structured log;
    }

    await Delay(pollingInterval);
}
```

注意：

不同設備不可因單一設備異常導致全部停止。

每個 device collection 必須獨立 try/catch。

---

# 18. Retry Policy

第一版：

```text
Max Retry = 3
Retry Delay = 1000 ms
```

未來可加入：

```text
Exponential Backoff
```

Retry 資訊必須存進：

```text
CommunicationRecord.RetryCount
```

---

# 19. Fault Diagnosis Engine

第一階段先實作 Rule-based。

Interface：

```csharp
public interface IFaultDiagnosisEngine
{
    Task<DiagnosisResult> DiagnoseAsync(
        CommunicationRecord record,
        CancellationToken cancellationToken);
}
```

第一版：

```text
RuleBasedFaultDiagnosisEngine
```

未來：

```text
MachineLearningFaultDiagnosisEngine
LlmFaultDiagnosisEngine
RagFaultDiagnosisEngine
```

---

# 20. DiagnosisResult

```text
Id
CommunicationRecordId
DiagnosisMethod
PredictedFaultType
Confidence
PossibleCauses
SuggestedActions
DiagnosisDurationMs
CreatedAt
```

PossibleCauses / SuggestedActions 可以 JSON 儲存。

---

# 21. Rule-based Diagnosis

第一版規則範例：

```text
SocketError.ConnectionRefused
→ ConnectionRefused

TimeoutException
→ Timeout

Modbus Exception Code 02
→ IllegalAddress

OPC UA BadNodeIdUnknown
→ InvalidNodeId

MQTT authentication failure
→ AuthenticationFailed
```

避免將規則全部 hardcode 在單一 service。

建議：

```text
IFaultRule

ConnectionRefusedRule
TimeoutRule
IllegalAddressRule
...
```

---

# 22. Database

使用：

```text
PostgreSQL
```

EF Core Migration。

初始 Tables：

```text
devices
experiments
experiment_runs
communication_records
system_metrics
diagnosis_results
```

所有時間：

```text
UTC
```

UI / Report 再轉 Asia/Taipei。

---

# 23. Docker Compose

建議專案加入：

```text
docker-compose.yml
```

第一版至少包含：

```text
PostgreSQL
Seq
Mosquitto
```

之後再加入：

```text
Grafana
Prometheus
```

注意：

Modbus Simulator 與 Prosys OPC UA Simulation Server 初期可留在 Windows Host 執行。

---

# 24. 設定檔

`appsettings.Development.json`

範例：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=iiot_fault_lab;Username=postgres;Password=postgres"
  },
  "Seq": {
    "Url": "http://localhost:5341"
  },
  "Collector": {
    "DefaultPollingIntervalMs": 1000,
    "MaxRetry": 3,
    "RetryDelayMs": 1000
  }
}
```

敏感資訊不可 commit。

建立：

```text
appsettings.Local.json
```

加入 `.gitignore`。

---

# 25. 實驗資料輸出

必須提供 CSV Export。

API：

```http
GET /api/experiments/{id}/export
```

輸出至少：

```text
experiment_id
run_id
device_id
protocol
timestamp
response_time_ms
status
fault_type
error_code
error_message
retry_count
cpu_percent
memory_mb
```

後續研究可以直接用：

```text
Python
Jupyter
Pandas
Scikit-learn
```

分析。

---

# 26. 統計資料

Experiment 結束後產生：

```text
Total Requests
Success Count
Failure Count
Success Rate
Error Rate

Average Response Time
P50
P95
P99
Max Response Time

Average CPU
Max CPU

Average Memory
Max Memory
```

---

# 27. 測試

## Unit Test

至少覆蓋：

```text
FaultType mapping
Rule-based diagnosis
Experiment statistics
Protocol configuration validation
```

---

## Integration Test

使用：

```text
Docker PostgreSQL
```

至少測：

```text
Create Device
Save CommunicationRecord
Create Experiment
Create ExperimentRun
DiagnosisResult persistence
```

---

# 28. Coding Rules

Codex 開發時遵循：

1. 使用 .NET 8
2. 開啟 Nullable
3. 開啟 ImplicitUsings
4. 所有非同步 IO 使用 async / await
5. 所有可取消操作支援 CancellationToken
6. 不允許 `.Result`
7. 不允許 `.Wait()`
8. 不要吞 Exception
9. Logging 使用 structured logging
10. Domain 不依賴 Infrastructure
11. Protocol SDK 不可洩漏到 Application / Domain
12. DTO 與 Entity 分離
13. Controller 不放 business logic
14. 所有公共 API 做 Validation
15. 每完成一個 Task 必須確保 Solution 可 build
16. 不為了「先跑起來」寫大量 TODO
17. 不要過度設計，先完成 MVP

---

# 29. Git Strategy

Branch：

```text
main
develop
feature/*
```

Commit 建議：

```text
feat: initialize solution structure
feat: add device domain model
feat: implement modbus protocol adapter
feat: add communication record persistence
feat: add experiment worker
feat: add rule based diagnosis
test: add fault classification tests
docs: update experiment setup
```

---

# 30. 實作階段

## Phase 0 - Project Bootstrap

目標：

建立可以正常 build 的 Solution。

Tasks：

- [ ] 建立 Solution
- [ ] 建立 Projects
- [ ] 設定 Project References
- [ ] 安裝必要 NuGet Packages
- [ ] 建立 `.gitignore`
- [ ] 建立 README
- [ ] 加入 Docker Compose
- [ ] PostgreSQL 啟動成功
- [ ] Seq 啟動成功
- [ ] API 啟動成功

完成條件：

```bash
dotnet build
```

必須成功。

---

# 31. Phase 1 - Domain / Database

Tasks：

- [ ] Device
- [ ] Experiment
- [ ] ExperimentRun
- [ ] CommunicationRecord
- [ ] SystemMetric
- [ ] DiagnosisResult
- [ ] DbContext
- [ ] EF Core Mapping
- [ ] Migration
- [ ] PostgreSQL schema

Acceptance Criteria：

可以：

```text
Create Device
Read Device
Update Device
Delete Device
```

---

# 32. Phase 2 - Modbus Adapter

Tasks：

- [ ] Install NModbus
- [ ] Modbus configuration model
- [ ] Connection validation
- [ ] Read Holding Registers
- [ ] Measure response time
- [ ] Exception normalization
- [ ] Structured logging
- [ ] Save CommunicationRecord

Acceptance：

正常 Simulator：

```text
Success = true
```

關閉 Simulator：

```text
Success = false
FaultType != None
```

---

# 33. Phase 3 - Collector Worker

Tasks：

- [ ] BackgroundService
- [ ] Poll enabled devices
- [ ] Cancellation
- [ ] Retry
- [ ] Independent device execution
- [ ] Experiment association
- [ ] Metrics
- [ ] Graceful Shutdown

Acceptance：

連續執行：

```text
30 minutes
```

不得因單次設備錯誤停止。

---

# 34. Phase 4 - Experiment Engine

Tasks：

- [ ] Create Experiment
- [ ] Start Experiment
- [ ] Stop Experiment
- [ ] Repeat Run
- [ ] Statistics
- [ ] Export CSV
- [ ] API

必須能跑：

```text
NORMAL
CONNECTION_REFUSED
ILLEGAL_ADDRESS
TIMEOUT
```

---

# 35. Phase 5 - Rule-based Diagnosis

Tasks：

- [ ] IFaultDiagnosisEngine
- [ ] IFaultRule
- [ ] ConnectionRefusedRule
- [ ] TimeoutRule
- [ ] IllegalAddressRule
- [ ] UnknownRule
- [ ] Diagnosis persistence
- [ ] Evaluation result

Acceptance：

預先標記的 fault label 與診斷結果可以比較：

```text
ActualFaultType
PredictedFaultType
```

---

# 36. Phase 6 - OPC UA

第一版 Modbus 完成後才開始。

Tasks：

- [ ] OPC UA Adapter
- [ ] Session Management
- [ ] Read Node
- [ ] Reconnect
- [ ] Error normalization
- [ ] OPC UA test scenarios

Fault：

```text
InvalidEndpoint
InvalidNodeId
CertificateError
SessionDisconnected
Timeout
```

---

# 37. Phase 7 - MQTT

Tasks：

- [ ] MQTTnet
- [ ] MQTT Adapter
- [ ] Mosquitto configuration
- [ ] Subscribe
- [ ] Message persistence
- [ ] Reconnect
- [ ] QoS
- [ ] Error normalization

Fault：

```text
BrokerUnavailable
AuthenticationFailed
TopicMismatch
PayloadInvalid
ConnectionLost
```

---

# 38. Phase 8 - Dataset

建立正式 Dataset。

每個 Fault 至少：

```text
100 runs
```

最好：

```text
500~1000 records
```

Dataset 必須保存 Ground Truth：

```text
ActualFaultType
```

與：

```text
PredictedFaultType
```

分開。

不可用 predicted value 覆寫 ground truth。

---

# 39. Phase 9 - Machine Learning

此階段不是第一版 MVP 必須完成。

Python pipeline：

```text
export CSV
    |
    v
Jupyter
    |
    v
Preprocessing
    |
    v
Train / Validation / Test
    |
    v
Classifier
```

可比較：

```text
Logistic Regression
Random Forest
XGBoost
SVM
```

Metrics：

```text
Accuracy
Precision
Recall
F1
Confusion Matrix
```

---

# 40. Phase 10 - LLM / RAG

最後才做。

Input：

```text
Protocol
Configuration
ErrorMessage
ErrorCode
ResponseTime
RetryCount
Historical Cases
```

Output：

```json
{
  "faultType": "IllegalAddress",
  "possibleCauses": [
    "Register address exceeds device supported range"
  ],
  "suggestedActions": [
    "Confirm register mapping",
    "Test the same address with a known Modbus client"
  ]
}
```

評估：

```text
Top-1 Accuracy
Top-3 Accuracy
Diagnosis Time
Explanation Quality
Hallucination Rate
```

---

# 41. 建議的第一組實驗

Codex 完成 Modbus MVP 後：

```text
Experiment:
MODBUS_NORMAL_001

Host:
127.0.0.1

Port:
502

Slave:
1

Function:
03

Address:
0

Length:
6

Polling:
1000 ms

Duration:
300 sec
```

接著：

```text
MODBUS_CONNECTION_REFUSED_001
```

方法：

停止 Simulator。

最後：

```text
MODBUS_ILLEGAL_ADDRESS_001
```

Address 設定為不存在的位置。

---

# 42. 論文研究資料不可污染

非常重要。

系統需區分：

```text
Ground Truth
```

與：

```text
Diagnosis
```

Experiment 設定的 FaultType：

```text
ActualFaultType
```

屬於 Ground Truth。

Diagnosis Engine：

```text
PredictedFaultType
```

屬於預測結果。

兩者禁止共用欄位。

---

# 43. 可重複性

為符合學術研究要求，每個 Experiment 必須保存：

```text
App Version
Git Commit
OS Version
Protocol
Protocol Library Version
Device Configuration
Fault Configuration
Start Time
Duration
Polling Interval
Machine CPU
Machine RAM
```

確保其他人可以重複實驗。

---

# 44. Definition of Done

每個 Codex Task 完成前：

- [ ] Code 可以 build
- [ ] 沒有 compiler error
- [ ] 新增功能有測試
- [ ] API 有基本錯誤處理
- [ ] 沒有 hardcoded password
- [ ] 沒有把 domain logic 寫進 Controller
- [ ] Logging 是 structured logging
- [ ] README / docs 有更新
- [ ] 不破壞既有功能

---

# 45. Codex 執行方式

Codex 不要一次完成全部 Phase。

每次只執行一個 Phase。

建議順序：

```text
Phase 0
↓
Phase 1
↓
Phase 2
↓
Phase 3
↓
Phase 4
↓
Phase 5
↓
Phase 6
↓
Phase 7
↓
Phase 8
↓
Phase 9
↓
Phase 10
```

每個 Phase 開始前：

1. 閱讀本文件
2. 閱讀現有 Solution
3. 確認已有功能
4. 不重複實作
5. 列出實作計畫
6. 再修改程式

完成後：

1. 執行 `dotnet build`
2. 執行 `dotnet test`
3. 回報變更檔案
4. 回報測試結果
5. 回報尚未完成事項
6. 不擅自開始下一個 Phase

---

# 46. Codex 第一個 Prompt

建立新 Repository 後，可以直接給 Codex：

```text
請閱讀 docs/ImplementationPlan.md。

目前要執行 Phase 0 - Project Bootstrap。

請先分析需求並列出本次實作計畫，
再依計畫建立 .NET 8 Solution 與專案結構。

要求：

1. 僅執行 Phase 0。
2. 不要提前實作 Modbus、OPC UA、MQTT。
3. 使用 Clean Architecture 的依賴方向。
4. 加入 PostgreSQL 與 Seq 的 docker-compose.yml。
5. 建立基本 ASP.NET Core Web API。
6. 完成後執行 dotnet build。
7. 若有測試專案，同時執行 dotnet test。
8. 更新 README.md，說明如何啟動開發環境。
9. 最後整理：
   - 新增/修改檔案
   - Architecture
   - Build Result
   - Test Result
   - 尚未完成項目

不要開始 Phase 1。
```

---

# 47. 最終預期成果

完成全部研究平台後，應形成：

```text
                 +----------------+
                 | Virtual Factory|
                 +-------+--------+
                         |
         +---------------+---------------+
         |               |               |
      Modbus           OPC UA           MQTT
         |               |               |
         +---------------+---------------+
                         |
                         v
                IIoT Collector
                         |
                         v
               Communication Log
                         |
             +-----------+-----------+
             |           |           |
             v           v           v
          Rules          ML        LLM/RAG
             |           |           |
             +-----------+-----------+
                         |
                         v
                  Evaluation
                         |
                         v
              Master's Thesis Data
```

這套平台的第一優先目標不是「功能很多」，而是：

> 可控、可重複、可量測、可產生可信的研究資料。

研究平台完成後，再逐步增加協定與診斷方法。
