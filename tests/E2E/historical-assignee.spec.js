const { test, expect } = require('@playwright/test');
const { execFileSync } = require('node:child_process');
const { login } = require('./helpers');

function fixture(operation, userId, caseId) {
  if (process.env.LEGACYLENS_E2E_DATABASE !== 'LegacyLensShowcaseE2E') throw new Error('Historical-assignee mutations require the isolated LegacyLensShowcaseE2E application.');
  const args = ['-NoProfile', '-File', 'tools/testing/assignee-fixture.ps1', '-Operation', operation];
  if (userId) args.push('-UserId', String(userId));
  if (caseId) args.push('-CaseId', String(caseId));
  return JSON.parse(execFileSync('powershell.exe', args, { encoding: 'utf8', windowsHide: true }).replace(/^\uFEFF/, '').trim());
}

for (const mutation of ['Disable', 'Demote']) {
  test(`Issue #9: manager transfers a historical assignee after ${mutation}`, async ({ page, browser }) => {
    const original = fixture('Create');
    let agentContext;
    try {
      await login(page);
      await page.goto('/CaseEdit.aspx');
      await page.getByLabel('案件主旨（必填）', { exact: true }).fill(`Issue #9 synthetic ${mutation} ${original.userId}`);
      await page.getByLabel('承辦人（必填）', { exact: true }).selectOption(String(original.userId));
      await page.getByRole('button', { name: '儲存案件', exact: true }).click();
      await expect(page.getByRole('alert')).toContainText('案件已儲存');
      const caseUrl = page.url();
      const caseId = Number(new URL(caseUrl).searchParams.get('id'));
      fixture(mutation, original.userId);
      const response = await page.reload();
      expect(response.status()).toBe(200);
      const selected = page.getByLabel('承辦人（必填）', { exact: true }).locator('option:checked');
      await expect(selected).toHaveText(`原承辦人（已停用或角色已變更，ID ${original.userId}）`);
      await expect(page.locator('table')).toContainText(original.username);
      const target = page.getByLabel('新承辦人', { exact: true });
      await expect(target.locator(`option[value="${original.userId}"]`)).toHaveCount(0);
      await target.selectOption({ label: 'agent2' });

      agentContext = await browser.newContext({ baseURL: new URL(caseUrl).origin });
      const agentPage = await agentContext.newPage();
      await login(agentPage, 'agent');
      await agentPage.goto(caseUrl);
      await expect(agentPage.getByRole('button', { name: '確認轉派', exact: true })).toHaveCount(0);
      const save = agentPage.getByRole('button', { name: '儲存案件', exact: true });
      await save.evaluate(button => button.disabled = false);
      const denied = agentPage.waitForResponse(r => r.url().includes('CaseEdit.aspx') && r.request().method() === 'POST');
      await save.click();
      expect((await denied).status()).toBe(403);
      expect(fixture('Inspect', original.userId, caseId).AssignedTo).toBe(original.userId);

      await page.getByLabel('轉派原因（5～500 字）', { exact: true }).fill(`合成回歸：原承辦人 ${mutation}，主管移交有效承辦人`);
      await page.getByRole('button', { name: '確認轉派', exact: true }).click();
      await expect(page.getByRole('alert')).toContainText('案件已儲存');
      await expect(page.locator('table')).toContainText('Reassigned');
      await expect(page.locator('table')).toContainText(original.username);
      const result = fixture('Inspect', original.userId, caseId);
      expect(result).toMatchObject({ database: 'LegacyLensShowcaseE2E', Username: 'agent2', HistoryCount: 2, OriginalHistoryCount: 1, TransferHistoryCount: 1, TransferCount: 1, TransferAuditCount: 1 });
      await page.goto('/Reports.aspx');
      await expect(page.locator('table').last().getByRole('row').filter({ hasText: 'Reassigned' }).filter({ has: page.getByRole('cell', { name: String(caseId), exact: true }) })).toHaveCount(1);
      console.log(JSON.stringify({ issue: 9, mutation, database: result.database, originalHistory: result.OriginalHistoryCount, transferHistory: result.TransferHistoryCount, transferAudit: result.TransferAuditCount, unauthorizedHttpStatus: 403 }));
    } finally {
      if (agentContext) await agentContext.close();
      fixture('Restore', original.userId);
    }
  });
}
