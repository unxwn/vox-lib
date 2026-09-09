import { defineConfig, devices } from '@playwright/test'

// Focus movement, live region announcements and keyboard traversal only behave
// correctly in a real browser, so the accessibility outcomes in SC-002, SC-003
// and SC-004 are measured here rather than in a simulated document.
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : [['list']],

  use: {
    baseURL: process.env.VOXLIB_WEB_ORIGIN ?? 'http://localhost:5173',
    trace: 'on-first-retry',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  // Reuses an already running dev server locally, and starts one in CI. The API
  // has to be up separately: the catalogue reads it, and the dev server only
  // proxies through to it.
  webServer: {
    command: 'pnpm dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
})
