# Troubleshooting

| 現象 | 檢查與處理 |
|---|---|
| MSBuild/WebApplication.targets缺少 | 用Visual Studio官方Installer補ASP.NET workload及4.8 SDK/Targeting Pack；使用正常UAC，Runtime不能取代Developer Pack |
| LocalDB automatic instance建立失敗 | setup選最新工具並明確create已安裝engine的MSSQLLocalDB。查SqlLocalDB versions/info與SQL errorlog；第一輪CI確實發生，修正後重跑成功 |
| 中文亂碼或ASPX parse error | Web.config明確UTF-8；Windows PowerShell腳本用UTF-8 BOM |
| 首次POST就MAC error | 確認Session cookie及OnInit持久化CsrfKey；不可關閉MAC/EventValidation |
| 選單切換沒POST | 查site.js的requestSubmit與CSP，等server hint；不能只看client選取值 |
| 找不到密碼 | 首次Seed寫.runtime/credentials-LegacyLens.json；既有使用者不會被Seed覆寫。勿提交本機密碼 |
| Migration003 Invalid column RequestId | 使用修正後延後編譯的固定DDL；初稿真實失敗見ADR0004 |
| 複本修復仍是舊行為 | WriteAllText更新source時間戳再編譯；Copy-Item保留舊時間可能讓MSBuild跳過 |
| 案件版本衝突 | 重新載入最新資料，不移除rowversion檢查 |
| 轉派暫停 | 查FeatureFlags；rollback保留歷史，migration重跑不重新啟用 |
| Benchmark規模不符 | public rows須等於Scale；使用獨立DB按10k→50k→100k匯入 |
| 一般錯誤頁 | 依Correlation ID查App_Data/logs/events.jsonl；log不記密碼、表單內容或DB憑證 |

DEV-001/DEV-002是真實開發問題；Controlled Fault Injection的預期失敗需檢查正確斷言，不能把任意環境錯誤算成成功重現。
