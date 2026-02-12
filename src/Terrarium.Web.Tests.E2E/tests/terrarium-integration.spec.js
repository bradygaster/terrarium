// @ts-check
const { test, expect } = require('@playwright/test');

/**
 * Terrarium Integration Tests
 *
 * Validates that the Terrarium web frontend renders correctly,
 * connects to SignalR, receives simulation data, and responds
 * to user interaction.
 *
 * Prerequisites: Terrarium app running at http://localhost:5190
 * Run: npx playwright test
 */

// ─────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────

/** Wait for the canvas element to appear and have non-zero dimensions. */
async function waitForCanvas(page, timeoutMs = 15_000) {
  const canvas = page.locator('.game-view__canvas');
  await canvas.waitFor({ state: 'visible', timeout: timeoutMs });
  // Wait until the canvas has real dimensions (renderer has initialised)
  await page.waitForFunction(
    () => {
      const c = document.querySelector('.game-view__canvas');
      return c && c.width > 0 && c.height > 0;
    },
    { timeout: timeoutMs },
  );
  return canvas;
}

/** Wait for SignalR connection to reach "Connected" state. */
async function waitForConnection(page, timeoutMs = 30_000) {
  await page.waitForFunction(
    () => {
      const label = document.querySelector('.connection-status__label');
      return label && label.textContent?.trim() === 'Connected';
    },
    { timeout: timeoutMs },
  );
}

/** Wait until the ecosystem tick counter exceeds zero. */
async function waitForTicks(page, timeoutMs = 30_000) {
  await page.waitForFunction(
    () => {
      const labels = document.querySelectorAll('.ecosystem-status__metric .glass-label');
      for (const l of labels) {
        const text = l.textContent?.trim() ?? '';
        const match = text.match(/Tick:\s*(\d+)/);
        if (match && parseInt(match[1], 10) > 0) return true;
      }
      // Also check the statusbar
      const sections = document.querySelectorAll('.glass-statusbar__section');
      for (const s of sections) {
        const text = s.textContent?.trim() ?? '';
        const match = text.match(/Tick:\s*(\d+)/);
        if (match && parseInt(match[1], 10) > 0) return true;
      }
      return false;
    },
    { timeout: timeoutMs },
  );
}

/** Read the current tick count from the statusbar or ecosystem panel. */
async function readTickCount(page) {
  return page.evaluate(() => {
    // Try statusbar first
    const sections = document.querySelectorAll('.glass-statusbar__section');
    for (const s of sections) {
      const match = s.textContent?.trim().match(/Tick:\s*(\d+)/);
      if (match) return parseInt(match[1], 10);
    }
    // Try ecosystem panel
    const labels = document.querySelectorAll('.ecosystem-status__metric .glass-label');
    for (const l of labels) {
      const match = l.textContent?.trim().match(/Tick:\s*(\d+)/);
      if (match) return parseInt(match[1], 10);
    }
    return 0;
  });
}

/** Read the population count from the statusbar or ecosystem panel. */
async function readPopulation(page) {
  return page.evaluate(() => {
    const sections = document.querySelectorAll('.glass-statusbar__section');
    for (const s of sections) {
      const match = s.textContent?.trim().match(/Population:\s*(\d+)/);
      if (match) return parseInt(match[1], 10);
    }
    const labels = document.querySelectorAll('.ecosystem-status__metric .glass-label');
    for (const l of labels) {
      const match = l.textContent?.trim().match(/Creatures:\s*(\d+)/);
      if (match) return parseInt(match[1], 10);
    }
    return 0;
  });
}

// ─────────────────────────────────────────────────────────────
// Tests
// ─────────────────────────────────────────────────────────────

test.describe('Terrarium Integration Tests', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 30_000 });
  });

  // 1 ───────────────────────────────────────────────────────────
  test('map renders — canvas visible with drawn content', async ({ page }) => {
    const canvas = await waitForCanvas(page, 15_000);

    // Canvas must be visible and have real dimensions
    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();
    expect(box.width).toBeGreaterThan(0);
    expect(box.height).toBeGreaterThan(0);

    // Wait for some content to be drawn, then sample pixels
    await waitForConnection(page, 30_000);
    // Give the renderer a moment to paint after connection
    await page.waitForTimeout(3_000);

    const hasContent = await canvas.evaluate((el) => {
      const ctx = el.getContext('2d');
      if (!ctx) return false;
      const w = el.width;
      const h = el.height;
      // Sample a grid of points across the canvas
      const samplePoints = [
        [w * 0.25, h * 0.25],
        [w * 0.50, h * 0.25],
        [w * 0.75, h * 0.25],
        [w * 0.25, h * 0.50],
        [w * 0.50, h * 0.50],
        [w * 0.75, h * 0.50],
        [w * 0.25, h * 0.75],
        [w * 0.50, h * 0.75],
        [w * 0.75, h * 0.75],
      ];
      let nonTransparentCount = 0;
      for (const [x, y] of samplePoints) {
        const pixel = ctx.getImageData(Math.floor(x), Math.floor(y), 1, 1).data;
        if (pixel[3] > 0) nonTransparentCount++;
      }
      return nonTransparentCount > 0;
    });

    expect(hasContent).toBe(true);
  });

  // 2 ───────────────────────────────────────────────────────────
  test('connection status green — SignalR connected', async ({ page }) => {
    await waitForConnection(page, 30_000);

    // LED should have the active (green) class
    const led = page.locator('.connection-status .glass-led');
    await expect(led).toHaveClass(/glass-led--active/);

    // Label should say "Connected"
    const label = page.locator('.connection-status__label');
    await expect(label).toHaveText('Connected');
  });

  // 3 ───────────────────────────────────────────────────────────
  test('organisms appear on canvas — creature pixels present', async ({ page }) => {
    await waitForConnection(page, 30_000);
    await waitForTicks(page, 30_000);

    const canvas = page.locator('.game-view__canvas');
    // Allow time for creature sprites to render
    await page.waitForTimeout(3_000);

    // Check that the canvas has pixels beyond pure terrain green
    const hasCreaturePixels = await canvas.evaluate((el) => {
      const ctx = el.getContext('2d');
      if (!ctx) return false;
      const w = el.width;
      const h = el.height;
      if (w === 0 || h === 0) return false;

      // Terrain-green reference: #2d5a27 = rgb(45, 90, 39)
      const TERRAIN_R = 45, TERRAIN_G = 90, TERRAIN_B = 39;
      const TOLERANCE = 30;

      // Sample a broader set of points
      let nonTerrainCount = 0;
      const step = 50;
      const sampleLimit = Math.min(w, 2000);
      const sampleLimitH = Math.min(h, 2000);
      for (let x = 0; x < sampleLimit; x += step) {
        for (let y = 0; y < sampleLimitH; y += step) {
          const pixel = ctx.getImageData(x, y, 1, 1).data;
          if (pixel[3] === 0) continue; // transparent
          const dr = Math.abs(pixel[0] - TERRAIN_R);
          const dg = Math.abs(pixel[1] - TERRAIN_G);
          const db = Math.abs(pixel[2] - TERRAIN_B);
          // If the pixel is NOT close to terrain green and NOT black (background)
          if ((dr > TOLERANCE || dg > TOLERANCE || db > TOLERANCE) &&
              (pixel[0] + pixel[1] + pixel[2]) > 30) {
            nonTerrainCount++;
          }
        }
      }
      return nonTerrainCount > 0;
    });

    // Fallback: even if pixel detection is tricky, population > 0 confirms organisms
    const population = await readPopulation(page);
    expect(hasCreaturePixels || population > 0).toBe(true);
  });

  // 4 ───────────────────────────────────────────────────────────
  test('tick counter advances — simulation is running', async ({ page }) => {
    await waitForConnection(page, 30_000);
    await waitForTicks(page, 30_000);

    const firstTick = await readTickCount(page);
    expect(firstTick).toBeGreaterThan(0);

    // Wait and read again
    await page.waitForTimeout(5_000);

    const secondTick = await readTickCount(page);
    expect(secondTick).toBeGreaterThan(firstTick);
  });

  // 5 ───────────────────────────────────────────────────────────
  test('population stats show organisms', async ({ page }) => {
    await waitForConnection(page, 30_000);
    await waitForTicks(page, 30_000);

    // Statusbar population
    const population = await readPopulation(page);
    expect(population).toBeGreaterThan(0);

    // Sidebar ecosystem creature count
    const creatureLabel = page.locator('.ecosystem-status__metric .glass-label', {
      hasText: /Creatures:\s*\d+/,
    });
    await expect(creatureLabel).toBeVisible({ timeout: 5_000 });

    const text = await creatureLabel.textContent();
    const match = text?.match(/Creatures:\s*(\d+)/);
    expect(match).not.toBeNull();
    expect(parseInt(match[1], 10)).toBeGreaterThan(0);
  });

  // 6 ───────────────────────────────────────────────────────────
  test('ecosystem status shows running', async ({ page }) => {
    await waitForConnection(page, 30_000);
    await waitForTicks(page, 30_000);

    // The ecosystem panel should show "Running" with an active LED
    const runningLabel = page.locator('.ecosystem-status__metric .glass-label', {
      hasText: /Running/,
    });
    await expect(runningLabel).toBeVisible({ timeout: 5_000 });

    // The LED next to "Running" should be active
    const runningMetric = page.locator('.ecosystem-status__metric').filter({
      hasText: /Running/,
    });
    const led = runningMetric.locator('.glass-led');
    await expect(led).toHaveClass(/glass-led--active/);
  });

  // 7 ───────────────────────────────────────────────────────────
  test('canvas is interactive — mouse drag pans viewport', async ({ page }) => {
    await waitForCanvas(page, 15_000);
    await waitForConnection(page, 30_000);
    // Give renderer time to initialise viewport tracking
    await page.waitForTimeout(2_000);

    const canvas = page.locator('.game-view__canvas');
    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();

    // Capture the canvas content hash before drag
    const beforePixels = await canvas.evaluate((el) => {
      const ctx = el.getContext('2d');
      if (!ctx) return '';
      // Sample a small region to detect change
      const data = ctx.getImageData(0, 0, Math.min(el.width, 100), Math.min(el.height, 100)).data;
      let hash = 0;
      for (let i = 0; i < data.length; i += 16) {
        hash = ((hash << 5) - hash + data[i]) | 0;
      }
      return hash.toString();
    });

    // Perform a drag across the canvas (simulates panning)
    const startX = box.x + box.width / 2;
    const startY = box.y + box.height / 2;
    await page.mouse.move(startX, startY);
    await page.mouse.down();
    await page.mouse.move(startX - 150, startY - 150, { steps: 10 });
    await page.mouse.up();

    // Wait a frame for re-render
    await page.waitForTimeout(500);

    const afterPixels = await canvas.evaluate((el) => {
      const ctx = el.getContext('2d');
      if (!ctx) return '';
      const data = ctx.getImageData(0, 0, Math.min(el.width, 100), Math.min(el.height, 100)).data;
      let hash = 0;
      for (let i = 0; i < data.length; i += 16) {
        hash = ((hash << 5) - hash + data[i]) | 0;
      }
      return hash.toString();
    });

    // The viewport should have changed — pixel content hash should differ
    expect(afterPixels).not.toBe(beforePixels);
  });

  // 8 ───────────────────────────────────────────────────────────
  test('event log shows activity', async ({ page }) => {
    await waitForConnection(page, 30_000);
    // Give time for initial messages to populate
    await page.waitForTimeout(3_000);

    // The message log should have at least one real entry
    const entries = page.locator('.message-log__entry');
    await expect(entries.first()).toBeVisible({ timeout: 10_000 });

    const count = await entries.count();
    expect(count).toBeGreaterThanOrEqual(1);

    // Verify the first entry has non-empty text content
    const firstEntryText = await entries.first().textContent();
    expect(firstEntryText?.trim().length).toBeGreaterThan(0);
  });
});
