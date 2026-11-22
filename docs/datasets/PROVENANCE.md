# NYC 311 資料來源、使用條款與轉換

分類：Public Dataset Observation。取得日期：2026-11-22（Asia/Taipei）。

## 選擇與條款

評估 [NYC 311 官方資料](https://data.cityofnewyork.us/Social-Services/311-Service-Requests-from-2020-to-Present/erm2-nwe9) 與 [Chicago 311 官方目錄](https://catalog.data.gov/dataset/311-service-requests)。兩者都是公共服務請求，與 B2B 支援不同。NYC 有穩定 service request key、建立日期、分類與狀態，可用伺服器端欄位 allowlist 與排序抽樣；因此使用 NYC，避免混合兩套語意與字典。Chicago 目錄的 legacy record 欄位亦有維護展示價值，但本次無需第二份資料。

NYC metadata 的 `licenseId` 為 null；**不將它誤標為 MIT / CC0**。[NYC 官方 Technical Standards Manual 的 public policies](https://cityofnewyork.github.io/opendatatsm/publicpolicies.html) 說明公開資料的使用政策、來源／版本／修改的標示，以及資料可能隨時修正、不保證完整或適用性。[官方 Open Data FAQ](https://www.nyc.gov/opendata/get-started/FAQs) 亦提供使用說明。使用時同時遵循 [Open Data Terms](https://data.cityofnewyork.us/stories/s/Terms-of-Use/k9k7-3cje) 與 [NYC.gov Terms](https://www.nyc.gov/main/terms-of-use)。本作品只保存來源、轉換、hash 與測試證據，不大量重新發布原始資料。

## 快照與限制

[snapshot-manifest.json](snapshot-manifest.json) 保存每頁查詢 URL、取得時間、來源更新 epoch、五個欄位字典、100,000 筆原始／唯一鍵數及 JSON/CSV SHA-256。選取 2024-01-01 至 2024-02-01 範圍內最早 100,000 筆，依 created_date、unique_key 排序。這是固定時間窗的排序子集，**不是隨機或具有全市代表性的抽樣**。

官方資料可更新，單靠 URL 無法保證未來抓到相同 bytes；本機 `.runtime/data` 的已下載快照與 hash 是本次實驗版本。另一位工程師可使用原快照精確重跑；若重新下載 hash 不同，須產生新的 manifest 與 Benchmark，不沿用本報告數值。原始大型快照不提交 Git。

## 欄位映射

| 官方欄位 | LegacyLens | 規則 |
|---|---|---|
| unique_key | SourceKey | 原字串保存，唯一索引，重跑不重複 |
| created_date | CreatedAt | NYC 當地時間經 Windows Eastern Standard Time（含 DST 規則）轉 UTC |
| complaint_type | Title | 保留分類名稱；超過 160 字即停止，不靜默截斷 |
| status | SourceStatus + Status | 原值保留；Closed 映射 Closed，其他映射 Open，並非推定內部工作流 |
| agency | Agency | 保留機關縮寫 |
| 無 | CustomerId / AssignedTo | 明確的合成映射客戶與 agent |
| 無 | Description | 固定註記來源與合成欄位；不使用自由文字 resolution |

地址、ZIP、座標、姓名、聯絡方式、descriptor 與 resolution 文字從未要求下載。匯入先驗 CSV SHA-256、筆數、key 及長度，以 SqlBulkCopy 暫存表在 transaction 寫入；任何失敗全部 rollback。重複匯入透過 SourceKey unique index 與 NOT EXISTS 保護。

## 實際規模

10,000 → 50,000 → 100,000 累積匯入，新增筆數分別為 10,000、40,000、50,000；各 JSON 報告保存 checksum、資料庫、匯入階段耗時。此耗時只包含 bulk copy 與 SQL transaction，未包含下載、CSV 讀取與時區轉換，不能當作完整 ETL throughput。
