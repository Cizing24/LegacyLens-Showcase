# DEV-002 — CSP 阻擋 Web Forms 自動 Postback

分類：Authentic Development Issue。不是預設的故障注入。

## 發現經過

Baseline 5 項 E2E 通過後，WEB-001 新增「等待分類切換後的伺服器提示」斷言，發現有／無注入版本都失敗。當時草稿仍存在、伺服器沒有 Category POST 紀錄，不能將這個結果當作 lifecycle 故障重現。

## 根因與選項

Web Forms `AutoPostBack=true` 產生字串參數的 `setTimeout('__doPostBack(...)', 0)`，被現有 CSP（未開放 unsafe-eval）擋下。選項一為新增 unsafe-eval，代價是擴大腳本執行權限；選項二為外部 JavaScript 對分類 select 的 change 使用原生 `form.requestSubmit()`，仍由真正的 Web Forms Page Lifecycle 與 SelectedIndexChanged 處理。

選擇第二項；不新增前端框架，不降低 CSP，不繞過 ViewState MAC 或 EventValidation。既有 Save 與 Logout 仍使用 Web Forms Button。

## 回歸與證據限制

WEB-001 測試現在明確等待 CaseEdit.aspx 的 HTTP POST=200 與 CategoryHint，再檢查主旨／說明。最初同樣失敗的 red/green 試跑屬於 harness investigation，之後修正並重跑，不將它們宣稱為成功重現。Baseline 原始結果保留並補註測試盲點。
