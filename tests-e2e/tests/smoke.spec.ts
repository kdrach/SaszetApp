import { test, expect } from '@playwright/test';

test('Backend API health check is responding', async ({ request }) => {
  const apiUrl = process.env.API_URL || 'http://localhost:5000';
  const response = await request.get(`${apiUrl}/health`);
  expect([200, 401]).toContain(response.status());
});

test('Frontend mobile is served', async ({ page }) => {
  const mobileUrl = process.env.MOBILE_URL || 'http://localhost:3010';
  page.on('pageerror', exception => {
    console.error(`Uncaught exception: "${exception}"`);
    throw exception;
  });
  await page.goto(mobileUrl);
  const root = await page.locator('#root');
  await expect(root).toBeVisible();
});

test('Frontend admin is served', async ({ page }) => {
  const adminUrl = process.env.ADMIN_URL || 'http://localhost:3011';
  page.on('pageerror', exception => {
    console.error(`Uncaught exception: "${exception}"`);
    throw exception;
  });
  await page.goto(adminUrl);
  const root = await page.locator('#root');
  await expect(root).toBeVisible();
});
