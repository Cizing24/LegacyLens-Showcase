# Showcase 公開前安全審查

日期：2026-11-22（Asia/Taipei）。此審查只涵蓋獨立 LegacyLens-Showcase 快照及其全新 Git 物件。原私人 Repository 的完整歷史、私人審查清單與敏感原始證據均未匯入。

**目前：驗證中，尚未宣告 Ready for Public。** Repository 必須維持 Private，Public 切換由擁有者決定；沒有網站部署。

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
