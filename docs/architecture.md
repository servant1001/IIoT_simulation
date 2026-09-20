# Phase 3 Architecture

所有專案使用 .NET 8、Nullable 與 ImplicitUsings。

## Project References

```text
Api ────────────> Application ──> Domain
  ├────────────> Infrastructure
  └────────────> Protocols ─────> Domain
Worker ────────> Application ──> Protocols ──> Domain
  └────────────> Infrastructure ──> Application
                               └─> Protocols
UnitTests ─────> Application, Domain, Protocols
IntegrationTests ──> Api
```

Api 與 Worker 是 composition roots，透過框架內建的 Microsoft DI 組裝 Host。
Domain 無套件或其他專案相依。Application 不依賴 Infrastructure 或通訊 SDK。
Infrastructure 使用 EF Core 與 Npgsql 實作 Application 的 repository abstraction。Protocols 使用 NModbus 實作 Modbus TCP，並只公開不含 SDK 型別的 adapter contract、設定模型與結果模型。

API 提供 `/health`、Device CRUD 與 `POST /api/devices/{id}/test`。Controller 只負責 request validation、HTTP status 與呼叫 Application service；Domain validation、Modbus 存取及資料庫存取不在 Controller。
Worker 使用 `BackgroundService` 呼叫 Application 的 `ICollectorService` 與 `IMetricService`。每次迴圈只採集啟用的設備，設備錯誤不會中斷其他設備或下一輪；停止 Token 會傳遞至 I/O、retry delay 與 polling delay。
API 與 Worker 都從設定檔建立 Serilog Console / File / Seq structured logging。

Docker Compose 提供 PostgreSQL 16 與 Seq 2024.3，各自保存 named volume，密碼從未提交的 `.env` 讀入。
`FaultDiagnosisDbContext` 的初始 migration 建立六個研究資料表。Phase 2 migration 允許單次連線測試的 `communication_records` 不帶 experiment/run 關聯；Phase 3 migration 允許 Worker 的主機 `system_metrics` 不帶 Run 關聯。時間均使用 UTC 的 `timestamp with time zone`；`experiments.actual_fault_type` 和 `diagnosis_results.predicted_fault_type` 保持分離。

## Phase Boundary

Phase 3 建立 Modbus Collector Worker。它使用預設 Function 03／Slave 1／Address 0／6 registers，並依設定重試與保存每次採集的 `CommunicationRecord`。`CollectorExecutionContext` 可攜帶既有 Experiment／Run ID，供 Phase 4 啟動實驗時使用。
不建立 Experiment execution、Fault Diagnosis Engine、OPC UA、MQTT 或 CSV export。
既有計畫文件保留在 `doc/ImplementationPlan_IIoT_FaultDiagnosis.md`。
