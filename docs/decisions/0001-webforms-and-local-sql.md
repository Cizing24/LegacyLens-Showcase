# ADR 0001 — Web Forms、ADO.NET 與本機 SQL Server

日期：2026-11-22。狀態：Accepted；執行環境驗證持續記錄於 testing。

## 背景與證據

作品明確要求 .NET Framework 4.8、Web Forms 與 SQL Server；目錄原先為空。已找到 Visual Studio 2022 MSBuild，但缺少 WebApplication targets、4.8 reference assemblies、IIS Express 與 SQL Server。Runtime 已存在，Runtime 不能取代 Developer Pack。

## 方案與決定

採傳統 Web Application csproj、Domain / Application / Infrastructure / Web 四個具實際責任的 assembly。ADO.NET 已包含於 Framework，方便保存實際 SQL 與查詢計畫；Dapper 可減少映射程式，但本作品查詢有限，不新增套件。Entity Framework 會增加非核心遷移成本，且不利呈現 SQL 調查。ASP.NET Core 不符合需求，排除。

優先 SQL Server LocalDB 與 Windows Integrated Security，避免 repository 內含資料庫密碼。LocalDB 具真正 SQL Server 查詢計畫及 transaction，但只適合開發，無法代表企業部署負載。安裝透過官方 Visual Studio Installer，要求 UAC 時使用正常授權流程。

## 安全與範圍

Forms Authentication cookie 與重新建立的 session；session-bound ViewState MAC 防護 Postback，資料操作再次查驗角色。PBKDF2-SHA256 每個密碼獨立 salt；登入失敗暫時鎖定。SQL 參數明確指定型別；更新使用 rowversion 並將修改、history、audit 放入同一 transaction。

## 代價與驗證

自行維護精簡 Forms Authentication 流程，缺少 MFA/企業 SSO；求職作品不擴大到身分平台。驗證需包含錯誤登入、權限、過期編輯與資料庫 rollback，只有編譯通過不足以驗收。
