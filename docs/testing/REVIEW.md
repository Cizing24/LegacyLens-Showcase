# Code / Security Review

2026-11-22。本次為實作代理依 code-review-and-quality、security-and-hardening 的自我審查；沒有冒稱第三方獨立審核或完整 penetration test。

## 邊界與資產

HTTP form / cookie / ViewState 為外部輸入；Socrata 欄位為外部資料；SQL 為狀態與 audit 的信任邊界。保護資產是展示帳號、案件正確性、歷程與私有原始碼。Repository Private、loopback IIS Express 與非本機請求拒絕降低展示暴露面。

## 發現並完成的修正

- SessionID 在首次沒有寫入 Session 時未持久化：寫入 CsrfKey 後綁 ViewStateUserKey。未關閉安全驗證。
- AutoPostBack 產生的字串 timer 被 CSP 阻擋：外部 JS requestSubmit，不增加 unsafe-eval；測試等待真正 POST。
- 新轉派規則可被舊 Save 路徑繞過：在 Application 與 transaction 內阻擋既有案件直接改承辦人。
- Login lockout 到期後的錯誤嘗試可能用舊 FailedAttempts 立即重新鎖定；改為過期先重算，鎖定期間不因請求而延長。新增真正 SQL identity regression。
- 非本機拒絕原用 CompleteRequest 仍可能繼續 handler；改為在 BeginRequest 結束請求。
- Integration fixture 繼承曾讓 10 個基底測試重複執行；抽出只有 setup/helpers 的 DatabaseFixture，最終數字不含重複的 inherited tests。

## 已檢查

SQL 值均為 typed parameters；可組合的 predicate 僅來自固定字串。表格 BoundField 預設 HtmlEncode，頁面動態字串採編碼。Forms cookie 為 HttpOnly / SameSite Strict，登入後清除並放棄 Session，ViewState 強制 MAC / encryption / EventValidation。更動、History、Audit、轉派 ledger 在單一 transaction。rowversion 防止 stale writes，request key 防重送。

54 個 NUnit（含真實 SQL）與 11 個 Chromium E2E 實際通過；ViewState 竄改回一般錯誤頁，沒有 stack 或 connection string；Viewer 即使啟用按鈕仍被後端403拒絕；過期編輯不能覆寫新版本。`npm audit --audit-level=high` 實際回報 0 vulnerabilities。沒有把這些有限測試稱為全方位安全保證。

## 明確限制

本機 HTTP 的 cookies 沒有 Secure；轉成任何網路可用環境前需正式 TLS、Secure cookies、金鑰／權限／觀測與部署審查。CSP 因 Web Forms 內嵌 Postback 保留 unsafe-inline；未開 unsafe-eval。LocalDB 開發 Windows 身分有 DB owner，未用 production 最小權限帳號。未做 MFA、SSO、分散式 session、資安掃描器全站滲透或螢幕閱讀器驗證。
