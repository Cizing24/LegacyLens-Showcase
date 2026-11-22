# LegacyLens Showcase 交付報告

日期：2026-11-22（Asia/Taipei）。本版為獨立審查快照，完整開發歷程保存在私人來源，未匯入其Git歷史。快照來源、保留與差異見 [SNAPSHOT](SNAPSHOT.md)。

## Repository 與範圍

Repository：[Cizing24/LegacyLens-Showcase](https://github.com/Cizing24/LegacyLens-Showcase)。建立時Private，Public切換由擁有者自行操作；沒有部署、Fork、Mirror、原Tags或來源Git refs。Initial Commit不是完整開發歷史。必要證據均能在本版文件內查閱。

src產品程式碼與database Schema／Migration維持來源bytes。登入／Session／角色、客戶管理、案件建立編輯／歷程、搜尋分頁／統計、主管轉派／稽核維持既有功能。只有測試DB名稱／port、CI與公開文件／掃描調整。

## 工程案例

- [WEB-001](incidents/WEB-001.md)：Controlled Fault Injection，Page_Load重新binding覆寫Postback草稿；採!IsPostBack；保留同一真正POST斷言的1 failed→1 passed報告與ADR0002。
- [SQL-001](incidents/SQL-001.md)：Public Dataset Observation＋本機實驗；10k／50k／100k，比較predicate rewrite與covering indexes。原samples、IO、actual plans及結果等價檢查保留。
- [CHANGE-001](incidents/CHANGE-001.md)：Synthetic Scenario，additive migration／backfill、舊入口規則、idempotency、rowversion及保留資料的feature rollback；回歸包含權限、併發、重跑及失敗回復。
- [歷史承辦人回歸](testing/ISSUE-9.md)：停用／降為Viewer後主管開啟舊案並轉派；History、ledger、Audit保留，未授權403。兩項專屬E2E在本版重新執行。

取捨、根因與實際開發問題見 [Casebook](CASEBOOK.md) 及 [ADR](decisions/0001-webforms-and-local-sql.md)，未偽造本版Initial Commit以前的Git歷程。

## 本版實際驗證

Release build成功；58/58 NUnit（25Unit＋33SQL integration/regression）、13/13 Chromium E2E、6/6工具測試通過，無skipped。npm audit各嚴重度皆0。證據：[本版NUnit](testing/showcase-nunit.xml)、[本版E2E](testing/showcase-e2e.json)、[本版CI紀錄](testing/CI.md)。遠端以本版Actions實際結果為準，來源報告僅作歷史資料。

## 資料與 Benchmark

NYC311官方五欄allowlist，來源取得日2026-11-22（台灣時間），100,000筆及100,000唯一鍵；客戶、承辦與測試情境為合成。Metadata licenseId=null，依官方政策／Terms標示來源，沒有改稱MIT／CC0。JSON／CSV SHA-256、字典與查詢URL保留於 [Provenance](datasets/PROVENANCE.md) 與manifest；大型原快照不提交。

100k歷史本機warm cache結果保持不變：Baseline P50/P95=22.3420/26.7085ms、Logical Reads=3255；Rewrite＋indexes=1.0832/1.3413ms、87reads。每方案5warmups與40samples，offsets0/20/200有序ID相同。索引儲存與寫入有代價；未測cold cache、長壓力或寫入overhead，不是production SLA／企業ROI。完整 [RESULTS](benchmarks/RESULTS.md) 與原始JSON／plans／IO均保留。

## 媒體與公開審查

真實 [封面](media/cover.png)、[功能截圖清單](media/README.md)、[20秒GIF](media/workflow.gif)、[118秒MP4](media/demo.mp4)、[架構圖](media/architecture.svg)、[Benchmark圖](media/sql-benchmark.png) 重用原bytes；字幕保留錄製當時54/11，README已標明目前58/13。沒有重製媒體或新增線上Demo。

隱私／全部Git物件與依賴檢查、公開結論見 [PUBLIC_READINESS](security/PUBLIC_READINESS.md)。來源私人審查清單與敏感原始證據未帶入。原來源Repository的設定、Branches、Tags、PRs與歷史不變。

## 未驗證範圍

真實手機、Safari／Firefox、螢幕閱讀器、正式IIS、cold cache、多使用者長壓力、索引寫入成本與完整滲透測試仍未驗證。詳見 [Known Limitations](KNOWN_LIMITATIONS.md)。本次未重跑效能Benchmark，保留既有實測數據。
