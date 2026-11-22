# Deployment / Rollback Runbook

本次僅本機IIS Express，未部署正式／公開服務。以下為已驗證的維護與feature rollback。

## 升級

1. 記錄目前source/binary ref、DB版本與備份，停止寫入。
2. restore/build，先在LegacyLensTests執行schema/migrations/NUnit。
3. 對展示DB套additive migration。003舊policy=1、新default=2，保留舊History。
4. 啟動IIS Express，確認登入、舊資料、一般編輯、真正Postback、轉派及Audit。
5. 失敗先保存Correlation ID與原錯誤，不關閉驗證或擴權讓測試過關。

## 已驗證的安全回復

對正確開發DB執行 `database/migrations/003-reassignment-rollback.sql`：只停用Reassignment flag，保留schema、原因、ledger與案件。現行binary繼續提供查詢／編輯，UI及後端都暫停新轉派。MigrationTests已驗證資料／歷程數不變，重跑migration不擅自重啟功能。

排除問題與完成回歸後，由操作人員明確執行 `UPDATE dbo.FeatureFlags SET Enabled=1 WHERE Name='Reassignment'`。這是feature rollback，不是任意binary降版；Baseline舊寫入入口不懂新理由規則，不應降版後開放寫入。未提供會刪除新資料的automatic down migration。

## 正式環境邊界

非本次已完成項目：TLS/Secure cookies、受保護的穩定machineKey、最小權限應用DB帳號、備份還原演練、IIS identity/ACL、集中日誌、容量／長時間併發測試、正式監控及存取控管。對外部署需另行授權，不能把本機結果視為production-ready認證。
