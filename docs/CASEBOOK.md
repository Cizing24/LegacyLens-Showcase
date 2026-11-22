# Engineering Casebook

| 分類 | 案例 | 問題與證據 | 決策／修正 | 本版可查閱證據 |
|---|---|---|---|---|
| Verified baseline | [Baseline](testing/BASELINE.md) | 真正 Web Forms／SQL CRUD、32個NUnit、5個初版E2E；後續誠實補註AutoPostBack測試盲點 | [ADR0001](decisions/0001-webforms-and-local-sql.md) | [Baseline紀錄](testing/BASELINE.md) · [快照邊界](SNAPSHOT.md) |
| Controlled Fault Injection | [WEB-001](incidents/WEB-001.md) | 真正POST後，無條件Page_Load使草稿回到SQL舊值；[red](testing/web001-red.json) | 初次request binding；[green](testing/web001-green.json)；[ADR0002](decisions/0002-postback-binding.md) | [RCA](incidents/WEB-001.md) · [ADR](decisions/0002-postback-binding.md) |
| Public Dataset Observation＋受控實驗 | [SQL-001](incidents/SQL-001.md) | 10k／50k／100k官方紀錄、plan／reads／40samples；[結果](benchmarks/RESULTS.md) | 固定predicate片段＋covering indexes，保留OFFSET契約；[ADR0003](decisions/0003-search-plan.md) | [RCA](incidents/SQL-001.md) · [ADR](decisions/0003-search-plan.md) |
| Synthetic Scenario | [CHANGE-001](incidents/CHANGE-001.md) | 新規則、舊入口繞過風險、migration/backfill、失敗rollback、重送與併發 | Additive schema＋ledger＋保留資料的feature rollback；[ADR0004](decisions/0004-reassignment-compatibility.md) | [RCA](incidents/CHANGE-001.md) · [ADR](decisions/0004-reassignment-compatibility.md) |

## 實際開發問題

- [DEV-001](incidents/DEV-001-bootstrap.md)：Windows PowerShell編碼、rollback不回收IDENTITY造成寫死ID失敗。
- [DEV-002](incidents/DEV-002-postback-csp.md)：CSP阻擋原生AutoPostBack，以及原測試沒有等待POST的盲點。
- WEB-001 harness還原source保留舊時間戳，MSBuild跳過編譯；WriteAllText更新timestamp後才真正取得green，見WEB-001原順序。
- Migration003同批DDL的metadata編譯失敗；改用固定SQL的延後編譯，見ADR0004。
- 第一輪Windows CI自動LocalDB instance失敗；明確建立已安裝engine後實際重跑成功，見[CI](testing/CI.md)。

這些紀錄反映實際執行次序，不是事後虛構企業調查歷程。本版保留原決策與測試證據；原Issue／PR／Commit鏈留在私人來源，未匯入本版Git歷史。

## 2026-11-22 歷史承辦人專屬回歸

[來源 Issue #9](testing/ISSUE-9.md)補齊停用／降為Viewer承辦人的2項E2E；main既有顯示修復不需再改產品行為。本版獨立LegacyLensShowcaseE2E驗證舊案載入、原承辦標示、轉派、History/Audit及非主管403，詳見 [測試紀錄](testing/ISSUE-9.md)。目前本機58NUnit／13E2E，歷史案例數字仍按原紀錄保留。
