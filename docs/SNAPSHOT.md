# Showcase 快照來源與邊界

本版是 LegacyLens 經審查的最新 main 快照，來源版本識別為 `453ffa2d6c998b4b3a4f342bc89a0c9e10f984f2`，整理於 2026-11-22（Asia/Taipei）。完整開發歷程、原始 Baseline Tags、Issues、PRs 與私有審查證據保存在原私人 Repository，原資料與設定不變。本版使用全新 Git 初始化，沒有 Fork、Mirror、原 `.git`、原 Tags、歷史 Commit 或指向來源的 Git References。

Initial Commit 只表示這份展示快照的起點，**不是完整開發歷史**。文件中的來源 Issue 編號、舊 Commit 識別與歷史測試數字是原工程紀錄；不代表這些 Git 物件或 GitHub 討論存在於本版。必要查證資料均保存在本版的 [Casebook](CASEBOOK.md)、[RCA](incidents/WEB-001.md)、[ADR](decisions/0001-webforms-and-local-sql.md)、[Benchmark](benchmarks/RESULTS.md) 及 [測試報告](testing/README.md)，不要求讀者取得私人 Repository 權限。

## 保留與調整

- `src/` 產品程式碼與 `database/` Schema／Migration 保持來源 bytes；沒有新增產品功能。
- 實測 Benchmark 的 samples、結果 JSON、Execution Plans、IO、資料 manifest 與既有二進位媒體保持 bytes。文字報告只調整私有連結，不改實測數字、方法或決策。
- 來源歷史測試報告保留原本結果並標為歷史；本版自己的 `showcase-*` 報告與 GitHub Actions 另外執行，不借用來源 CI 成功紀錄。
- 新測試使用 `LegacyLensShowcaseTests`／`LegacyLensShowcaseE2E` 與 port 5194；不修改來源的測試 DB。Migration regression 自建唯一 DB，完成後只清理自己建立的資料。
- 使用公開 noreply 提交身分；不帶入私有審查清單、原始敏感報告、內部任務清單、runtime、credentials、browser traces、資料庫與 build output。
- CI 只在清理報告成功時上傳 artifacts；清理失敗不會退回上傳未清理檔案。

完整檔案差異與 SHA-256 見 [snapshot-differences.json](snapshot-differences.json)。此清單是檔案 provenance，不是對來源 Git History 的引用。

## 重現限制

原大型 NYC 311 快照不提交 Git。可依 [Provenance](datasets/PROVENANCE.md) 與 [重跑指南](benchmarks/REPRODUCE.md) 下載、驗 hash 並重跑；上游可能回寫資料，若 hash 不同必須建立新的實驗紀錄，不能冒用原 Benchmark 數字。歷史原始快照留在私人來源，公開讀者不保證能取得完全相同 bytes。

本版不含原 Baseline checkout，但附帶的 WEB-001 隔離故障／修復工具可從目前程式碼重現 Page Lifecycle 問題。歷史決策與測試結果未改寫成新事件。

Repository 建立時為 Private；是否轉為 Public 由擁有者決定。沒有部署網站。
