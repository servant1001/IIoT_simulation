# IIoT Communication Fault Diagnosis Lab

用於建立與驗證工業物聯網（IIoT）通訊異常的本機研究平台。專案以虛擬設備取代實體 PLC，蒐集 Modbus TCP 與 OPC UA 的正常及異常通訊紀錄，提供實驗執行、CSV 匯出與規則式故障診斷，作為後續 Dataset、Machine Learning 與 LLM/RAG 研究的資料基礎。

原始實作規劃位於 [docs/ImplementationPlan_IIoT_FaultDiagnosis.md](docs/ImplementationPlan_IIoT_FaultDiagnosis.md)。目前已完成 **Phase 0 至 Phase 6**；MQTT 與後續階段尚未開始。

## 已完成功能

| 範圍 | 功能 |
| --- | --- |
| 專案基礎 | .NET 8 Clean Architecture、PostgreSQL、Seq、Serilog、Health Check |
| Device | 設備 CRUD、協定、Host、Port 與啟用狀態管理 |
| Modbus TCP | Function 03 Holding Register 讀取、連線測試、retry、錯誤正規化 |
| Collector | 背景輪詢、通訊紀錄、系統 CPU／記憶體指標、取消與停止處理 |
| Experiment | 建立、背景執行、停止、Run 統計、通訊紀錄查詢與 CSV 匯出 |
| Diagnosis | Rule-based 診斷、診斷結果保存與實驗評估 |
| OPC UA | Session 管理、多 Node 讀取、斷線後重連、錯誤正規化與 Prosys 外部驗證 |

## 架構

```text
IIoT.FaultDiagnosis.Domain          實體、列舉與商業規則
IIoT.FaultDiagnosis.Application     Use cases、DTO、Repository abstraction、Collector、Diagnosis
IIoT.FaultDiagnosis.Protocols       Modbus TCP 與 OPC UA Adapter
IIoT.FaultDiagnosis.Infrastructure  EF Core、PostgreSQL、Repository 實作
IIoT.FaultDiagnosis.Api             ASP.NET Core Web API
IIoT.FaultDiagnosis.Worker          背景 Collector Worker
tests/                               Unit 與 PostgreSQL-backed Integration Tests
```

依賴方向由外層指向內層；Domain 不相依 EF Core 或通訊 SDK。

## 前置需求

- Windows 10/11 64-bit
- .NET 8 SDK 與 ASP.NET Core 8 Runtime
- Docker Desktop（Linux containers）
- Prosys OPC UA Simulation Server（驗證 OPC UA 時需要）
- Modbus TCP Simulator（執行正常 Modbus 外部整合測試時需要）

## 快速開始

### 1. 設定環境變數

```powershell
Copy-Item .env.example .env
```

編輯 `.env`，填入下列兩個密碼，並保留在本機，不要提交至版本控制：

```text
POSTGRES_PASSWORD=
SEQ_FIRSTRUN_ADMINPASSWORD=
```

### 2. 啟動 PostgreSQL 與 Seq

```powershell
docker compose up -d
docker compose ps
```

服務位置：

| 服務 | 位址 |
| --- | --- |
| PostgreSQL | `127.0.0.1:5432` |
| Seq UI | <http://127.0.0.1:8081> |
| Seq ingestion | `http://127.0.0.1:5341` |

停止服務可執行 `docker compose down`；named volumes 會保留資料。

### 3. 設定 API 與 Worker 的本機資料庫連線

```powershell
Copy-Item src/IIoT.FaultDiagnosis.Api/appsettings.Local.example.json src/IIoT.FaultDiagnosis.Api/appsettings.Local.json
Copy-Item src/IIoT.FaultDiagnosis.Worker/appsettings.Local.example.json src/IIoT.FaultDiagnosis.Worker/appsettings.Local.json
```

將 `.env` 的 PostgreSQL 密碼填入兩個 `appsettings.Local.json`。這些檔案已在 `.gitignore` 中排除。

### 4. 套用資料庫 Migration

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/IIoT.FaultDiagnosis.Infrastructure --startup-project src/IIoT.FaultDiagnosis.Api
```

### 5. 啟動 API 與 Worker

```powershell
dotnet run --project src/IIoT.FaultDiagnosis.Api -- --urls http://127.0.0.1:5080
```

健康檢查：<http://127.0.0.1:5080/health>

另一個終端機可啟動 Collector Worker：

```powershell
dotnet run --project src/IIoT.FaultDiagnosis.Worker
```

Worker 預設每秒輪詢啟用設備，最多重試 3 次，並每 5 秒保存一次系統 Metric。設定可由 `appsettings.json`、`appsettings.Local.json`、環境變數或命令列覆寫。

## API 概覽

| 方法 | 路徑 | 用途 |
| --- | --- | --- |
| `GET` | `/health` | API 健康檢查 |
| `POST` | `/api/devices` | 建立設備 |
| `GET` | `/api/devices` | 列出設備 |
| `GET` / `PUT` / `DELETE` | `/api/devices/{id}` | 查詢、更新、刪除設備 |
| `POST` | `/api/devices/{id}/test` | 測試 Modbus TCP 讀取 |
| `POST` | `/api/devices/{id}/test-opc-ua` | 測試 OPC UA 連線與 Node 讀取 |
| `POST` | `/api/experiments` | 建立實驗 |
| `POST` | `/api/experiments/{id}/start` | 背景啟動實驗 |
| `POST` | `/api/experiments/{id}/stop` | 停止實驗 |
| `GET` | `/api/experiments/{id}/runs` | 查詢 Run 統計 |
| `GET` | `/api/experiments/{id}/records` | 查詢通訊紀錄 |
| `GET` | `/api/experiments/{id}/export` | 匯出 CSV |
| `POST` | `/api/communication-records/{id}/diagnose` | 執行規則式診斷 |
| `GET` | `/api/experiments/{id}/diagnosis-evaluation` | 查詢診斷評估 |

`protocolType` 目前 API 使用數字列舉：`1` 為 `ModbusTcp`、`2` 為 `OpcUa`、`3` 為預留的 `Mqtt`。

## 設備與通訊範例

### 建立 Modbus TCP 設備

```json
{
  "name": "Machine 01",
  "protocolType": 1,
  "host": "127.0.0.1",
  "port": 502,
  "isEnabled": true
}
```

```text
POST /api/devices/{id}/test
```

```json
{
  "slaveId": 1,
  "startAddress": 0,
  "numberOfPoints": 6
}
```

### 建立 OPC UA 設備

將 Prosys OPC UA Simulation Server 啟動後，預設可使用：

```json
{
  "name": "Prosys Simulation Server",
  "protocolType": 2,
  "host": "opc.tcp://127.0.0.1:53530/OPCUA/SimulationServer",
  "port": 53530,
  "isEnabled": true
}
```

```text
POST /api/devices/{id}/test-opc-ua
```

```json
{
  "nodeIds": ["i=2258"],
  "useSecurity": false,
  "operationTimeoutMs": 10000
}
```

`i=2258` 為 OPC UA ServerStatus CurrentTime。成功時會回傳解析後的值，且每次 Modbus 或 OPC UA 測試都會保存一筆 `communication_records`。

將 `useSecurity` 設為 `true` 時，adapter 會選擇安全端點並驗證伺服器與用戶端憑證。`operationTimeoutMs` 可用於控制 OPC UA 請求逾時；可在測試環境縮短以重現 Timeout。

OPC UA Adapter 支援 `InvalidEndpoint`、`InvalidNodeId`、`CertificateError`、`SessionDisconnected` 與 `Timeout`。讀取遇到 `BadConnectionClosed`、Session 或 Secure Channel 中斷時，會延遲後重建 Session 並重試一次。

## 實驗與診斷

實驗會建立對應協定的設備設定、在背景執行指定次數，並保存每筆通訊的 response time、status、fault type、原始請求／回應、解析值與 retry count。每一個 Run 都會保存成功／失敗數、延遲統計與系統資源平均值。

`actual_fault_type` 是實驗 Ground Truth，`predicted_fault_type` 是診斷結果，兩者分開保存。CSV 匯出可作為後續 Dataset 建立的輸入。

目前規則式診斷覆蓋：

- `ConnectionRefused`
- `Timeout`
- `IllegalAddress`

## 建置與測試

```powershell
dotnet build IIoT.FaultDiagnosis.sln
dotnet test tests/IIoT.FaultDiagnosis.UnitTests/IIoT.FaultDiagnosis.UnitTests.csproj
```

目前 Unit Tests 包含 Domain、Modbus、OPC UA 設定與錯誤分類、OPC UA 無效 Endpoint、Collector protocol routing、retry、metrics 與 diagnosis。

完整 Integration Tests 需要 PostgreSQL 與外部 Modbus TCP Simulator。若 `127.0.0.1:502` 未提供符合測試預期的 Modbus Server，Phase 4 的正常與 Illegal Address 情境會失敗；這不會影響單元測試或 OPC UA 驗收。

## 已驗證項目

- `dotnet build IIoT.FaultDiagnosis.sln --no-restore` 成功，0 warnings、0 errors。
- Unit Tests 31/31 通過。
- PostgreSQL、Seq、API health check 與 Seq 日誌接收已驗證。
- Prosys OPC UA Simulation Server 已驗證正常讀取、無效 NodeId、無效 Endpoint、Server 重啟後 Session 重連，以及安全端點拒絕未受信任用戶端憑證時的 `CertificateError`。
- 獨立 TCP listener 在接受連線後不回覆 OPC UA 封包，已驗證回傳 `BadRequestTimeout` 時正規化為 `Timeout`。

## 尚未開始的階段

- Phase 7：MQTT
- Phase 8：Dataset
- Phase 9：Machine Learning
- Phase 10 及後續研究功能

本 README 僅描述目前已完成的功能，不代表後續 Phase 已實作。
