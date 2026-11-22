const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { spawnSync } = require('node:child_process');
const { chromium } = require('playwright');
const { login, credentials } = require('../../tests/E2E/helpers');
let captureBrowser;

async function main() {
  fs.mkdirSync('docs/media', { recursive: true });
  const browser = await chromium.launch();
  captureBrowser = browser;
  const auth = await browser.newContext({ baseURL: 'http://localhost:5188' });
  const authPage = await auth.newPage();
  await login(authPage);
  const state = await auth.storageState();
  await auth.close();
  const context = await browser.newContext({ baseURL: 'http://localhost:5188', storageState: state,
    viewport: { width: 1280, height: 720 }, recordVideo: { dir: '.runtime/media', size: { width: 1280, height: 720 } } });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  const started = Date.now();
  async function until(second) {
    const remaining = second * 1000 - (Date.now() - started);
    if (remaining > 0) await page.waitForTimeout(remaining);
  }
  async function fill(label, value) { await page.getByLabel(label, { exact: true }).fill(value); }
  async function click(name) { await page.getByRole('button', { name, exact: true }).click(); }
  async function loginAt(origin) {
    await page.goto(origin + '/Login.aspx');
    await fill('帳號', 'manager');
    await fill('密碼', credentials.password);
    await click('登入');
    await page.getByRole('heading', { name: '案件總覽', exact: true }).waitFor();
  }
  console.log('Recording live overview and real source-data search');
  await page.goto('/Cases.aspx');
  await until(5);
  await fill('案件主旨', 'Noise');
  await page.getByLabel('狀態', { exact: true }).selectOption('Closed');
  await click('查詢案件');
  await until(13);
  console.log('Recording actual case creation');
  await page.goto('/CaseEdit.aspx');
  await fill('案件主旨（必填）', '合成展示：報表更新後的服務案件');
  await page.getByLabel('客戶（必填）', { exact: true }).selectOption({ label: '示範客戶：北區服務中心' });
  await page.getByLabel('承辦人（必填）', { exact: true }).selectOption({ label: 'agent' });
  await fill('案件說明', '合成展示情境：接手既有服務案件，確認重現條件、保留修改歷程，並由主管依承辦專長轉派。');
  await until(24);
  await click('儲存案件');
  await page.getByRole('alert').filter({ hasText: '案件已儲存' }).waitFor();
  const caseUrl = page.url();
  await page.getByLabel('處理狀態', { exact: true }).selectOption('InProgress');
  await click('儲存案件');
  await until(33);
  console.log('Recording injected lifecycle failure on isolated localhost port');
  const faultUrl = caseUrl.replace('localhost:5188', 'localhost:5191');
  await loginAt('http://localhost:5191');
  await page.goto(faultUrl);
  await fill('案件主旨（必填）', 'WEB-001：這份草稿在注入版本會遺失');
  await until(39);
  await page.getByLabel('分類提示（Postback）', { exact: true }).selectOption({ label: '技術維護' });
  await page.getByText('已切換分類提示；尚未儲存的案件內容仍保留。', { exact: true }).waitFor();
  const lost = await page.getByLabel('案件主旨（必填）', { exact: true }).inputValue();
  if (lost.startsWith('WEB-001')) throw new Error('Injected fault did not lose the draft');
  await until(46);
  console.log('Recording guarded binding and retained draft');
  await loginAt('http://localhost:5188');
  await page.goto(caseUrl);
  const retained = '合成展示：Postback 後仍保留的服務案件';
  await fill('案件主旨（必填）', retained);
  await page.getByLabel('分類提示（Postback）', { exact: true }).selectOption({ label: '技術維護' });
  await page.getByText('已切換分類提示；尚未儲存的案件內容仍保留。', { exact: true }).waitFor();
  if (await page.getByLabel('案件主旨（必填）', { exact: true }).inputValue() !== retained) throw new Error('Fixed draft was not retained');
  await until(57);
  await click('儲存案件');
  await until(62);
  console.log('Recording actual measured SQL evidence');
  await page.goto(pathToFileURL(path.resolve('docs/media/evidence.html')).href);
  await until(80);
  console.log('Recording audited reassignment and history');
  await page.goto(caseUrl);
  await page.getByLabel('新承辦人', { exact: true }).selectOption({ label: 'agent2' });
  await fill('轉派原因（5～500 字）', '合成展示：依報表維護專長調整承辦，保留交接原因');
  await page.getByRole('heading', { name: '案件轉派', exact: true }).scrollIntoViewIfNeeded();
  await until(88);
  await click('確認轉派');
  await page.getByRole('alert').filter({ hasText: '案件已儲存' }).waitFor();
  await page.getByRole('heading', { name: '處理歷程', exact: true }).scrollIntoViewIfNeeded();
  await until(101);
  await page.goto('/Reports.aspx');
  await until(109);
  await page.goto(pathToFileURL(path.resolve('docs/media/architecture.svg')).href);
  await until(118);
  const video = page.video();
  await context.close();
  const videoPath = await video.path();
  console.log('Saving actual screenshots at desktop and mobile sizes');
  const screens = await browser.newContext({ baseURL: 'http://localhost:5188', viewport: { width: 1600, height: 900 } });
  const shot = await screens.newPage();
  await login(shot);
  await shot.getByLabel('案件主旨', { exact: true }).fill('Noise');
  await shot.getByLabel('狀態', { exact: true }).selectOption('Closed');
  await shot.getByRole('button', { name: '查詢案件', exact: true }).click();
  await shot.screenshot({ path: 'docs/media/cover.png' });
  await shot.screenshot({ path: 'docs/media/case-search.png', fullPage: true });
  await shot.goto(caseUrl);
  await shot.screenshot({ path: 'docs/media/case-detail.png', fullPage: true });
  await shot.getByRole('heading', { name: '案件轉派', exact: true }).scrollIntoViewIfNeeded();
  await shot.screenshot({ path: 'docs/media/reassignment-history.png' });
  await shot.goto('/Customers.aspx');
  await shot.getByLabel('客戶名稱', { exact: true }).fill('示範');
  await shot.getByRole('button', { name: '查詢客戶', exact: true }).click();
  await shot.screenshot({ path: 'docs/media/customers.png' });
  await shot.goto('/Reports.aspx');
  await shot.screenshot({ path: 'docs/media/audit-report.png' });
  await shot.setViewportSize({ width: 390, height: 844 });
  await shot.goto('/CaseEdit.aspx');
  await shot.screenshot({ path: 'docs/media/mobile-form.png', fullPage: true });
  const capture = { recordedAtUtc: new Date().toISOString(), durationTargetSeconds: 118, videoSource: videoPath,
    viewport: { width: 1280, height: 720 }, screenshots: '1600x900 desktop; 390x844 mobile', caseUrl,
    classification: 'Actual IIS Express application; public categories and synthetic customer/case fields; controlled failure only on isolated port 5191', pageErrors: errors };
  fs.writeFileSync('docs/media/capture-manifest.json', JSON.stringify(capture, null, 2));
  await screens.close();
  await browser.close();
  if (errors.length) throw new Error('Browser page errors: ' + errors.join('; '));
  console.log('Encoding real browser recording and its 20-second GIF excerpt');
  function ffmpeg(args) {
    const result = spawnSync('ffmpeg', ['-y', '-hide_banner', '-loglevel', 'error', ...args], { stdio: 'inherit' });
    if (result.status !== 0) throw new Error('FFmpeg encoding failed');
  }
  ffmpeg(['-i', videoPath, '-t', '118', '-vf', "subtitles=docs/media/demo.zh-TW.srt:force_style='FontName=Microsoft JhengHei,FontSize=17,Outline=1,MarginV=18'", '-c:v', 'libx264', '-preset', 'medium', '-crf', '29', '-pix_fmt', 'yuv420p', '-r', '24', '-an', '-movflags', '+faststart', 'docs/media/demo.mp4']);
  ffmpeg(['-ss', '13', '-t', '20', '-i', videoPath, '-filter_complex', '[0:v]fps=8,scale=800:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=96[p];[b][p]paletteuse=dither=bayer', '-loop', '0', 'docs/media/workflow.gif']);
  for (const file of ['docs/media/demo.mp4', 'docs/media/workflow.gif']) {
    if (fs.statSync(file).size > 8 * 1024 * 1024) throw new Error('Compress media below 8 MB before committing: ' + file);
  }
  console.log('Live screenshots, 118-second MP4 and 20-second genuine workflow GIF complete');
}
main().catch(async error => { console.error(error); if (captureBrowser) await captureBrowser.close(); process.exitCode = 1; });
