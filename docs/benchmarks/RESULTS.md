# SQL-001：公開 311 案件搜尋實測

日期：2026-11-22（Asia/Taipei）。資料分類：Public Dataset Observation；查詢實驗為受控本機 Benchmark。Issue：[SQL-001 #4](../incidents/SQL-001.md)。

## 條件與方法

NYC 311 原始五欄 allowlist 快照，10k / 50k / 100k 累積匯入，未複製成假大型資料。各規模另有 45 筆固定合成案件及少量明確標示的 E2E 案件；每份 results.json 記載當時實際 totalRows。SQL Server 2019 LocalDB、Windows build 26300、16,542,154,752 bytes RAM、12 logical processors；完整 SQL 版本文字在原始報告。

查詢回傳 `Id, Title, Status, CreatedAt, Assignee, Customer`，條件 `Status='Open'`、主旨空字串、offset=0、21 筆；應用 UI 顯示 20 筆並用第 21 筆判斷下一頁。穩定排序 `CreatedAt DESC, Id DESC`。40 次 measured executions，各 stage 先 5 次 warmup，使用同一個已開啟的本機 SqlConnection，Stopwatch 包含 ExecuteReader 與讀完結果。不含登入、HTTP、connection open 或網路遠端延遲。

每個規模依序比較 Baseline optional-OR SQL、僅改寫 predicate、改寫加 covering indexes。先移除本實驗擁有的兩個索引並 FULLSCAN 更新統計。plan/IO 另外收集，避免把 XML 診斷傳輸加進 latency。P50 / P95 使用 nearest rank。沒有 DBCC 清除快取；未測 cold cache、多使用者或長時間壓力。測試期間有本機開發及瀏覽器活動，數值可能受背景工作影響。

## 實測結果

| 公開資料筆數 | 方案 | P50 ms | P95 ms | 全查詢 Logical Reads |
|---:|---|---:|---:|---:|
| 10,000 | Baseline | 3.3023 | 4.1417 | 405 |
| 10,000 | 只改寫 | 2.4660 | 2.9644 | 405 |
| 10,000 | 改寫＋索引 | 1.1334 | 1.2300 | 86 |
| 50,000 | Baseline | 11.3786 | 15.2105 | 1,672 |
| 50,000 | 只改寫 | 7.9553 | 10.0961 | 1,672 |
| 50,000 | 改寫＋索引 | 1.0768 | 1.1548 | 88 |
| 100,000 | Baseline | 22.3420 | 26.7085 | 3,255 |
| 100,000 | 只改寫 | 16.8213 | 22.1663 | 3,255 |
| 100,000 | 改寫＋索引 | 1.0832 | 1.3413 | 87 |

100k 的原查詢 Cases 部分讀取 3,171 pages，加上 Users/Customers 合計 84；索引方案 Cases 只需 3 pages，總計 87。讀取資訊為 SQL Server `SET STATISTICS IO` 輸出，不是估算的假數字。

## 正確性與取捨

每個 stage 比對 offset 0、20、200 的有序 ID，全部相同。應用 regression 另涵蓋空／非空主旨、多狀態與分頁，避免只驗證 Open 第一頁。

只改寫 predicate 有延遲改善但讀取 pages 不變；covering index 才讓 Cases 範圍 seek、排序與 covering 成為可行路徑。採 `(Status,CreatedAt DESC,Id DESC) INCLUDE(Title,CustomerId,AssignedTo)`，另有 `(CreatedAt DESC,Id DESC)` covering index 支援不指定狀態的列表。100k 資料的所有 nonclustered indexes 合計 used pages 為 **16,872 KB**，其中也包含既有 SourceKey unique index，不能全數算成新增成本。新增索引會增加儲存與 INSERT/UPDATE 維護；本次未對寫入 overhead 定量，不能宣稱沒有代價。

Open 在這份較舊的公開紀錄中占比較低，因此不能推論所有狀態或關鍵字都有相同比例改善。主旨 substring `LIKE '%term%'` 仍不具一般 B-tree seek 的優勢；深頁 OFFSET 仍有線性跳過成本。現有頁碼契約保留，未強制改成 keyset pagination。

## 原始證據與重跑

- [10k samples / environment](10000/results.json)、[50k](50000/results.json)、[100k](100000/results.json)。
- 各資料夾都有 baseline / rewrite-only / indexed 的 `.sqlplan` 與 `-io.txt`。
- SQL：tools/benchmark/baseline.sql、rewritten.sql；索引 migration：database/migrations/002-search-index.sql。
- 先 setup / seed、download.py，依 10k→50k→100k 順序執行 import.ps1 與 run.ps1。Benchmark 拒絕實際 public row count 與 Scale 不符，避免錯標規模。
- 重跑會操作兩個具名實驗索引；只在 LegacyLens 本機展示 DB 執行，勿對共用或正式資料庫執行。
