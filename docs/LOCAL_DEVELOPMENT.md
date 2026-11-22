# Local Development / Database Setup

## 需求與實測環境

Windows 10/11、Visual Studio 2022 ASP.NET and web development workload、.NET Framework **4.8 Developer Pack**（Runtime 不足）、IIS Express 10、SQL Server LocalDB 2019+、Node.js 22+，以及目前 Repository 的存取權。Python 3 用於資料下載；FFmpeg 與 matplotlib 僅重產媒體時需要。

本機實測為 Windows build 26300、PowerShell 7.6.9 / Windows PowerShell 5.1、MSBuild 17.14、SQL Server 2019 LocalDB、Node 24.22.1、Python 3.14.2、Playwright 1.58.2 / Chromium 145、FFmpeg 8.0。其他組合未聲稱已驗證。

## Clone / Build

```powershell
git clone https://github.com/Cizing24/LegacyLens-Showcase.git
cd LegacyLens-Showcase
git switch main
./tools/restore.ps1
./tools/build.ps1
```

傳統 Web Application csproj，由 vswhere 找 MSBuild，必要的 WebApplication targets 缺少會直接失敗。不要改建為 ASP.NET Core。NUnit/NUnitLite 3.14 下載至 ignored packages；應用 backend 僅使用 Framework 內建函式庫。

## Database / Seed / Migration

使用 Windows PowerShell 5.1 的 Framework System.Data.SqlClient；腳本為 UTF-8 BOM。

```powershell
powershell.exe -NoProfile -File tools/database/setup.ps1
powershell.exe -NoProfile -File tools/database/seed.ps1
powershell.exe -NoProfile -File tools/database/migrate.ps1
```

連線 `(localdb)\MSSQLLocalDB`，Windows Integrated Security。setup 只建立不存在的 LegacyLens-prefixed DB，schema/migrations 可重跑。Seed 首次建立四位合成帳號 manager / agent / agent2 / viewer、三個客戶與45筆案件；密碼隨機產生，保存在 `.runtime/credentials-LegacyLens.json`，請在本機讀取，勿提交或放入影片。Seed 重跑保留既有資料／密碼，不重設 IDENTITY。Web.config 不含資料庫憑證。

## Run

```powershell
./tools/start.ps1
```

開啟 http://localhost:5188/Login.aspx。IIS Express 隱藏執行；PID、stdout/error 存 `.runtime/server-5188.json` 及同目錄 log。停止前確認記錄的 PID 仍是本專案 IIS Express，不影響其他專案。

應用拒絕非本機 request。Manager 可維護客戶、初次指派、編輯任意案件、重新開啟與轉派；Agent 可建立／編輯自己案件；Viewer 只能讀取。每個 request 重新查驗 active Actor。此為共同工作空間，非逐客戶租戶隔離。

## Public data（選用）

```powershell
python tools/data-import/download.py --limit 100000
powershell.exe -NoProfile -File tools/data-import/import.ps1 -Limit 100000
```

五欄 allowlist、SHA-256、時間窗與轉換見 [PROVENANCE](datasets/PROVENANCE.md)。沒有公開資料也能使用45筆合成 Seed 操作全部功能。來源更新使 hash 不同時，需記錄新版本並重跑 Benchmark。
