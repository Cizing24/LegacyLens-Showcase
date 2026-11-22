# Skills 與工具來源

## 重用

- Ponytail full：最小完整實作，不引入大型框架；規格要求的驗證與媒體仍完成。
- using-agent-skills、planning-and-task-breakdown、incremental-implementation、git-workflow-and-versioning、test-driven-development、documentation-and-adrs：規劃、分支／Tag、red/green、回歸與ADR。
- code-review-and-quality、security-and-hardening：五軸自我review、輸入／授權／transaction／安全回歸；未聲稱獨立審核。
- graphify：重用已安裝CLI，src結構174nodes／417edges；ASPX等未支援檔案另用source/reference查驗。
- Impeccable：企業工作台的資訊層級、表單、focus、RWD與實際render檢視；未使用影像生成。
- speak-human-tw：查閱台灣文字、事實與數字保留原則；依本次「一次完成、不逐段確認」的明確授權處理新撰README，未改寫使用者原稿。

## 調查但不重複安裝

- [dotnet/skills](https://github.com/dotnet/skills)：實際列出現有SKILL.md並閱讀MSBuild Skill，採用必要import、incremental timestamp與真實build證據的指引。大量ASP.NET Core/Blazor/.NET新版工具不適用本作，未假設它們都支援Web Forms。
- [addyosmani/agent-skills](https://github.com/addyosmani/agent-skills)：本機已有相同工作流程，直接重用。
- [microsoft/playwright-cli](https://github.com/microsoft/playwright-cli)：查驗官方用途與Skill位置；本作需要可提交的固定E2E與錄影腳本，因此使用官方Playwright package/API，未額外安裝重複CLI。
- [openai/skills](https://github.com/openai/skills)：查驗來源；本作沒有需要新增通用Skill的重複維護工作，未製造只為展示而存在的webforms-maintenance Skill。
- Trail-Code：目前已提供graphify與直接source檢查，沒有額外安裝需求。

未安裝新第三方Skills，未直接執行未知來源的破壞腳本。Skills沒有取代MSBuild、LocalDB、NUnit或實際瀏覽器驗證。

## 安裝／使用的開發元件

透過Visual Studio官方Installer與正常UAC補齊ASP.NET workload、.NET4.8 SDK/Targeting Pack、IIS Express與LocalDB。初次quiet installer因未elevated遭拒後，使用正常Windows UAC，沒有繞過權限。固定NuGet NUnit/NUnitLite、固定npm Playwright與Chromium；matplotlib放在專案ignored venv，FFmpeg為原有工具。
