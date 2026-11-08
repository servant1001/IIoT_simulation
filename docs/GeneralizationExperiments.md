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

## G-02：Modbus TCP 獨立延遲分布驗證

G-02 用於驗證 G-01 改善後的 Rule-based 門檻與 RAG 知識，是否能在**不同資料分布**下維持結果。執行前已凍結診斷規則、RAG 知識庫與三個 ML 模型；只有模擬器的延遲序列改為 `118、267、411、639、888 ms`。因此它是改善措施的獨立重複驗證，不是新的模型訓練。

| 項目 | 設計／結果 |
| --- | --- |
| 協定與 Ground Truth | Modbus TCP，`NetworkDelay (19)`。 |
| Simulator | 同一個 TCP Proxy，但透過 `MODBUS_DELAY_SEQUENCE_MS` 設定為新的序列；對外仍為 `127.0.0.1:1505`。 |
| 小型驗證 | `MODBUS_NETWORK_DELAY_G02_001_VALIDATION_20261108`，Experiment ID `6d1608d5-56a8-49cc-9a42-2aac6ed543a5`；1 / 1 Completed、4 records，回應時間為 139.78、272.68、416.14、643.15 ms。 |
| 正式 Experiment | `MODBUS_NETWORK_DELAY_G02_100RUN_20261108`，Experiment ID `033d2eea-e766-4d83-b201-a3a28a7cfdaf`。 |
| 正式收集 | 100 / 100 Completed、388 records；全部為 `Success` 且 `observed_fault_type = None`。 |
| 回應時間 | 最小 121.37 ms、平均 468.76 ms、中位數 415.59 ms、最大 895.02 ms。 |
| Dataset CSV | `artifacts/generalization/modbus-network-delay-g02-20261108/dataset.csv`（388 rows，SHA-256：`D501F88B72A72269E3066BFADAC4D493CA5707A143DCB2BE837ABBA91725EC9C`）。 |
| Coverage | 100 runs / 388 records / 388 diagnosed，`remainingRunCount = 0`。 |

### G-02 評估結果

| 方法 | 結果 | 固定條件下的解讀 |
| --- | --- | --- |
| Rule-based | 388 / 388（100%） | 所有成功回覆均超過 G-01 建立的 100 ms 實驗室門檻。 |
| RAG | Top-1、Top-3 均 388 / 388（100%） | 固定的 `modbus-network-delay` 知識來源被正確檢索；平均診斷時間 3.33 ms。 |
| Logistic Regression | 0 / 388（0%），全部預測 `None` | 固定模型的類別空間不含 `NetworkDelay`。 |
| Random Forest | 0 / 388（0%），全部預測 `None` | 固定模型的類別空間不含 `NetworkDelay`。 |
| SVM | 0 / 388（0%），全部預測 `None` | 固定模型的類別空間不含 `NetworkDelay`。 |

ML 的可重現輸出、混淆矩陣與 metrics 存於 `ml/outputs/generalization/modbus-network-delay-g02/`；G-02 CSV 與 coverage 快照不併入 Phase 8 的固定訓練資料。

### G-02 結論與限制

G-02 支持「G-01 建立的 100 ms 規則與 RAG 知識，可在新的、較高且不同分布的延遲序列中穩定工作」；但它仍是同一協定、同一 TCP Proxy 與所有值皆高於門檻的受控驗證。它不能證明跨協定、接近門檻（例如 80–120 ms）或真實工廠網路下的泛化能力。兩次實驗也一致指出：封閉集合 ML 對未出現在訓練類別的故障會錯誤地自信預測 `None`，應在後續導入 Unknown／拒答機制與跨協定資料。

## G-03：新增標註資料後的 ML 獨立驗證

G-03 回答的不是「未見類別能否直接辨識」，而是「取得 G-01 的標註資料並重新訓練後，模型能否辨識未參與訓練的 G-02 資料」。訓練前已明確排除 G-02；合併工具會保存來源 hash、Experiment ID 與 Run 數，確保資料來源可稽核。

| 項目 | 設計／結果 |
| --- | --- |
| 訓練輸入 | 原正式快照 3,800 records／1,500 runs，加上 G-01 398 records／100 runs。 |
| 訓練輸入 hash | `9CCB43A00246850D45447BECA7B15854B47D4817F67195A675C523153E3838F2`。 |
| 訓練規模 | 4,198 records、1,600 runs、13 個 `actual_fault_type`，包含 398 筆 `NetworkDelay`。 |
| 排除資料 | G-02 的 388 records／100 runs 未出現在合併快照、Run-aware split、訓練或模型選擇中。 |
| Run-aware split | Train 4,143、Validation 29、Test 26 records；同一個 Run 不會跨集合。 |
| 訓練產物 | `ml/outputs/networkdelay-v1-20261108/`。 |
| 外部測試產物 | `ml/outputs/generalization/modbus-network-delay-g02-retrained/`。 |

### G-03 模型結果

| 模型 | 內部 Run-aware Test | 對 G-02 的獨立結果 | G-02 混淆情形 |
| --- | --- | --- | --- |
| Logistic Regression | Accuracy 88.46%；`NetworkDelay` recall 25.00%（support 4）。 | 155 / 388，39.95%。 | `NetworkDelay` 155、`None` 233。 |
| Random Forest | Accuracy 100.00%；`NetworkDelay` precision／recall 皆 100.00%（support 4）。 | 388 / 388，100.00%。 | 全部預測 `NetworkDelay`。 |
| SVM | Accuracy 42.31%；`NetworkDelay` recall 50.00%（support 4）。 | 232 / 388，59.79%。 | `NetworkDelay` 232、`None` 156。 |

相較於改善前的三個模型在 G-02 均為 0 / 388，G-03 證明新增標註的 `NetworkDelay` 資料能讓模型學得該類別；其中 Random Forest 在這個獨立資料集達到完整辨識。Logistic Regression 主要僅將約 642 ms 以上的回覆判為延遲，SVM 主要在約 415 ms 以上才判為延遲，因此兩者仍把較低延遲的成功回覆誤判為 `None`。

### G-03 效度限制

Random Forest 的 100% 是一次保留的、同協定 TCP Proxy 延遲分布結果，不能據此宣稱已適用真實工廠或不同協定；內部 test 中 `NetworkDelay` 僅含 4 筆紀錄，也不適合單獨作為強泛化證據。G-02 在本輪結果公布後不應再被用來調整 Random Forest 超參數或特徵；下一輪應建立新的 G-04 延遲分布與跨協定資料，並評估 Unknown／拒答策略。

## G-04：接近 Rule 門檻的獨立延遲驗證

G-04 在 G-03 模型已凍結後，以新的延遲序列 `70、90、110、200、400 ms` 檢驗接近 100 ms Rule-based 門檻的情況。所有資料的 Ground Truth 均為 `NetworkDelay`，因為每次回覆皆由 Proxy 主動注入顯著高於正常基準（約 30 ms）的延遲；100 ms 只是既有 Rule/RAG 的判斷門檻，並非 Ground Truth 的定義。

| 項目 | 設計／結果 |
| --- | --- |
| 小型驗證 | `MODBUS_NETWORK_DELAY_G04_001_VALIDATION_20261108`，Experiment ID `11dce9b8-afeb-48ce-870e-e2cae0fa70b5`；1 / 1 Completed、5 records，回應時間 105.97、93.93、113.97、204.29、403.28 ms。 |
| 正式 Experiment | `MODBUS_NETWORK_DELAY_G04_100RUN_20261108`，Experiment ID `b9d26a84-e69d-4d03-a7b3-ae3c961a2a7b`。 |
| 正式收集 | 100 / 100 Completed、500 records；全部為 `Success` 且 `observed_fault_type = None`。 |
| Response Time | 最小 72.26 ms、平均 177.68 ms、最大 406.60 ms。 |
| Dataset CSV | `artifacts/generalization/modbus-network-delay-g04-20261108/dataset.csv`（500 rows，SHA-256：`AA02B74EC8C08AC87968FA5CD5592965534EFD21F96ED3924E9B6A3CAF8EA39C`）。 |
| Coverage | 100 runs / 500 records / 500 diagnosed，`remainingRunCount = 0`。 |
| 凍結條件 | 沿用 G-03 的 Rule、RAG 與三個已訓練模型；G-04 不參與訓練或調參。 |

### G-04 邊界結果

| 方法 | 正確數／總數 | Accuracy | 觀察 |
| --- | ---: | ---: | --- |
| Rule-based | 300 / 500 | 60.00% | 實測低於 100 ms 的 70／90 ms 注入延遲被判為 `None`。 |
| RAG | Top-1、Top-3 均 300 / 500 | 60.00% | 使用同一 100 ms 檢索門檻；平均診斷時間 3.08 ms。 |
| Logistic Regression | 0 / 500 | 0.00% | 全部預測 `None`；G-04 的最高回應時間仍低於其在 G-02 顯示的高延遲判斷區域。 |
| Random Forest | 400 / 500 | 80.00% | 72.26–79.62 ms 的 100 筆預測 `None`；92.64–406.60 ms 的 400 筆預測 `NetworkDelay`。 |
| SVM | 100 / 500 | 20.00% | 僅 402.92–406.60 ms 的 100 筆預測 `NetworkDelay`，其餘預測 `None`。 |

### G-04 結論與限制

G-04 推翻「Random Forest 在所有延遲條件下均為 100%」的過度推論：它在 G-02 的較高延遲資料完整辨識，但對本輪最低延遲區間漏判 100 筆。Rule-based 與 RAG 的 60% 則直接反映固定 100 ms 門檻的設計取捨，而不是資料收集失敗。

此結果使研究結論更具可信度：不同方法在延遲邊界呈現不同敏感度；目前最合理的後續工作不是依 G-04 再微調門檻或模型，而是以 G-04 保持為已報告的獨立結果，建立新的故障型態資料，並研究 Unknown／拒答機制與成本敏感的告警門檻。

## G-05：Unknown／人工確認護欄

G-05 使用新的 `modbus-unknown-exception-simulator` 回覆 Modbus Exception Code `03`。目前 Adapter 不將此碼映射到已知 FaultType，因此其 `observed_fault_type` 為 `Unknown`；此情境沒有加入 G-03 的模型訓練快照。

| 項目 | 設計／結果 |
| --- | --- |
| 小型驗證 | `MODBUS_UNKNOWN_EXCEPTION_G05_001_VALIDATION_20261108`，Experiment ID `9fa7fcc2-8a66-4ab8-b1e7-816741b67050`；1 / 1 Completed、2 records，皆為 `Unknown`／`ModbusExceptionCode03`。 |
| 正式 Experiment | `MODBUS_UNKNOWN_EXCEPTION_G05_100RUN_20261108`，Experiment ID `01348cc8-49de-484b-9bba-4f89423134d7`。 |
| 正式收集 | 100 / 100 Completed、200 records；Ground Truth 與 Adapter 觀測皆為 `Unknown`，錯誤碼皆為 `ModbusExceptionCode03`。 |
| Dataset CSV | `artifacts/generalization/modbus-unknown-exception-g05-20261108/dataset.csv`（SHA-256：`3E679CFE80AB7BD61568D572F6560CF58AB1AEDA2F1EF1AB9807DB02CEEF0893`）。 |
| Rule-based／RAG | 均為 200 / 200；RAG 使用 `modbus-unknown-protocol-fault` 來源，明確建議保存 raw trace 並人工確認。 |

### Closed-set 與 open-set ML 比較

| 模型 | 原 closed-set 輸出 | 加入 open-set 護欄後 |
| --- | --- | --- |
| Logistic Regression | 200 / 200 誤判為 `IllegalAddress`。 | 200 / 200 輸出 `Unknown`，要求人工確認。 |
| Random Forest | 200 / 200 誤判為 `IllegalAddress`。 | 200 / 200 輸出 `Unknown`，要求人工確認。 |
| SVM | 166 筆 `IllegalAddress`、34 筆 `TopicMismatch`。 | 200 / 200 輸出 `Unknown`，要求人工確認。 |

open-set 評估會以 G-03 合併快照的 Run-aware validation split 校準每個模型的信心門檻（Logistic Regression 0.5576、Random Forest 1.0000、SVM 0.9996），但 G-05 的 200 筆資料皆由 `adapter_unknown` 護欄觸發，而非低信心門檻。Unknown 偵測 precision／recall 均為 100%。

### G-05 結論與限制

G-05 證明安全護欄可避免 closed-set ML 把 Adapter 已明確標示的未知協定例外錯誤歸類為既有故障，並把處置轉為人工確認。這不是「模型自行發現所有未知故障」的證明：本輪 100% 主要來自 Adapter 提供的 `Unknown` 訊號。後續應加入不被 Adapter 標示為 Unknown、但特徵分布偏離訓練資料的情境，才能單獨衡量信心門檻的 OOD 偵測能力。

## 評估原則

1. 固定訓練快照不加入本實驗資料；ML 不可在尚未評估前以 `NetworkDelay` 資料重新訓練。
2. Rule-based 與 RAG 使用既有規則／知識執行，記錄其對未見 Ground Truth 的預測與建議。
3. ML 應載入原本的模型，對此資料做 inference；若模型無法輸出 `NetworkDelay`，要明確記錄其錯誤分類，而不是將資料加入訓練後再宣稱泛化。
4. 完成後需保存 Experiment ID、CSV 快照、回應時間分布、混淆情形與人工評閱結果。
