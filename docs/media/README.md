# 真實媒體交付

| 素材 | 實際內容 |
|---|---|
| cover.png | 1600×900真實案件搜尋，Noise/Closed官方分類紀錄 |
| case-search.png | 真正SQL搜尋與分頁的完整頁面 |
| case-detail.png | 合成展示案件的實際表單與歷程 |
| customers.png | 合成客戶查詢與維護表單 |
| reassignment-history.png | 實際主管轉派、原因及歷程 |
| audit-report.png | 真正SQL統計與Audit |
| mobile-form.png | 390×844 viewport的實際表單，非真實手機驗證 |
| web001-before.png | 隔離受控故障的真正失敗測試畫面 |
| workflow.gif | 20秒真實操作錄影節錄，約1.67MB |
| demo.mp4 | 118秒、1280×720真實錄影，約1.57MB，繁體中文字幕 |
| demo.zh-TW.srt / demo.en.srt | 繁中／英文字幕 |
| architecture.svg | 根據實際source、project refs與graphify檢查的圖 |
| sql-benchmark.png / .svg | matplotlib從原始samples產生的效能圖 |
| evidence.html | 錄影中開啟的真實Benchmark報告頁，非應用Mock Dashboard |

`capture-manifest.json`保存取得時間、viewport、實際案件URL、分類與page error陣列。raw WebM留ignored .runtime，交付的是壓縮MP4與GIF；兩者均小於2MB，合理保存於Private Repository，不另發布Release或外部託管。

已檢視桌面／行動版截圖及影片40s/70s畫面；MP4已由FFmpeg完整解碼檢查。素材全部來自真正執行；不使用AI圖片，未把靜態圖串成假操作。重新產生：先準備localhost:5191受控複本，再執行figures.py與capture.js；需matplotlib／FFmpeg／Playwright及已匯入資料。
