const { defineConfig } = require('@playwright/test');
process.env.LEGACYLENS_E2E_DATABASE = 'LegacyLensShowcaseE2E';
module.exports = defineConfig({
  testDir: './tests/E2E', workers: 1, retries: 0, timeout: 30000,
  captureGitInfo: { commit: false, diff: false },
  reporter: [['list'], ['json', { outputFile: 'docs/testing/e2e-results.json' }]],
  use: { baseURL: process.env.LEGACYLENS_URL || 'http://localhost:5194', viewport: { width: 1440, height: 900 }, trace: 'retain-on-failure', screenshot: 'only-on-failure' }
});
