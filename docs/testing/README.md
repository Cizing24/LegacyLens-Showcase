# 執行測試與證據

先完成 [Local Development](../LOCAL_DEVELOPMENT.md) 的 restore/build。

```powershell
powershell.exe -NoProfile -File tools/database/setup.ps1 -DatabaseName LegacyLensShowcaseTests
powershell.exe -NoProfile -File tools/database/seed.ps1 -DatabaseName LegacyLensShowcaseTests
powershell.exe -NoProfile -File tools/database/migrate.ps1 -DatabaseName LegacyLensShowcaseTests
./tests/UnitTests/bin/Release/LegacyLens.Tests.exe --result=docs/testing/local-nunit.xml
```

目前本機 **58/58 通過**：25 Unit、33 真正 SQL integration/regression，沒有 skipped。[showcase-nunit.xml](showcase-nunit.xml) 保存本版重新執行結果。`--where="cat != Integration"` 只跑25個無DB測試，不能取代完整驗收。MigrationTests 自建唯一 LegacyLensMigrationTests_* DB，完成後只清理自己建立且名稱通過檢查的 DB。

歷史：初始交付54/54（22Unit、32SQL）；合併前review新增4項後為58/58，當時瀏覽器套件11項。這些歷史報告與CI紀錄保留，不把它們改寫成本次已新增測試。

```powershell
npm ci
npx playwright install chromium
./tools/testing/start-e2e.ps1
npm run test:e2e
npm audit --audit-level=high
```

目前本機 **13/13 Chromium E2E 通過**：原有11項，加上Issue #9的停用／降為Viewer兩項專屬回歸。全部連真正SQL-backed ASPX，沒有Mock backend。`npm audit` 當次回報0 vulnerabilities。

E2E使用**獨立LegacyLensShowcaseE2E DB與localhost:5194應用複本**；start-e2e依序setup/seed/migrate，複製編譯後Web檔並只在ignored複本改connection string。展示DB／root Web.config保持原設定。Playwright設定指定對應credentials；不要把此套件的URL指到展示或共用環境。Issue #9 fixture僅修改自己建立的e2e-history-*帳號，finally恢復狀態，詳見 [ISSUE-9.md](ISSUE-9.md)。

工具測試共6項：原有3項報告清理及3項公開掃描測試。執行方式：`python -m unittest discover -s tests/ToolTests -v`，與58個NUnit分開計算。

原交付階段的本機報告為 [final-nunit.xml](final-nunit.xml)、[final-e2e.json](final-e2e.json)（54 NUnit）；新增加的四個測試在最新遠端 CI 執行，去識別化來源歷史報告保存在本版，私人Actions不是必要證據。browser trace可能包含cookies，只留本機、不提交或上傳。Benchmark的40次查詢不算成40個功能測試。

本版重新執行報告是 [showcase-nunit.xml](showcase-nunit.xml)、[showcase-e2e.json](showcase-e2e.json)，本版遠端結果見 [CI](CI.md)。readiness-*及final-*皆為來源歷史副本。可提交報告清除了個人路徑、身分Email與host identity，測試名稱、結果、斷言與測量值保留。未清理原檔存本機ignored目錄；CI也會在artifact上傳前清理，並關閉Playwright的Git身分擷取。來源敏感歷史未匯入本版；本版Git物件另行檢查。

## WEB-001 重現

```powershell
./tools/reproduction/prepare-web001.ps1 -Port 5190
$env:LEGACYLENS_URL='http://localhost:5190'
npx playwright test tests/E2E/web001.spec.js
# 預期草稿 retention assertion 失敗；不是任何非零 exit 都算重現。
./tools/reproduction/repair-web001.ps1
npx playwright test tests/E2E/web001.spec.js
# 相同 HTTP POST-aware assertion 預期通過。
Remove-Item Env:LEGACYLENS_URL
```

注入只改 ignored `.runtime/web001-*` 複本；位置/PID 存 `.runtime/web001.json`，不要對公開服務套用。新 feature 需要先套用 migrations。

## CI

Windows workflow 實際 restore/build、建立 LocalDB、跑 NUnit、啟動 IIS Express與Playwright；缺少 DB/IIS 直接fail。reports保存14天，未上傳credentials/binlog/traces。遠端結果見 CI.md；首次 provisioning failure 亦保留紀錄。
