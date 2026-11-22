# 重跑 SQL-001

原始結果來自 `maintenance/sql001` 階段、CHANGE-001 schema 之前。以現在 schema/不同硬體重跑可能有不同 pages與latency，應保存為新實驗。

為避免干擾展示DB，建立專用 LegacyLensBenchmark；不刪除既有資料。先 restore/build，再使用本機原快照或重新下載。

```powershell
powershell.exe -NoProfile -File tools/database/setup.ps1 -DatabaseName LegacyLensBenchmark
powershell.exe -NoProfile -File tools/database/seed.ps1 -DatabaseName LegacyLensBenchmark
python tools/data-import/download.py --limit 100000
powershell.exe -NoProfile -File tools/data-import/import.ps1 -DatabaseName LegacyLensBenchmark -Limit 10000
powershell.exe -NoProfile -File tools/benchmark/run.ps1 -DatabaseName LegacyLensBenchmark -Scale 10000
powershell.exe -NoProfile -File tools/data-import/import.ps1 -DatabaseName LegacyLensBenchmark -Limit 50000
powershell.exe -NoProfile -File tools/benchmark/run.ps1 -DatabaseName LegacyLensBenchmark -Scale 50000
powershell.exe -NoProfile -File tools/data-import/import.ps1 -DatabaseName LegacyLensBenchmark -Limit 100000
powershell.exe -NoProfile -File tools/benchmark/run.ps1 -DatabaseName LegacyLensBenchmark -Scale 100000
```

新增 DatabaseName 參數為最終重現工具的安全改善；較早 SQL-001 source 只用 LegacyLens。需要當時 source 時，請以新的獨立開發環境重跑，勿把現有DB刪掉降版。

Benchmark會操作兩個具名實驗索引並更新統計；執行前另存現有 docs/benchmarks 結果，工具將產出新的同名 results/plans/io。40 samples預設，少於20次拒絕；5 warmups。資料量必須精確符合Scale。

評估全部samples、P50/P95、plan、reads與結果等價；warm cache、排序非隨機子集、Open選擇性及本機單client皆限制外推，沒有production SLA結論。
