// Renders local HTML/SVG files to PNG with headless Chrome, sized to the content.
// Usage: node tools/render.mjs <in.html|in.svg> <out.png> [<in> <out> ...]
import { spawn } from "node:child_process";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

const CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const PORT = 9334;
const sleep = ms => new Promise(r => setTimeout(r, ms));
const profile = mkdtempSync(join(tmpdir(), "render-chrome-"));
const chrome = spawn(CHROME, ["--headless=new", "--disable-gpu", "--hide-scrollbars", `--remote-debugging-port=${PORT}`,
  `--user-data-dir=${profile}`, "about:blank"], { stdio: "ignore" });

let targets;
for (let i = 0; i < 50 && !targets; i++) {
  try { targets = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json(); } catch { await sleep(200); }
}
const ws = new WebSocket(targets.find(t => t.type === "page").webSocketDebuggerUrl);
await new Promise(r => ws.addEventListener("open", r));
let nextId = 1; const pending = new Map();
ws.addEventListener("message", ev => { const m = JSON.parse(ev.data); if (pending.has(m.id)) { pending.get(m.id)(m.result); pending.delete(m.id); } });
const send = (method, params = {}) => new Promise(r => { const id = nextId++; pending.set(id, r); ws.send(JSON.stringify({ id, method, params })); });

const args = process.argv.slice(2);
for (let i = 0; i < args.length; i += 2) {
  await send("Emulation.setDeviceMetricsOverride", { width: 1600, height: 100, deviceScaleFactor: 2, mobile: false });
  await send("Page.navigate", { url: `file://${resolve(args[i])}` });
  await sleep(800);
  const { result } = await send("Runtime.evaluate", { returnByValue: true, expression:
    `(() => { const e = document.documentElement, b = document.body || e;
      const svg = document.querySelector("svg");
      if (svg && !document.body) return [svg.width.baseVal.value, svg.height.baseVal.value];
      return [Math.ceil(Math.max(b.scrollWidth, e.scrollWidth)), Math.ceil(Math.max(b.scrollHeight, e.scrollHeight))]; })()` });
  const [w, h] = result.value;
  await send("Emulation.setDeviceMetricsOverride", { width: w, height: h, deviceScaleFactor: 2, mobile: false });
  await sleep(300);
  const { data } = await send("Page.captureScreenshot", { format: "png" });
  writeFileSync(args[i + 1], Buffer.from(data, "base64"));
  console.log(`rendered ${args[i + 1]} (${w}x${h} CSS px)`);
}
ws.close(); chrome.kill(); await sleep(500);
process.exit(0);
