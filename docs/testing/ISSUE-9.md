# Issue #9：歷史承辦人停用／角色變更 E2E

分類：Synthetic Scenario / Regression。來源原Issue #9的公開案例紀錄；本版不含私人Issue討論。

## 既有根因與修復範圍

合併前缺陷是 `AssignedTo.SelectedValue` 只綁有效 Agent/Manager，當既有承辦人停用或降為Viewer，資料庫保存的ID不在選單而可能中斷。main已在LoadCase保留標示「原承辦人（已停用或角色已變更，ID …）」的唯讀選項；新轉派目標仍只列有效角色。本次實測該修復已可正常工作，因此沒有再改產品功能或重設UI。

## 專屬測試

`tests/E2E/historical-assignee.spec.js` 有Disable、Demote兩項：

1. 在獨立LegacyLensShowcaseE2E建立專用有效Agent，主管透過真正ASPX表單建立並指派案件。
2. fixture只可在LegacyLensShowcaseE2E修改`e2e-history-*`帳號，分別停用或改為Viewer。
3. 主管reload原案件HTTP200，確認舊承辦標示、原歷程與有效新目標；舊承辦不會進入新轉派選項。
4. 非主管開啟同案，無轉派控制項；強行啟用Save仍收到後端403，DB承辦人不變。
5. 主管填理由轉派給agent2；確認畫面、原History、新History、TransferOperations及Audit各自正確。
6. finally恢復專用帳號的active/Agent狀態。全程不修改LegacyLens展示DB、LegacyLensShowcaseTests或其他共用DB。

fixture的DB名稱固定且會用DB_NAME再次核對；沒有測試用HTTP後門或放寬驗證。瀏覽器套件由獨立IIS Express應用複本讀LegacyLensShowcaseE2E，credentials也是該DB自己的ignored本機檔。

## 本版實際結果

Showcase獨立目錄重新執行專屬2/2與完整13/13 Chromium E2E；58/58 NUnit（25Unit、33SQL）通過，無skipped。結果：[showcase-e2e.json](showcase-e2e.json)、[showcase-nunit.xml](showcase-nunit.xml)。本版的遠端驗證見 [CI](CI.md)。

readiness-*報告為私人來源當時58/13的歷史公開副本，不冒充本版的新結果。原Issue／PR討論保留私人，必要行為與斷言皆在本頁及測試原始碼。
