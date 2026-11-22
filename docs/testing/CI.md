# Showcase Windows CI

本 Repository 使用自己的 [Windows Web Forms verification](https://github.com/Cizing24/LegacyLens-Showcase/actions/workflows/windows.yml)。來源私人 CI 的成功不作為本版 CI 通過證據。

## 本版執行狀態

本機已在獨立 Showcase 目錄執行 Release、58/58 NUnit（25 Unit、33 SQL integration/regression）、13/13 Chromium E2E，以及6/6工具測試（原有3項報告測試＋3項公開掃描測試），無 skipped。原始公開副本：[NUnit](showcase-nunit.xml)、[E2E](showcase-e2e.json)。

遠端 CI 尚待本版 Initial Commit 推送後執行，未以來源結果冒充。完成後由本頁記錄本版 Run 與去識別化報告。

## Workflow

Windows2022 Runner 實際 restore/build、建立全新 Hosted Runner 的 LocalDB、準備 LegacyLensShowcaseTests／LegacyLensShowcaseE2E、執行真正 NUnit與SQL、啟動IIS Express 5194並跑13項E2E、執行6項工具測試與npm audit。缺SQL/IIS直接fail。只有報告清理成功才上傳XML/JSON，保留14天；credentials、binlog、browser traces均不上傳。

CI只用合成Seed與測試資料，不在每次push下載100k或重跑SQL效能。100k報告是原本本機warm cache實驗，不是本版遠端Benchmark。

## 來源歷史證據

既有readiness-ci-nunit.xml／readiness-ci-e2e.json保存來源58/13的去識別化結果；ci-nunit.xml／ci-e2e.json及final-*保存來源原交付54/11。不得把這些檔案改標成本版的新執行。來源首次LocalDB自動instance失敗、明確初始化已安裝engine後重跑成功的實際經驗保留於本段與Casebook；完整私人run討論不包含在本版。

真實裝置、Safari／Firefox、正式IIS與production負載未驗證。
