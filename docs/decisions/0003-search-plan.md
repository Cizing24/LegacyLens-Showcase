# ADR 0003 — 保留頁碼契約，以 predicate 與 covering index 改善搜尋

狀態：Accepted。日期：2026-11-22。

## 背景與證據

參照 SQL-001 與 benchmarks/RESULTS.md。10k、50k、100k 真實資料的 optional-OR 查詢，reads 隨資料量增加。只改寫 predicate 仍讀相同 pages；索引方案降低讀取並通過多個 offset 結果等價測試。

## 方案比較

1. 僅 query rewrite：最少 schema 變更，實測仍掃描，不能達到本次讀取成本目標。
2. Query rewrite + covering indexes：保留查詢與頁碼語意，可觀察 seek 路徑，代價是兩個索引的空間與寫入維護。
3. Keyset pagination：深頁較穩定，但需要 cursor contract、上一頁 state 與 UI 調整；目前 100k 展示沒有足夠證據要求改 API。
4. 全文檢索或外部搜尋服務：增加查詢語意與維運成本，非目前需求。

## 決定與後果

採第 2 項；固定 SQL 片段、型別參數、CreatedAt+Id tie-breaker。保留 baseline SQL 與原始 plans；migration 可重跑。substring 與極深頁限制寫入文件，不以少數 Open 查詢推論所有查詢。寫入成本只說明原理，未量測就不提供數字。
