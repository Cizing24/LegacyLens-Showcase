const { test, expect } = require('@playwright/test');
const { login } = require('./helpers');
test('WEB-001 unsaved edits survive a real AutoPostBack', async ({ page }) => {
  await login(page);
  await page.locator('table a').first().click();
  const title = page.getByLabel('案件主旨（必填）', { exact: true });
  const original = await title.inputValue();
  const draft = 'WEB-001 draft ' + Date.now();
  await title.fill(draft);
  await page.getByLabel('案件說明', { exact: true }).fill('Unsaved description must also survive.');
  const postback = page.waitForResponse(response => response.request().method() === 'POST' && response.url().includes('CaseEdit.aspx'));
  await page.getByLabel('分類提示（Postback）', { exact: true }).selectOption({ label: '技術維護' });
  expect((await postback).status()).toBe(200);
  await expect(page.getByText('已切換分類提示；尚未儲存的案件內容仍保留。', { exact: true })).toBeVisible();
  const actual = await title.inputValue();
  console.log(JSON.stringify({ classification: 'Controlled Fault Injection', caseId: 'WEB-001', original, expected: draft, actual, retained: actual === draft }));
  await expect(title).toHaveValue(draft);
  await expect(page.getByLabel('案件說明', { exact: true })).toHaveValue('Unsaved description must also survive.');
});
