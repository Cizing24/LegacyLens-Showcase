const { test, expect } = require('@playwright/test');
const { login } = require('./helpers');
test('CHANGE-001 manager transfers a new case with history and validation', async ({ page }) => {
  await login(page);
  await page.goto('/CaseEdit.aspx');
  await page.getByLabel('案件主旨（必填）', { exact: true }).fill('CHANGE-001 合成轉派驗證 ' + Date.now());
  await page.getByLabel('承辦人（必填）', { exact: true }).selectOption({ label: 'agent' });
  await page.getByRole('button', { name: '儲存案件', exact: true }).click();
  await page.getByLabel('新承辦人', { exact: true }).selectOption({ label: 'agent2' });
  await page.getByLabel('轉派原因（5～500 字）', { exact: true }).fill('短');
  await page.getByRole('button', { name: '確認轉派', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('5～500');
  await page.getByLabel('轉派原因（5～500 字）', { exact: true }).fill('合成情境：依專長調整服務案件承辦人');
  await page.getByRole('button', { name: '確認轉派', exact: true }).click();
  await expect(page.getByLabel('承辦人（必填）', { exact: true })).toHaveValue(await page.getByLabel('新承辦人', { exact: true }).locator('option', { hasText: 'agent2' }).getAttribute('value'));
  await expect(page.locator('table')).toContainText('Reassigned');
  await expect(page.locator('table')).toContainText('合成情境：依專長調整服務案件承辦人');
});
test('CHANGE-001 agent sees no manager transfer controls', async ({ page }) => {
  await login(page, 'agent');
  await page.locator('table a').first().click();
  await expect(page.getByRole('button', { name: '確認轉派', exact: true })).toHaveCount(0);
});
