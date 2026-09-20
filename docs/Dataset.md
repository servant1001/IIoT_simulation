# Phase 8 正式資料集

本資料集由完成實驗所產生的 `communication_records` 建立。匯出 API 只選擇具有 `experiment_id` 的紀錄，確保每一列都有來自 Experiment 的 Ground Truth；手動裝置連線測試不屬於正式資料集。

## 標籤規則

- `actual_fault_type`：Experiment 建立時指定的真實故障類型（Ground Truth）。
- `predicted_fault_type`：該筆通訊紀錄最新一筆 `diagnosis_results` 的預測結果；尚未診斷時為空白。
- `observed_fault_type`：Adapter 根據實際通訊結果正規化的觀測錯誤，保留供特徵分析與診斷驗證。

三個欄位用途不同。匯出流程不會用 `predicted_fault_type` 覆寫 `actual_fault_type`。

## CSV 欄位

| 欄位 | 說明 |
| --- | --- |
| `dataset_record_id` | 原始通訊紀錄 ID |
| `experiment_id`、`experiment_run_id`、`run_number` | 實驗與執行批次識別 |
| `device_id`、`protocol` | 設備與通訊協定 |
| `actual_fault_type` | Ground Truth |
| `predicted_fault_type`、`diagnosis_method`、`diagnosis_confidence` | 最新診斷結果，可為空白 |
| `started_at`、`completed_at`、`response_time_ms` | 時間與延遲資料 |
| `status`、`observed_fault_type`、`error_code`、`error_message`、`retry_count` | 通訊觀測結果 |
| `raw_request`、`raw_response`、`parsed_value` | 原始通訊與解析內容 |

CSV 使用 UTF-8；非空白值會加上雙引號，並正確跳脫內嵌的雙引號與逗號。

## 收集與驗收

1. 為每個預定的 Protocol／ActualFaultType 組合建立 Experiment。
2. 啟動對應外部模擬器或故障情境，讓 Experiment 完成所需的 repeat runs。
3. 視需要對通訊紀錄呼叫診斷 API，保留預測欄位供後續模型評估。
4. 查詢 `GET /api/datasets/communication-records/summary?minimumRunsPerFault=100`。
5. 每個目標組合的 `remainingRunCount` 為 `0` 後，再以 export API 產生訓練資料集。

Phase 8 的程式已提供正式資料集結構與覆蓋率檢查；實際的 100 runs／fault 必須由外部模擬器依研究情境收集，不能以合成或手動測試紀錄替代。
