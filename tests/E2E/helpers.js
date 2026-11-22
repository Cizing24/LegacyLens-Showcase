const fs = require('node:fs');
const { expect } = require('@playwright/test');
const database = process.env.LEGACYLENS_E2E_DATABASE || 'LegacyLens';
if (!['LegacyLens', 'LegacyLensShowcaseE2E'].includes(database)) throw new Error('Unknown local credential fixture.');
const credentials = JSON.parse(fs.readFileSync(`.runtime/credentials-${database}.json`, 'utf8').replace(/^\uFEFF/, ''));
async function login(page, username = 'manager') {
  await page.goto('/Login.aspx');
  await page.getByLabel('帳號', { exact: true }).fill(username);
  await page.getByLabel('密碼', { exact: true }).fill(credentials.password);
  await page.getByRole('button', { name: '登入', exact: true }).click();
  await expect(page.getByRole('heading', { name: '案件總覽', exact: true })).toBeVisible();
}
module.exports = { login, credentials };
