# CHANGE-001 — 新增案件轉派與相容性

分類：Synthetic Scenario（需求）；Migration 失敗測試為 Controlled Fault Injection。Issue：[CHANGE-001 #6](CHANGE-001.md)。

## Problem / Impact

Baseline 可在一般編輯直接修改承辦人，但沒有轉派理由、Request ID 或重送處理。新需求要追溯交接，又不能替舊紀錄杜撰原因或刪除歷史。

## 影響分析與選項

影響 Domain request、Application 規則、CaseStore 一般編輯入口、TransferStore transaction、Web Forms 表單、schema/backfill、NUnit/Playwright 與 rollback runbook。沒有更換 Framework 或既有頁碼契約。選項與取捨完整記錄於 [ADR 0004](../decisions/0004-reassignment-compatibility.md)。

## 規則與實作

- 限 Manager；目標需 active 且角色為 Agent/Manager。
- 目標不能是目前承辦人；Closed 案件先由主管重新開啟。
- 原因 trim 後 5～500 字；request key、case ID、8-byte rowversion 必填。
- 同一 request key 與同一 payload 為 idempotent；不同 payload 拒絕。
- Transaction 一次完成 owner update、TransferOperations、History、Audit；失敗不留下部分狀態。
- 一般編輯入口不得直接轉派，不能只靠 UI disabled。

## Migration / Backfill / 舊資料

003 增加 policy version、History 原因／request、操作 ledger 與安全回復 flag。舊案件 policy=1，新的 default=2；先前 History 的新增欄位保持 null。初稿真實執行遇到同批 DDL metadata 編譯問題，修正後才成功；不把初稿當成通過。

## Verification

NUnit 包含 manager 成功、agent 拒絕、Closed／同承辦人／短原因／Viewer 目標／inactive agent 拒絕、舊版更新入口阻擋、過期版本不寫 ledger、相同 key 重送及併發重送、不同 payload conflict。獨立 temporary DB 驗證 migration 受控失敗 rollback、重跑兩次、backfill、新 default、legacy read/history 與 feature rollback。詳見 final-nunit.xml 與 MigrationTests。

Playwright 真正建立案件、先測短原因驗證，再完成轉派並查核新承辦人與原因歷程；Agent 不顯示主管控制項。轉派 SQL 仍再次查驗權限，UI 並非授權邊界。

## Rollback / Remaining Risks

003-reassignment-rollback.sql 只暫停新轉派；既有案件及 ledger 保留。重新開啟必須由操作人員明確更新旗標，migration rerun 不改動它。未驗證 production deployment 或任意歷史 binary 的可寫入降版；不提供會刪掉新資料的 automatic down migration。
