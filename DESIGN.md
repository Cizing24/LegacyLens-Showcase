# LegacyLens UI implementation record

模式：Operate。這份文件記錄已實作與實際檢視的介面，不宣稱使用者曾核准額外視覺概念。

企業內部工具的工作重點是案件表格、可辨識的狀態、明確編輯與歷程。桌面採深藍側欄、白色工作區、青藍主要操作，字型為Windows系統Segoe UI / Microsoft JhengHei。色票與spacing以Content/site.css為準。

主要資訊層級：頁名／說明→狀態總數→搜尋條件→案件表格→分頁。表單將主旨、客戶、承辦、狀態與說明分組，轉派是獨立需要原因的操作。Focus有外框，label連結server控制項，錯誤以alert呈現；小螢幕改橫向導覽、單欄表單，寬表格在自身容器捲動。

實際1600×900、1280×720與390×844素材見docs/media。沒有裝飾動畫、外部前端框架、虛構數據卡片或AI生成視覺。
