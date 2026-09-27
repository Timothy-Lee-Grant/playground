// Drives headless Chrome over the DevTools protocol: opens the idle-WebSocket page, takes screenshots at chosen
// idle times and right after the connection closes, and saves the page's log text.
//
// Usage: node tools/browser/capture.mjs <out-prefix> <page-url> [shot-seconds...]
//   e.g. node tools/browser/capture.mjs evidence/screenshots/008-browser \
//          "http://127.0.0.1:8088/index.html?auto=1" 5 95
// CAPTURE_MAX_SECONDS (default 400) stops the watch if the connection is still open.
// Needs Node 22+ (built-in WebSocket) and Google Chrome.
import { spawn } from "node:child_process";
import { mkdtempSync, writeFileSync, mkdirSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";

const [outPrefix, pageUrl, ...shotArgs] = process.argv.slice(2);
const shots = shotArgs.map(Number);
const CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const PORT = 9333;
const MAX_SECONDS = Number(process.env.CAPTURE_MAX_SECONDS ?? 400); // stop watching if still open
mkdirSync(dirname(outPrefix), { recursive: true });

const chrome = spawn(CHROME, [
  "--headless=new", "--disable-gpu", `--remote-debugging-port=${PORT}`, "--window-size=1000,720",
  `--user-data-dir=${mkdtempSync(join(tmpdir(), "idle-ws-chrome-"))}`, "about:blank",
], { stdio: "ignore" });
const sleep = ms => new Promise(r => setTimeout(r, ms));

let targets;
for (let i = 0; i < 50 && !targets; i++) {
  try { targets = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json(); } catch { await sleep(200); }
}
const ws = new WebSocket(targets.find(t => t.type === "page").webSocketDebuggerUrl);
await new Promise(r => ws.addEventListener("open", r));
let nextId = 1; const pending = new Map();
ws.addEventListener("message", ev => {
  const m = JSON.parse(ev.data);
  if (m.id && pending.has(m.id)) { pending.get(m.id)(m.result); pending.delete(m.id); }
});
const send = (method, params = {}) => new Promise(r => { const id = nextId++; pending.set(id, r); ws.send(JSON.stringify({ id, method, params })); });
const evaluate = async expr => (await send("Runtime.evaluate", { expression: expr, returnByValue: true })).result.value;
const shot = async name => {
  const { data } = await send("Page.captureScreenshot", { format: "png" });
  writeFileSync(`${outPrefix}-${name}.png`, Buffer.from(data, "base64"));
  console.log(`${new Date().toLocaleTimeString()}  saved ${outPrefix}-${name}.png`);
};

await send("Page.enable");
await send("Page.navigate", { url: pageUrl });
const t0 = Date.now();
console.log(`${new Date().toLocaleTimeString()}  navigated to ${pageUrl}`);

const remaining = [...shots].sort((a, b) => a - b);
while (true) {
  await sleep(250);
  const elapsed = (Date.now() - t0) / 1000;
  if (remaining.length && elapsed >= remaining[0]) await shot(`t${String(remaining.shift()).padStart(3, "0")}s`);
  if ((await evaluate(`document.getElementById("state").textContent`)) === "CLOSED") {
    await sleep(500);
    await shot("closed");
    break;
  }
  if (elapsed > MAX_SECONDS) { await shot(`still-open-t${Math.round(elapsed)}s`); break; }
}
const logText = await evaluate(`document.getElementById("log").textContent`);
writeFileSync(`${outPrefix}-page-log.txt`, logText);
console.log(logText);
ws.close(); chrome.kill(); await sleep(1000);
process.exit(0);
