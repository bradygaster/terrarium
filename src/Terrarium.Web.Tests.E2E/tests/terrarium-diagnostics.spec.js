// @ts-check
const { test, expect } = require('@playwright/test');

/**
 * Terrarium Diagnostic Tests
 * 
 * These are NOT regression tests. They capture browser behavior, console errors,
 * network traffic, and DOM state to help diagnose the connectivity problem between
 * the Blazor frontend (localhost:5190) and the Terrarium server (localhost:5180).
 * 
 * Run: npx playwright test --reporter=list
 */

const WEB_URL = 'http://localhost:5190';
const SERVER_URL = 'http://localhost:5180';

test.describe('Terrarium Diagnostic Suite', () => {

  // ─────────────────────────────────────────────────────────────
  // 1. Basic page load
  // ─────────────────────────────────────────────────────────────
  test('1. Page loads and title is correct', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Page Load Check');
    console.log('════════════════════════════════════════');

    const response = await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });

    console.log(`  HTTP status: ${response?.status()}`);
    console.log(`  URL after navigation: ${page.url()}`);
    console.log(`  Page title: ${await page.title()}`);

    const bodyHTML = await page.locator('body').innerHTML();
    console.log(`  Body HTML length: ${bodyHTML.length} chars`);
    console.log(`  Body preview (first 500 chars):\n${bodyHTML.substring(0, 500)}`);

    expect(response?.status()).toBe(200);
  });

  // ─────────────────────────────────────────────────────────────
  // 2. Network status indicator
  // ─────────────────────────────────────────────────────────────
  test('2. Connection status indicator — what state is it showing?', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Connection Status LED');
    console.log('════════════════════════════════════════');

    await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });

    // Wait for Blazor to finish rendering (interactive server mode)
    await page.waitForTimeout(5_000);

    // Look for the ConnectionStatus component
    const connectionStatusDiv = page.locator('.connection-status');
    const count = await connectionStatusDiv.count();
    console.log(`  .connection-status elements found: ${count}`);

    if (count > 0) {
      const ledSpan = page.locator('.connection-status .glass-led');
      const ledClasses = await ledSpan.getAttribute('class');
      console.log(`  LED CSS classes: "${ledClasses}"`);

      const labelSpan = page.locator('.connection-status .connection-status__label');
      const labelText = await labelSpan.textContent();
      console.log(`  Status label text: "${labelText}"`);

      const title = await ledSpan.getAttribute('title');
      console.log(`  LED title attribute: "${title}"`);

      // Determine status
      if (ledClasses?.includes('glass-led--active')) {
        console.log('  ✅ LED is ACTIVE (green) — Connected');
      } else if (ledClasses?.includes('glass-led--waiting')) {
        console.log('  🟡 LED is WAITING (yellow) — Connecting/Reconnecting');
      } else if (ledClasses?.includes('glass-led--idle')) {
        console.log('  🔴 LED is IDLE (red) — Disconnected');
      } else {
        console.log('  ❓ LED class not recognized');
      }
    }

    // Also check the PeerList's network status
    const peerListLed = page.locator('.peer-list .glass-led');
    if (await peerListLed.count() > 0) {
      const peerLedClasses = await peerListLed.getAttribute('class');
      const peerLabelText = await page.locator('.peer-list .glass-label').first().textContent();
      console.log(`  PeerList LED classes: "${peerLedClasses}"`);
      console.log(`  PeerList label text: "${peerLabelText}"`);
    }

    // Check the footer status bar
    const statusbar = page.locator('.glass-statusbar');
    if (await statusbar.count() > 0) {
      const statusbarText = await statusbar.textContent();
      console.log(`  Status bar text: "${statusbarText?.trim()}"`);
    }
  });

  // ─────────────────────────────────────────────────────────────
  // 3. Browser console errors
  // ─────────────────────────────────────────────────────────────
  test('3. Browser console errors and warnings', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Browser Console Messages');
    console.log('════════════════════════════════════════');

    /** @type {Array<{type: string, text: string, url: string}>} */
    const consoleMessages = [];

    page.on('console', msg => {
      consoleMessages.push({
        type: msg.type(),
        text: msg.text(),
        url: msg.location().url || '',
      });
    });

    page.on('pageerror', error => {
      console.log(`  🚨 PAGE ERROR: ${error.message}`);
    });

    await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });
    // Wait extra time for SignalR reconnect attempts, Blazor circuit setup, etc.
    await page.waitForTimeout(10_000);

    console.log(`\n  Total console messages captured: ${consoleMessages.length}`);
    console.log('  ───────────────────────────────────');

    // Group by type
    const errors = consoleMessages.filter(m => m.type === 'error');
    const warnings = consoleMessages.filter(m => m.type === 'warning');
    const info = consoleMessages.filter(m => m.type === 'info' || m.type === 'log');

    console.log(`  Errors: ${errors.length}`);
    errors.forEach((e, i) => {
      console.log(`    [ERR ${i + 1}] ${e.text.substring(0, 300)}`);
      if (e.url) console.log(`            source: ${e.url}`);
    });

    console.log(`  Warnings: ${warnings.length}`);
    warnings.forEach((w, i) => {
      console.log(`    [WARN ${i + 1}] ${w.text.substring(0, 300)}`);
    });

    console.log(`  Info/Log: ${info.length}`);
    info.forEach((m, i) => {
      console.log(`    [INFO ${i + 1}] ${m.text.substring(0, 200)}`);
    });
  });

  // ─────────────────────────────────────────────────────────────
  // 4. Network requests — what URLs does the browser call?
  // ─────────────────────────────────────────────────────────────
  test('4. Network traffic analysis — all requests from browser', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Network Request Analysis');
    console.log('════════════════════════════════════════');

    /** @type {Array<{method: string, url: string, resourceType: string, status: number|null, failure: string|null}>} */
    const networkRequests = [];

    page.on('request', request => {
      networkRequests.push({
        method: request.method(),
        url: request.url(),
        resourceType: request.resourceType(),
        status: null,
        failure: null,
      });
    });

    page.on('requestfinished', async request => {
      const entry = networkRequests.find(r => r.url === request.url() && r.status === null);
      if (entry) {
        const response = await request.response();
        entry.status = response?.status() ?? null;
      }
    });

    page.on('requestfailed', request => {
      const entry = networkRequests.find(r => r.url === request.url() && r.status === null);
      if (entry) {
        entry.failure = request.failure()?.errorText ?? 'unknown';
      }
    });

    await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });
    await page.waitForTimeout(10_000);

    console.log(`\n  Total network requests: ${networkRequests.length}`);
    console.log('  ───────────────────────────────────');

    // Categorize requests
    const blazorRequests = networkRequests.filter(r => r.url.includes('_blazor'));
    const signalrRequests = networkRequests.filter(r => 
      r.url.includes('signalr') || r.url.includes('/terrarium') || r.url.includes('/negotiate')
    );
    const apiRequests = networkRequests.filter(r => 
      r.url.includes('/api/') || r.url.includes(':5180')
    );
    const staticRequests = networkRequests.filter(r => 
      r.resourceType === 'stylesheet' || r.resourceType === 'script' || 
      r.resourceType === 'image' || r.resourceType === 'font'
    );
    const failedRequests = networkRequests.filter(r => r.failure !== null);
    const serverErrors = networkRequests.filter(r => r.status !== null && r.status >= 400);

    console.log(`\n  📡 Blazor circuit requests (${blazorRequests.length}):`);
    blazorRequests.forEach(r => {
      console.log(`    ${r.method} ${r.url} → ${r.status ?? 'FAILED: ' + r.failure}`);
    });

    console.log(`\n  🔌 SignalR/Hub requests (${signalrRequests.length}):`);
    signalrRequests.forEach(r => {
      console.log(`    ${r.method} ${r.url} → ${r.status ?? 'FAILED: ' + r.failure}`);
    });

    console.log(`\n  🌐 API/Server requests to :5180 (${apiRequests.length}):`);
    apiRequests.forEach(r => {
      console.log(`    ${r.method} ${r.url} → ${r.status ?? 'FAILED: ' + r.failure}`);
    });

    console.log(`\n  ❌ Failed requests (${failedRequests.length}):`);
    failedRequests.forEach(r => {
      console.log(`    ${r.method} ${r.url} → ${r.failure}`);
    });

    console.log(`\n  ⚠️ Server error responses (${serverErrors.length}):`);
    serverErrors.forEach(r => {
      console.log(`    ${r.method} ${r.url} → HTTP ${r.status}`);
    });

    console.log(`\n  📦 Static asset requests: ${staticRequests.length}`);
    console.log(`\n  All requests (full list):`);
    networkRequests.forEach((r, i) => {
      const statusStr = r.status !== null ? `HTTP ${r.status}` : (r.failure ? `FAILED: ${r.failure}` : 'pending');
      console.log(`    [${i + 1}] ${r.method} ${r.url.substring(0, 120)} → ${statusStr}`);
    });
  });

  // ─────────────────────────────────────────────────────────────
  // 5. Canvas element inspection
  // ─────────────────────────────────────────────────────────────
  test('5. Canvas element — does it exist and have content?', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Canvas Element Inspection');
    console.log('════════════════════════════════════════');

    await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });
    await page.waitForTimeout(5_000);

    // Check for any canvas elements
    const canvasElements = page.locator('canvas');
    const canvasCount = await canvasElements.count();
    console.log(`  Canvas elements found: ${canvasCount}`);

    for (let i = 0; i < canvasCount; i++) {
      const canvas = canvasElements.nth(i);
      const classes = await canvas.getAttribute('class') ?? '(none)';
      const width = await canvas.getAttribute('width') ?? await canvas.evaluate(el => el.width);
      const height = await canvas.getAttribute('height') ?? await canvas.evaluate(el => el.height);
      const style = await canvas.getAttribute('style') ?? '(none)';
      const boundingBox = await canvas.boundingBox();

      console.log(`\n  Canvas #${i + 1}:`);
      console.log(`    CSS classes: "${classes}"`);
      console.log(`    width attr/prop: ${width}`);
      console.log(`    height attr/prop: ${height}`);
      console.log(`    inline style: "${style}"`);
      console.log(`    bounding box: ${JSON.stringify(boundingBox)}`);

      // Check if canvas has any drawn content (non-blank)
      const hasContent = await canvas.evaluate(el => {
        const ctx = el.getContext('2d');
        if (!ctx) return 'no 2d context';
        const imageData = ctx.getImageData(0, 0, el.width || 1, el.height || 1);
        const data = imageData.data;
        let nonZero = 0;
        for (let j = 0; j < data.length; j += 4) {
          if (data[j] !== 0 || data[j + 1] !== 0 || data[j + 2] !== 0 || data[j + 3] !== 0) {
            nonZero++;
          }
        }
        return `${nonZero} non-transparent pixels out of ${el.width * el.height} total`;
      });
      console.log(`    Content check: ${hasContent}`);
    }

    // Check for the GameView component wrapper
    const gameView = page.locator('.game-view');
    console.log(`\n  .game-view elements: ${await gameView.count()}`);

    const viewport = page.locator('.terrarium-home__viewport');
    if (await viewport.count() > 0) {
      const vpBox = await viewport.boundingBox();
      console.log(`  .terrarium-home__viewport bounding box: ${JSON.stringify(vpBox)}`);
    }
  });

  // ─────────────────────────────────────────────────────────────
  // 6. Direct server reachability from test process
  // ─────────────────────────────────────────────────────────────
  test('6. Server reachability — direct HTTP calls to localhost:5180', async ({ request }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Server Reachability');
    console.log('════════════════════════════════════════');

    // Try the root
    const endpoints = [
      { name: 'Server root', url: `${SERVER_URL}/` },
      { name: 'Health endpoint', url: `${SERVER_URL}/health` },
      { name: 'Alive endpoint', url: `${SERVER_URL}/alive` },
      { name: 'SignalR negotiate', url: `${SERVER_URL}/terrarium/negotiate?negotiateVersion=1` },
      { name: 'Swagger/API docs', url: `${SERVER_URL}/swagger` },
    ];

    for (const ep of endpoints) {
      try {
        const response = await request.get(ep.url, { timeout: 10_000 });
        const body = await response.text();
        console.log(`  ${ep.name} (${ep.url}):`);
        console.log(`    Status: ${response.status()}`);
        console.log(`    Headers: ${JSON.stringify(Object.fromEntries(response.headers().entries ? [...response.headers().entries()] : Object.entries(response.headers())))}`);
        console.log(`    Body preview: ${body.substring(0, 300)}`);
      } catch (err) {
        console.log(`  ${ep.name} (${ep.url}):`);
        console.log(`    ❌ FAILED: ${err.message}`);
      }
    }
  });

  // ─────────────────────────────────────────────────────────────
  // 7. Blazor circuit WebSocket inspection
  // ─────────────────────────────────────────────────────────────
  test('7. Blazor circuit — WebSocket connection analysis', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Blazor Circuit WebSocket');
    console.log('════════════════════════════════════════');

    /** @type {Array<{url: string, type: string}>} */
    const websockets = [];

    page.on('websocket', ws => {
      console.log(`  🔌 WebSocket opened: ${ws.url()}`);
      websockets.push({ url: ws.url(), type: 'opened' });

      ws.on('framereceived', frame => {
        // Only log first few frames to avoid spam
        if (websockets.filter(w => w.type === 'frame-received').length < 20) {
          const payload = frame.payload.toString().substring(0, 200);
          websockets.push({ url: ws.url(), type: 'frame-received' });
          // Log SignalR-relevant frames
          if (payload.includes('error') || payload.includes('Error') || payload.includes('fail')) {
            console.log(`  📩 WS frame (error-related): ${payload}`);
          }
        }
      });

      ws.on('framesent', frame => {
        if (websockets.filter(w => w.type === 'frame-sent').length < 10) {
          websockets.push({ url: ws.url(), type: 'frame-sent' });
        }
      });

      ws.on('close', () => {
        console.log(`  🔌 WebSocket closed: ${ws.url()}`);
        websockets.push({ url: ws.url(), type: 'closed' });
      });
    });

    await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });
    await page.waitForTimeout(10_000);

    console.log(`\n  WebSocket events captured: ${websockets.length}`);
    const opened = websockets.filter(w => w.type === 'opened');
    const closed = websockets.filter(w => w.type === 'closed');
    const received = websockets.filter(w => w.type === 'frame-received');
    const sent = websockets.filter(w => w.type === 'frame-sent');

    console.log(`  Opened: ${opened.length}`);
    opened.forEach(w => console.log(`    → ${w.url}`));
    console.log(`  Closed: ${closed.length}`);
    closed.forEach(w => console.log(`    → ${w.url}`));
    console.log(`  Frames received: ${received.length}`);
    console.log(`  Frames sent: ${sent.length}`);

    // Check if the Blazor circuit is active
    const blazorConnected = await page.evaluate(() => {
      // @ts-ignore
      return typeof Blazor !== 'undefined' ? 'Blazor object exists' : 'Blazor object NOT found';
    });
    console.log(`  Blazor JS: ${blazorConnected}`);
  });

  // ─────────────────────────────────────────────────────────────
  // 8. Full DOM snapshot of key areas
  // ─────────────────────────────────────────────────────────────
  test('8. DOM snapshot — sidebar, statusbar, and error indicators', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: DOM Snapshot');
    console.log('════════════════════════════════════════');

    await page.goto(WEB_URL, { waitUntil: 'networkidle', timeout: 30_000 });
    await page.waitForTimeout(5_000);

    // Sidebar content
    const sidebar = page.locator('.glass-sidebar');
    if (await sidebar.count() > 0) {
      const sidebarText = await sidebar.textContent();
      console.log(`  Sidebar text content:\n    ${sidebarText?.trim().replace(/\n/g, '\n    ')}`);
    } else {
      console.log('  ⚠️ No .glass-sidebar found');
    }

    // Status bar
    const statusbar = page.locator('.glass-statusbar');
    if (await statusbar.count() > 0) {
      const statusbarHTML = await statusbar.innerHTML();
      console.log(`\n  Status bar HTML:\n    ${statusbarHTML.substring(0, 500)}`);
    }

    // Look for any error messages in the DOM
    const errorElements = page.locator('[class*="error"], [class*="Error"], .blazor-error-boundary');
    const errorCount = await errorElements.count();
    console.log(`\n  Error-related DOM elements: ${errorCount}`);
    for (let i = 0; i < errorCount; i++) {
      const el = errorElements.nth(i);
      const classes = await el.getAttribute('class');
      const text = await el.textContent();
      console.log(`    [${i + 1}] class="${classes}" text="${text?.trim().substring(0, 100)}"`);
    }

    // Check for Blazor error UI
    const blazorError = page.locator('#blazor-error-ui');
    if (await blazorError.count() > 0) {
      const display = await blazorError.evaluate(el => window.getComputedStyle(el).display);
      const text = await blazorError.textContent();
      console.log(`\n  #blazor-error-ui display: ${display}`);
      console.log(`  #blazor-error-ui text: "${text?.trim()}"`);
    }

    // Check for message log entries
    const messageLog = page.locator('.message-log');
    if (await messageLog.count() > 0) {
      const logText = await messageLog.textContent();
      console.log(`\n  Message log content:\n    ${logText?.trim().replace(/\n/g, '\n    ')}`);
    }

    // Ecosystem status
    const ecosystemStatus = page.locator('.ecosystem-status');
    if (await ecosystemStatus.count() > 0) {
      const ecoText = await ecosystemStatus.textContent();
      console.log(`\n  Ecosystem status:\n    ${ecoText?.trim()}`);
    }

    // Screenshot
    await page.screenshot({ path: 'tests/diagnostic-screenshot.png', fullPage: true });
    console.log('\n  📸 Full-page screenshot saved to tests/diagnostic-screenshot.png');
  });

  // ─────────────────────────────────────────────────────────────
  // 9. Wait for SignalR reconnect cycle and observe
  // ─────────────────────────────────────────────────────────────
  test('9. Extended observation — SignalR reconnect behavior over 30s', async ({ page }) => {
    console.log('\n════════════════════════════════════════');
    console.log('  DIAGNOSTIC: Extended SignalR Observation (30s)');
    console.log('════════════════════════════════════════');

    /** @type {string[]} */
    const timeline = [];

    page.on('console', msg => {
      if (msg.type() === 'error' || msg.type() === 'warning') {
        const ts = new Date().toISOString().split('T')[1];
        timeline.push(`[${ts}] console.${msg.type()}: ${msg.text().substring(0, 150)}`);
      }
    });

    page.on('requestfailed', request => {
      const ts = new Date().toISOString().split('T')[1];
      timeline.push(`[${ts}] REQUEST FAILED: ${request.method()} ${request.url().substring(0, 100)} → ${request.failure()?.errorText}`);
    });

    page.on('websocket', ws => {
      const ts = new Date().toISOString().split('T')[1];
      timeline.push(`[${ts}] WS OPEN: ${ws.url()}`);
      ws.on('close', () => {
        const ts2 = new Date().toISOString().split('T')[1];
        timeline.push(`[${ts2}] WS CLOSE: ${ws.url()}`);
      });
    });

    await page.goto(WEB_URL, { waitUntil: 'load', timeout: 30_000 });

    // Observe status changes over 30 seconds
    const snapshots = [];
    for (let i = 0; i < 6; i++) {
      await page.waitForTimeout(5_000);
      const statusLabel = await page.locator('.connection-status__label').textContent().catch(() => '(not found)');
      const ts = new Date().toISOString().split('T')[1];
      snapshots.push(`[${ts}] t+${(i + 1) * 5}s: status="${statusLabel}"`);
      console.log(`  ${snapshots[snapshots.length - 1]}`);
    }

    console.log('\n  Timeline of errors/warnings/failures:');
    if (timeline.length === 0) {
      console.log('    (none captured)');
    } else {
      timeline.forEach(t => console.log(`    ${t}`));
    }
  });
});
