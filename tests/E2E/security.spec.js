const { test, expect } = require('@playwright/test');
const { login } = require('./helpers');
test('stale browser edit is rejected and the latest title remains', async ({ page, context }) => {
  await login(page);
  await page.locator('table a').first().click();
  const url = page.url();
  const stale = await context.newPage();
  await stale.goto(url);
  const freshTitle = '併發驗證：最新內容 ' + Date.now();
  await page.getByLabel('案件主旨（必填）', { exact: true }).fill(freshTitle);
  await page.getByRole('button', { name: '儲存案件', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('案件已儲存');
  await stale.getByLabel('案件主旨（必填）', { exact: true }).fill('不應覆寫的舊版本');
  await stale.getByRole('button', { name: '儲存案件', exact: true }).click();
  await expect(stale.getByRole('alert')).toContainText('資料已被其他人更新');
  await page.reload();
  await expect(page.getByLabel('案件主旨（必填）', { exact: true })).toHaveValue(freshTitle);
});
test('tampered ViewState is rejected without leaking diagnostics', async ({ page }) => {
  await login(page);
  await page.locator('table a').first().click();
  await page.locator('input[name="__VIEWSTATE"]').evaluate(input => input.value = 'tampered');
  const response = page.waitForResponse(r => r.request().method() === 'POST' && r.url().includes('CaseEdit.aspx'));
  await page.getByRole('button', { name: '儲存案件', exact: true }).click();
  expect((await response).status()).toBe(500);
  await expect(page.getByRole('heading', { name: '暫時無法完成操作', exact: true })).toBeVisible();
  await expect(page.locator('body')).not.toContainText('Exception');
  await expect(page.locator('body')).not.toContainText('ConnectionString');
});
test('mobile viewport keeps forms and navigation usable without document overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await login(page);
  await expect(page.getByRole('heading', { name: '案件總覽', exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole('link', { name: '建立案件', exact: true }).first().click();
  await expect(page.getByLabel('案件主旨（必填）', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
