import { test, expect } from '@playwright/test';

test('Backend API health check is responding', async ({ request }) => {
  const response = await request.get('http://localhost:5000/health');
  expect(response.ok()).toBeTruthy();
});

test('Frontend mobile is served', async ({ page }) => {
  await page.goto('http://localhost:3010/');
  // Just expecting the page to not be a 404 or connection refused
  // The default Vite app title or empty is fine, we just want to ensure it loads
  const body = await page.locator('body');
  await expect(body).toBeVisible();
});

test('Frontend admin is served', async ({ page }) => {
  await page.goto('http://localhost:3011/');
  const body = await page.locator('body');
  await expect(body).toBeVisible();
});
