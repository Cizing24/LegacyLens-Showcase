# Legacy Baseline 驗證（來源歷史）

本頁記錄私人來源的當時結果。原Tag及Commit沒有匯入本展示版，不能在本版checkout；本版自己的測試見 [測試說明](README.md)，重現工具不依賴原Tag。

完整可重現的 Baseline Tag：`legacy-baseline-v1.1`。第一個 `legacy-baseline-v1` Commit 的 staged file 清單遺漏 schema；隨即以獨立 packaging correction Commit 補齊，保留原始 Tag，不改寫歷史。兩者的應用程式碼相同。

2026-11-22（Asia/Taipei）。.NET Framework 4.8 Web Application，MSBuild 17.14、IIS Express 10、SQL Server LocalDB。

- Release build 實際成功，WarningLevel 4 / TreatWarningsAsErrors。
- NUnit：32 / 32 通過；22 Unit 與 10 SQL integration。
- Playwright Chromium：5 / 5 通過。登入／錯誤密碼、案件建立／編輯／儲存／歷程、搜尋／分頁、客戶建立／編輯，以及 Viewer 即使重新啟用按鈕仍收到 403、登出後不能重返案件頁。初版測試的分類選單僅檢查選取後的值，未等待 POST；WEB-001 調查發現 CSP 阻擋原生 AutoPostBack 的字串 timer，因此這個 Baseline 結果不能當成自動 Postback 成功的證據。後續測試明確等待 HTTP POST response 與伺服器回饋。
- 固定 Seed：四位合成使用者、三個客戶、45 筆案件。帳號 ID 不預設連續。
- Browser baseline 執行時另有 10,000 筆 NYC 311 allowlisted records；查驗快照來源另見 dataset manifest。E2E 新增的資料均標示 Baseline E2E / 合成 E2E。
- 原始結果：baseline-all.xml、baseline-unit.xml、baseline-e2e.json。

## 真實開發問題

首輪 ASPX 未明確指定 fileEncoding，繁體中文 Windows 預設編碼造成中文及 HTML 標籤損壞；明確指定 UTF-8 後恢復。下一輪第一次 Postback 暴露 session 尚未寫入時 SessionID 不持久化，ViewStateUserKey 因而不一致。改為在 OnInit 寫入不可預測 CsrfKey，再綁定 ViewStateUserKey，登入與完整操作流程隨後通過。未停用 MAC、EventValidation 或 RequestValidation。

## 範圍

這是本機 Chromium 與 LocalDB 證據；不代表真實企業導入。真實行動裝置、Safari、Firefox、螢幕閱讀器與公開 IIS 部署尚未驗證。
