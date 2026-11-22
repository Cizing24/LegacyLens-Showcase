# ADR 0002 — 初次載入 binding 與可觀測的 Postback

狀態：Accepted。日期：2026-11-22。

## 背景與證據

WEB-001 在隔離複本無條件 Page_Load 載入資料時，實際 POST 後草稿被原資料覆寫；同測試在 guarded binding 版本通過。DEV-002 另外證實原生 AutoPostBack 的字串 timer 與現有 CSP 不相容。來源與完整順序見 WEB-001 casebook。

## 決定與取捨

採 `!IsPostBack` 初次載入，保持現有固定伺服器控制項與 ViewState。相較於所有控制項在 Init 重建，修改範圍小且容易回歸。相較於從 Request.Form 強行恢復，保留 Web Forms 原生資料載入機制。

分類 change 使用外部 JS `requestSubmit()`；保留真正 Page Lifecycle / SelectedIndexChanged，不新增 unsafe-eval。CSP 仍需 unsafe-inline 以相容 Web Forms 其他內嵌程式；這是明確的相容性限制。

## 範圍與驗證

CaseEdit.aspx(.cs)、site.js、E2E 測試、隔離重現／修復工具。等待 POST response 與伺服器提示，再斷言兩個欄位；不能只檢查 client-side 值。失敗與成功報告皆保留，故障無公開存取環境或 app setting 開關。
