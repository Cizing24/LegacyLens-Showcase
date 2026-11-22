# DEV-001 — 初始化腳本的實際開發問題

分類：Authentic Development Issue。日期：2026-11-22。

## 實際順序與證據

第一輪 Windows PowerShell 5.1 執行 UTF-8（無 BOM）腳本，內含繁體中文的 SQL 被錯誤解碼，SQL Server 回傳 `Incorrect syntax near 'Closed'` 與未關閉字串錯誤。Transaction rollback 正常，沒有留下部分使用者或案件。

將腳本轉為 UTF-8 BOM 後，下一次嘗試揭露另一個真實錯誤：Seed 將 CustomerId / AssignedTo 寫死為 1 / 2。SQL Server rollback 不會回收 IDENTITY 序號，重跑後已不存在這些 ID，因此發生 FK constraint 衝突。

## 根因、選項與修正

編碼選項：全面改用 PowerShell 7，或保留 Framework 工具常用的 Windows PowerShell 5.1 並使用 UTF-8 BOM。選擇後者，避免要求額外 Runtime。資料 ID 選項：重設 IDENTITY，或依合成資料的穩定 business key 查詢。選擇查詢 Username / Customer 名稱，不刪資料、不 reseed IDENTITY。

## 驗證

修正後實際建立四位合成使用者、三個客戶及 45 筆案件；再次執行會保留既有 Seed。後續 regression 使用動態查詢 ID，不假設 IDENTITY 連續。這是真實開發問題，與 WEB-001 的受控故障分開保存。
