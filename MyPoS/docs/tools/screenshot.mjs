/**
 * Mengambil tangkapan layar aplikasi lewat Chrome DevTools Protocol.
 * Tanpa dependensi: Node 22 sudah punya WebSocket dan fetch bawaan.
 *
 * Pakai: node shoot.mjs <baseUrl> <outDir>
 */
import { spawn } from 'node:child_process';
import { mkdirSync, writeFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';

const BASE = process.argv[2] ?? 'http://localhost:5296';
const OUT = process.argv[3] ?? './shots';
const PORT = 9333;

const CHROME_CANDIDATES = [
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
];

const chromePath = CHROME_CANDIDATES.find((p) => existsSync(p));
if (!chromePath) throw new Error('Chrome/Edge tidak ditemukan.');

mkdirSync(OUT, { recursive: true });

const userDataDir = join(process.env.TEMP ?? '.', `mypos-shots-${Date.now()}`);
const chrome = spawn(chromePath, [
  `--remote-debugging-port=${PORT}`,
  `--user-data-dir=${userDataDir}`,
  '--headless=new',
  '--no-first-run',
  '--no-default-browser-check',
  '--disable-extensions',
  '--hide-scrollbars',
  '--force-device-scale-factor=2',
  '--window-size=1440,900',
  'about:blank',
], { stdio: 'ignore' });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function browserWsUrl() {
  for (let i = 0; i < 60; i++) {
    try {
      const res = await fetch(`http://127.0.0.1:${PORT}/json/version`);
      const json = await res.json();
      if (json.webSocketDebuggerUrl) return json.webSocketDebuggerUrl;
    } catch { /* belum siap */ }
    await sleep(300);
  }
  throw new Error('DevTools tidak pernah siap.');
}

class Cdp {
  constructor(ws) {
    this.ws = ws;
    this.id = 0;
    this.pending = new Map();
    this.sessionId = null;
    ws.addEventListener('message', (event) => {
      const msg = JSON.parse(event.data);
      if (msg.id && this.pending.has(msg.id)) {
        const { resolve, reject } = this.pending.get(msg.id);
        this.pending.delete(msg.id);
        msg.error ? reject(new Error(JSON.stringify(msg.error))) : resolve(msg.result);
      }
    });
  }

  send(method, params = {}, useSession = true) {
    const id = ++this.id;
    const payload = { id, method, params };
    if (useSession && this.sessionId) payload.sessionId = this.sessionId;
    this.ws.send(JSON.stringify(payload));
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      setTimeout(() => {
        if (this.pending.has(id)) {
          this.pending.delete(id);
          reject(new Error(`Timeout: ${method}`));
        }
      }, 30000);
    });
  }

  async evaluate(expression) {
    const result = await this.send('Runtime.evaluate', {
      expression,
      returnByValue: true,
      awaitPromise: true,
    });
    if (result.exceptionDetails) {
      throw new Error(`JS gagal: ${result.exceptionDetails.text} — ${expression.slice(0, 120)}`);
    }
    return result.result.value;
  }

  /** Menunggu sampai selector muncul; Blazor Server baru merender setelah circuit hidup. */
  async waitFor(selector, timeoutMs = 20000) {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      const found = await this.evaluate(`!!document.querySelector(${JSON.stringify(selector)})`);
      if (found) return true;
      await sleep(200);
    }
    throw new Error(`Tidak muncul: ${selector}`);
  }

  async clickText(selector, text) {
    const clicked = await this.evaluate(`(() => {
      const nodes = [...document.querySelectorAll(${JSON.stringify(selector)})];
      const hit = nodes.find(n => (n.textContent || '').trim().toLowerCase().includes(${JSON.stringify(text.toLowerCase())}));
      if (!hit) return false;
      hit.click();
      return true;
    })()`);
    if (!clicked) throw new Error(`Tidak menemukan "${text}" pada ${selector}`);
    await sleep(700);
  }

  async goto(path) {
    await this.send('Page.navigate', { url: `${BASE}${path}` });
    await sleep(1200);
    await this.waitFor('#app-ready, .mud-layout, .login-shell, .panel, .empty', 25000).catch(() => {});
    await sleep(1400);
  }

  async shot(name) {
    // Tunggu font web selesai dimuat supaya teks tidak tertangkap dalam huruf cadangan.
    await this.evaluate('document.fonts ? document.fonts.ready.then(() => true) : true').catch(() => {});
    await sleep(400);
    const { data } = await this.send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(OUT, `${name}.png`), Buffer.from(data, 'base64'));
    console.log(`  ✓ ${name}.png`);
  }
}

const steps = [];
function step(name, fn) { steps.push({ name, fn }); }

// ---------------------------------------------------------------- skenario

step('01-login', async (cdp) => {
  await cdp.goto('/login');
  await cdp.shot('01-login');
});

step('login-sebagai-admin', async (cdp) => {
  await cdp.clickText('button', 'Admin');
  await sleep(400);
  await cdp.clickText('button', 'Masuk');
  await sleep(3000);
});

step('02-dasbor', async (cdp) => {
  await cdp.goto('/');
  await cdp.shot('02-dasbor');
});

step('03-kasir', async (cdp) => {
  await cdp.goto('/pos');
  await sleep(1200);
  // Isi keranjang supaya panel struk memperlihatkan perhitungan sebenarnya.
  await cdp.evaluate(`(() => {
    const tiles = [...document.querySelectorAll('.tile')];
    [0, 1, 3, 4].forEach(i => tiles[i] && tiles[i].click());
    return tiles.length;
  })()`);
  await sleep(2500);
  await cdp.shot('03-kasir');
});

step('04-produk', async (cdp) => {
  await cdp.goto('/produk');
  await cdp.shot('04-produk');
});

step('14-impor-template', async (cdp) => {
  await cdp.goto('/produk');
  await sleep(1200);
  await cdp.clickText('button', 'Impor Excel');
  await sleep(1600);
  await cdp.shot('14-impor-template');
  await cdp.evaluate(`document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))`);
  await sleep(600);
});

step('05-transaksi', async (cdp) => {
  await cdp.goto('/transaksi');
  await cdp.shot('05-transaksi');
});

step('06-struk', async (cdp) => {
  await cdp.evaluate(`(() => {
    const btn = document.querySelector('.mud-table-body button');
    if (btn) btn.click();
    return !!btn;
  })()`);
  await sleep(2000);
  await cdp.shot('06-struk');
  await cdp.evaluate(`document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))`);
  await sleep(600);
});

step('07-laporan', async (cdp) => {
  await cdp.goto('/laporan-penjualan');
  await cdp.shot('07-laporan');
});

step('08-pengaturan-pajak', async (cdp) => {
  await cdp.goto('/pengaturan');
  await sleep(1000);
  await cdp.clickText('.mud-tab', 'Pajak');
  await sleep(1200);
  await cdp.shot('08-pengaturan-pajak');
});

step('09-pengaturan-pembayaran', async (cdp) => {
  await cdp.clickText('.mud-tab', 'Pembayaran');
  await sleep(1200);
  await cdp.shot('09-pengaturan-pembayaran');
});

step('11-pengaturan-api', async (cdp) => {
  await cdp.clickText('.mud-tab', 'API');
  await sleep(1500);
  await cdp.shot('11-pengaturan-api');
});

step('12-swagger', async (cdp) => {
  await cdp.goto('/swagger');
  await sleep(3000);
  await cdp.shot('12-swagger');
});

step('10-dasbor-gelap', async (cdp) => {
  await cdp.goto('/');
  await sleep(800);
  await cdp.evaluate(`(() => {
    const btn = document.querySelector('button[aria-label="Ganti tema"]');
    if (btn) btn.click();
    return !!btn;
  })()`);
  await sleep(2000);
  await cdp.shot('10-dasbor-gelap');
});

// ---------------------------------------------------------------- jalankan

const ws = new WebSocket(await browserWsUrl());
await new Promise((resolve) => ws.addEventListener('open', resolve, { once: true }));

const cdp = new Cdp(ws);
const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' }, false);
const { sessionId } = await cdp.send('Target.attachToTarget', { targetId, flatten: true }, false);
cdp.sessionId = sessionId;

await cdp.send('Page.enable');
await cdp.send('Runtime.enable');
await cdp.send('Emulation.setDeviceMetricsOverride', {
  width: 1440, height: 900, deviceScaleFactor: 2, mobile: false,
});

let failures = 0;
for (const { name, fn } of steps) {
  try {
    await fn(cdp);
  } catch (err) {
    failures++;
    console.error(`  ✗ ${name}: ${err.message}`);
  }
}

ws.close();
chrome.kill();
console.log(failures === 0 ? 'Selesai tanpa kegagalan.' : `Selesai dengan ${failures} langkah gagal.`);
process.exit(failures === 0 ? 0 : 1);
