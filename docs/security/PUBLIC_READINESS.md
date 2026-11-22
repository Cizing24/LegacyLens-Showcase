# Showcase 公開前安全審查

日期：2026-11-22（Asia/Taipei）。此審查只涵蓋獨立 LegacyLens-Showcase 快照及其全新 Git 物件。原私人 Repository 的完整歷史、私人審查清單與敏感原始證據均未匯入。

**結論：Ready for Public（僅指本 Repository 的原始碼與展示資料公開）。** Repository 必須維持 Private，Public 切換由擁有者決定；沒有網站部署。

## 範圍與方法

- 只從已核對的最新來源 main 匯出追蹤檔案，不複製 `.git`、refs、Tags、runtime、原始 browser traces 或 credentials；全新 `git init`，使用 GitHub noreply 提交。
- 檢查目前檔案、staged內容與所有Git物件（包含不可達物件），驗證只有一個獨立root、無原歷史／Tag物件、無來源remote。
- Gitleaks及個人資訊規則檢查私人Email、Windows使用者路徑、主機識別、憑證與不應提交的診斷產物；掃描輸出不列私人值。
- XML／JSON報告只清理身分與路徑metadata，保留測試名稱、斷言、結果、時間與數值。私有原始副本留ignored目錄，不提交。
- 全部既有二進位媒體與來源SHA-256一致；檢查PNG／GIF、MP4完整解碼及metadata，沿用實際錄製來源與畫面審查。不宣稱逐幀人工稽核。
- NYC311實驗只含五欄allowlist與合成欄位；manifest、原始samples、plans、IO與SQL實測數字不變。大型原始快照不提交。
- 本版獨立執行Release、NUnit／SQL、E2E、工具測試、npm audit及自己的Windows CI。來源CI報告只作歷史資料。

## 安全邊界

公開原始碼與對外部署是不同範圍。應用預設拒絕非本機request；本機HTTP、Forms Authentication與Web Forms CSP限制仍見 [Known Limitations](../KNOWN_LIMITATIONS.md)。本審查不代表正式部署認證、完整滲透測試或未知漏洞不存在。

依賴版本不變，npm audit結果另由本版執行；[NuGet公告查核](nuget-audit.json)保存同日固定Framework相容套件的官方feed查驗。Dependabot不作未執行的成功證據。原實測Benchmark為本機單client warm cache，不是production SLA。

原來源歷史中的隱私風險不因本版建立而消失；來源保持Private且完全不改寫。Ready判斷只適用於本版可見檔案及獨立Git歷史。

## 實際結果與限制

本版Initial Commit的Windows CI已成功，58/58 NUnit、13/13 Chromium E2E、6/6工具測試，0 skipped；下載本版artifacts核對，詳見 [CI紀錄](../testing/CI.md)。本機也在本版目錄重新執行同樣測試。npm audit各嚴重度均0。

目前追蹤檔案與完整本機Git物件規則檢查無私人Email／使用者路徑／本機主機識別命中；Gitleaks的提交歷史與準備提交檔案匯出均0憑證命中。所有提交使用GitHub noreply；只有一個獨立root，不含原Commit／Tag物件、alternate object store或來源remote。最終交付仍再次核對遠端refs與最新CI，原始私有掃描報告不提交。

PNG檢查、GIF／MP4完整解碼通過；MP4為118秒，全部既有二進位媒體hash與來源一致。檢視封面與案例截圖、影片抽樣畫面及原錄製來源，metadata無私人路徑／身分。抽樣不是逐幀人工稽核；已保留這項限制。歷史測試報告的私人CI href只以私有來源標記取代，不變更stats或斷言；讀者不需開啟私有PR查證必要工程內容。

本結論不替原私人來源歷史背書，也不保證未知敏感內容或漏洞絕不存在。目前無尚待保留的已知私人資訊、真實憑證或必要私有證據依賴阻擋本版原始碼公開。Repository仍Private，沒有自行切換Visibility或部署網站。
