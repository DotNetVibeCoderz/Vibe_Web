// Captures the screenshots used in README.md and docs/.
// Usage: node shoot.js [baseUrl] [outDir]
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE = process.argv[2] || 'http://localhost:5247';
const OUT = process.argv[3] || path.resolve(__dirname, 'shots');
const PASSWORD = 'Lapak2025!';

const DESKTOP = { width: 1440, height: 900 };
const MOBILE = { width: 414, height: 896 };

fs.mkdirSync(OUT, { recursive: true });

async function settle(page) {
  // Blazor Server swaps in the interactive render after the circuit connects.
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.waitForTimeout(1200);
  // Clicking a control mid-page leaves the viewport scrolled; every shot should
  // start from the top of the page.
  await page.evaluate(() => window.scrollTo(0, 0)).catch(() => {});
  await page.waitForTimeout(250);
}

async function shoot(page, name, opts = {}) {
  const file = path.join(OUT, `${name}.png`);
  await settle(page);
  await page.screenshot({ path: file, fullPage: !!opts.full });
  console.log(`  captured ${name}.png`);
}

async function login(page, email) {
  await page.goto(`${BASE}/account/login`, { waitUntil: 'domcontentloaded' });
  await settle(page);
  await page.fill('#login-email', email);
  await page.fill('#login-password', PASSWORD);
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'domcontentloaded' }).catch(() => {}),
    page.click('button[type=submit]'),
  ]);
  await settle(page);
  const url = page.url();
  if (url.includes('/account/login')) throw new Error(`login failed for ${email}: ${url}`);
  console.log(`  signed in as ${email}`);
}

async function setDark(page, dark) {
  await page.evaluate((d) => localStorage.setItem('lapak-theme', d ? 'dark' : 'light'), dark);
  await page.reload({ waitUntil: 'domcontentloaded' });
  await settle(page);
}

(async () => {
  const browser = await chromium.launch();

  // ---- Anonymous, desktop ----
  console.log('anonymous pages');
  let ctx = await browser.newContext({ viewport: DESKTOP, deviceScaleFactor: 2, locale: 'id-ID' });
  let page = await ctx.newPage();

  await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '01-beranda');

  await setDark(page, true);
  await shoot(page, '02-beranda-gelap');
  await setDark(page, false);

  await page.goto(`${BASE}/products`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '03-produk');

  // First product card leads to a detail page
  await page.click('.product-card');
  await shoot(page, '04-detail-produk');

  await page.goto(`${BASE}/stores`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '05-toko');

  await page.goto(`${BASE}/promos`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '06-promo');

  await page.goto(`${BASE}/chat/tony-kurus`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '07-tony-kurus');

  await page.goto(`${BASE}/chat/siti-bohay`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '08-siti-bohay');

  await page.goto(`${BASE}/account/login`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '09-masuk');

  await ctx.close();

  // ---- Mobile ----
  console.log('mobile pages');
  ctx = await browser.newContext({ viewport: MOBILE, deviceScaleFactor: 2, isMobile: true, hasTouch: true, locale: 'id-ID' });
  page = await ctx.newPage();
  await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '10-mobile-beranda');
  await page.goto(`${BASE}/products`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '11-mobile-produk');
  await ctx.close();

  // ---- Buyer: cart + checkout ----
  console.log('buyer pages');
  ctx = await browser.newContext({ viewport: DESKTOP, deviceScaleFactor: 2, locale: 'id-ID' });
  page = await ctx.newPage();
  await login(page, 'zahra.aulia@lapak.com');

  // Put two different products in the cart so the pages have real content.
  for (const idx of [0, 3]) {
    await page.goto(`${BASE}/products`, { waitUntil: 'domcontentloaded' });
    await settle(page);
    await page.locator('.product-card').nth(idx).click();
    await settle(page);
    const addBtn = page.locator('button.btn-primary', { hasText: 'Masukkan keranjang' });
    if (await addBtn.count()) {
      await addBtn.first().click();
      await settle(page);
    }
  }

  await page.goto(`${BASE}/cart`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '12-keranjang');

  await page.goto(`${BASE}/checkout`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '13-checkout-alamat');

  // Step through to the payment step so the gateway picker is visible.
  const next = page.locator('button', { hasText: 'Lanjut ke pengiriman' });
  if (await next.count()) {
    await next.first().click();
    await settle(page);
    await page.waitForTimeout(1500);
    await shoot(page, '14-checkout-pengiriman');

    const firstOption = page.locator('.option-row').first();
    if (await firstOption.count()) {
      await firstOption.click();
      await settle(page);
      const toPay = page.locator('button', { hasText: 'Lanjut ke pembayaran' });
      if (await toPay.count()) {
        await toPay.first().click();
        await settle(page);
        await shoot(page, '15-checkout-pembayaran');
      }
    }
  }

  await page.goto(`${BASE}/account/orders`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '16-pesanan');

  await ctx.close();

  // ---- Seller ----
  console.log('seller pages');
  ctx = await browser.newContext({ viewport: DESKTOP, deviceScaleFactor: 2, locale: 'id-ID' });
  page = await ctx.newPage();
  await login(page, 'budi.santoso@lapak.com');

  await page.goto(`${BASE}/seller/manage-store`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '17-kelola-toko');

  await page.goto(`${BASE}/seller/manage-products`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '18-kelola-produk');

  await page.goto(`${BASE}/seller/manage-products/new`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '19-tambah-produk');

  await ctx.close();

  // ---- Admin ----
  console.log('admin pages');
  ctx = await browser.newContext({ viewport: DESKTOP, deviceScaleFactor: 2, locale: 'id-ID' });
  page = await ctx.newPage();
  await login(page, 'admin.lapak@lapak.com');

  await page.goto(`${BASE}/dashboard`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '20-dashboard');

  await setDark(page, true);
  await shoot(page, '21-dashboard-gelap');
  await setDark(page, false);

  await page.goto(`${BASE}/admin`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '22-admin');

  await page.goto(`${BASE}/admin/vouchers`, { waitUntil: 'domcontentloaded' });
  await shoot(page, '23-admin-voucher');

  await ctx.close();
  await browser.close();
  console.log('done');
})().catch((err) => {
  console.error('FAILED:', err.message);
  process.exit(1);
});
