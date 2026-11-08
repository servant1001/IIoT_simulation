# 泛化能力驗證實驗紀錄

本文件保存未納入 Phase 8 固定訓練快照的實驗。其目的是檢驗 Rule-based、RAG 與 ML 在未見條件下的行為；不得與 15 種正式受控情境的 3,800 筆資料混合計算。

## G-01：Modbus TCP Network Delay

| 項目 | 設計 |
| --- | --- |
| 實驗目的 | 驗證診斷方法是否可由回應時間辨識「通訊成功但網路品質劣化」的未見情境。 |
| 協定 | Modbus TCP |
| Ground Truth | `NetworkDelay (19)` |
| Simulator | `modbus-delay-simulator`，公開 `127.0.0.1:1505`。 |
| 實作方式 | TCP Proxy 轉送至正常 `modbus-simulator`，對每個回覆依序加入 150、300、450、600、750 ms 延遲。 |
| 原始訓練快照 | 不含 `NetworkDelay` 類別，因此此情境是未見類別／條件驗證。 |

### 小型驗證

| 項目 | 結果 |
| --- | --- |
| Experiment | `MODBUS_NETWORK_DELAY_001_VALIDATION_20261108` |
| Experiment ID | `b55cf76d-9419-4d22-9b12-f0f257aca750` |
| Run | 1 / 1 Completed |
| Communication Records | 4 |
| 回應時間 | 177.22、309.23、457.26、609.16 ms |
| Adapter 觀測 | 全部成功，`observed_fault_type = None`。 |
| Rule-based | 0 / 4：全部預測 `None`。 |
| RAG | Top-1／Top-3 均 0 / 4：全部預測 `None`。 |

此結果符合實驗設計：目前 Rule-based 與 RAG 將「成功回覆」判為正常，尚未將回應時間門檻視為 `NetworkDelay` 的證據。這不是 Simulator 失敗，而是未見退化條件下的預期 baseline。

### 正式泛化收集

| 項目 | 設定 |
| --- | --- |
| Experiment | `MODBUS_NETWORK_DELAY_100RUN_20261108` |
| Experiment ID | `d52cedec-4310-4889-a91b-c0a3f583de4b` |
| Repeat Count | 100 |
| 每 Run 時間 | 5 秒 |
| Polling Interval | 1,000 ms |
| 狀態 | 已停止；停止前有 43 個 Completed Run 與第 44 個 Stopped Run。為避免將停止實驗混入結果，全部排除。 |

| 項目 | 設定 |
| --- | --- |
| 正式重試 Experiment | `MODBUS_NETWORK_DELAY_100RUN_RETRY_20261108` |
| 正式重試 Experiment ID | `cdda33f0-1258-4aee-89f7-3c34a70526a8` |
| Repeat Count | 100 |
| 每 Run 時間 | 5 秒 |
| Polling Interval | 1,000 ms |
| 狀態 | 100 / 100 Completed；無失敗或停止 Run。 |
| Communication Records | 398（全部 `Success`；`observed_fault_type = None`） |
| Response Time | 最小 153.45 ms、平均 457.52 ms、最大 913.51 ms。 |
| Dataset CSV | `artifacts/generalization/modbus-network-delay-20261108/dataset.csv`（398 rows，SHA-256：`919CCD817AA977F94F7D16FE4D35538268137315F3B377AAFCF01DC79EC0D680`） |
| Coverage | `NetworkDelay`：100 runs / 398 records / 398 diagnosed，距離 100-run 目標為 0。 |

### 診斷結果

| 方法 | 結果 | 解讀 |
| --- | --- | --- |
| 原始 Rule-based baseline | 0 / 4（小型驗證） | 原規則只依 Adapter 正規化結果，將成功延遲回覆視為 `None`。 |
| 原始 RAG baseline | Top-1、Top-3 均 0 / 4（小型驗證） | 原知識庫沒有「成功但回應過慢」的診斷知識。 |
| 改善後 Rule-based | 398 / 398（100%） | 新增 Modbus 成功回覆超過 100 ms 的實驗室基準門檻，預測 `NetworkDelay`。 |
| 改善後 RAG | Top-1、Top-3 均 398 / 398（100%） | 新增 `modbus-network-delay` 知識來源，平均診斷時間 6.42 ms。 |
| Logistic Regression（固定原模型） | 0 / 398（0%） | 全部預測 `None`。 |
| Random Forest（固定原模型） | 0 / 398（0%） | 全部預測 `None`。 |
| SVM（固定原模型） | 0 / 398（0%） | 全部預測 `None`。 |

三個 ML 模型的訓練類別均不含 `NetworkDelay`，未見類別推論的 `accuracy` 皆為 0。其原始輸出、混淆矩陣與指標保存在 `ml/outputs/generalization/modbus-network-delay/`；此目錄是可重現的實驗產物，不納入原始 Phase 8 訓練快照。

### 結論與效度限制

G-01 證明受控資料上的高正確率不能直接等同於未見條件的泛化能力：原始 Rule-based、RAG 與全部既有 ML 模型皆未辨識延遲型故障。其後新增的 100 ms Rule-based 門檻與對應 RAG 知識，對此受控延遲資料皆達 100%，可作為**工程改善後的驗證**。

不過這不是未調整系統的純 held-out 成績：門檻與知識是在小型 baseline 已揭露該問題後加入。因此書審或論文應同時呈現「改善前 0 / 4」與「改善後 398 / 398」，並說明後者驗證的是改善措施，而非宣稱 ML 已學會未見類別。下一步應收集不同延遲分布、背景負載與其他協定的獨立資料，並建立 Unknown／異常偵測策略。

## 評估原則

1. 固定訓練快照不加入本實驗資料；ML 不可在尚未評估前以 `NetworkDelay` 資料重新訓練。
2. Rule-based 與 RAG 使用既有規則／知識執行，記錄其對未見 Ground Truth 的預測與建議。
3. ML 應載入原本的模型，對此資料做 inference；若模型無法輸出 `NetworkDelay`，要明確記錄其錯誤分類，而不是將資料加入訓練後再宣稱泛化。
4. 完成後需保存 Experiment ID、CSV 快照、回應時間分布、混淆情形與人工評閱結果。
