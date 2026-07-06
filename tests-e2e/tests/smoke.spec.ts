import { test, expect } from '@playwright/test';

test('Backend API health check is responding', async ({ request }) => {
  const apiUrl = process.env.API_URL || 'http://localhost:5000';
  const response = await request.get(`${apiUrl}/health`);
  expect([200, 401]).toContain(response.status());
});

test('Frontend mobile is served', async ({ page }) => {
  const mobileUrl = process.env.MOBILE_URL || 'http://localhost:3010';
  await page.goto(mobileUrl);
  // Just expecting the page to not be a 404 or connection refused
  // The default Vite app title or empty is fine, we just want to ensure it loads
  const body = await page.locator('body');
  await expect(body).toBeVisible();
});

test('Frontend admin is served', async ({ page }) => {
  const adminUrl = process.env.ADMIN_URL || 'http://localhost:3011';
  await page.goto(adminUrl);
  const body = await page.locator('body');
  await expect(body).toBeVisible();
});
