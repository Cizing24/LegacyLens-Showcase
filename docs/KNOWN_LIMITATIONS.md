# Known Limitations

- 本版是獨立展示快照，不含私人來源的Git開發歷史或Baseline Tags；必要工程證據保存在本版文件。公開審查只涵蓋本版，來源歷史未改寫。詳見 [公開審查](security/PUBLIC_READINESS.md) 與 [快照邊界](SNAPSHOT.md)。
- Issue #9的承辦人停用／降為Viewer分支現已通過2項專屬Chromium E2E，完整套件為13項；這不擴大為真實裝置／跨瀏覽器驗證。

- 本機Windows/IIS Express/SQL Server2019 LocalDB驗證；沒有正式部署、真實企業使用者、商業ROI或合作／產品相容性主張。
- 311是公共服務請求，並非B2B支援；客戶、承辦人、展示案例為合成欄位。排序時間窗最早100k不是隨機樣本，Closed占比較高。
- 公開來源會修正；原快照保存在本機且有hash，重下載不同hash不等於原實驗bytes。
- Benchmark為warm cache、本機單client與單一主要filter；未測cold cache、索引寫入成本、多使用者長壓力或production SQL。substring LIKE與深頁OFFSET有限制。
- Customer查詢上限100筆，案件每頁20筆。共同工作空間，不是multi-tenant隔離。
- Forms Authentication無MFA/SSO；本機HTTP cookies無Secure。CSP保留Web Forms所需unsafe-inline，無unsafe-eval。對外前需正式安全設計。
- 自我Code/Security Review不是獨立稽核或完整penetration test；npm audit只涵蓋當時已知公告。
- 真實手機、Safari、Firefox、螢幕閱讀器與完整人工WCAG稽核未驗證；390px Chromium為viewport模擬。
- 回復為保留schema/history的feature rollback，不代表任意舊binary可安全寫入。
- MP4/GIF為真實瀏覽器操作，展示局部流程，不能取代原始測試證據。影片無旁白，有繁體中文字幕與英文SRT。
