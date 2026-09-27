#!/usr/bin/env python3
"""Renders one experiment's raw evidence files as a four-pane console view (HTML), for screenshotting.

The panes show the files verbatim, except that blank lines and repeated "...still open" ticks can be elided with
--compact so the whole run fits on one screen. Usage (from sample/):

    python3 tools/logpanel.py 004-no-client-ka-server-default-aborts "Title" out.html [--compact]
"""
import html
import re
import sys
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent.parent / "evidence"
PANES = [("client", "IdleClient (the caller)"), ("proxy", "Proxy · YARP (the gatekeeper)"),
         ("tap-client-proxy", "wstap · client ↔ YARP"), ("tap-proxy-echo", "wstap · YARP ↔ EchoServer")]
NOISE = re.compile(r"^(Using launch settings|Building\.\.\.|\s+at System\.Runtime\.CompilerServices|\s+at System\.Threading)")


def body(path, compact):
    lines = [l for l in path.read_text().splitlines() if not l.startswith("#")]
    out, ticks = [], []
    for l in lines:
        if compact and "...still open at" in l:
            ticks.append(l)
            continue
        if ticks:
            out.append(ticks[0]); out.append(f"      … {len(ticks) - 2} more ticks …") if len(ticks) > 2 else None
            out.append(ticks[-1]) if len(ticks) > 1 else None
            ticks = []
        if compact and (not l.strip() or NOISE.match(l)):
            continue
        out.append(l)
    if ticks:
        out += [ticks[0], f"      … {len(ticks) - 2} more ticks …", ticks[-1]] if len(ticks) > 2 else ticks
    text = html.escape("\n".join(out))
    for word, cls in [("UpgradeActivityTimeout", "bad"), ("Connection died", "bad"), ("FIN", "bad"),
                      ("Aborted", "bad"), ("PONG", "good"), ("PING", "good"), ("still open", "good"),
                      ("Still open", "good"), ("warn:", "warn")]:
        text = text.replace(word, f'<span class="{cls}">{word}</span>')
    return text


def main():
    name, title, out = sys.argv[1:4]
    compact = "--compact" in sys.argv
    header = next(l for l in (EVIDENCE / f"{name}-client.txt").read_text().splitlines() if l.startswith("# settings"))
    panes = "".join(
        f'<section><h2>{label}<small>evidence/{name}-{suffix}.txt</small></h2><pre>{body(EVIDENCE / f"{name}-{suffix}.txt", compact)}</pre></section>'
        for suffix, label in PANES if (EVIDENCE / f"{name}-{suffix}.txt").exists())
    Path(out).write_text(f"""<!doctype html><meta charset="utf-8"><style>
body {{ margin: 0; padding: 18px; background: #f4f4f1; font: 13px -apple-system, system-ui, sans-serif; width: 1560px; }}
h1 {{ font-size: 17px; margin: 0 0 2px; }} .sub {{ color: #555; margin-bottom: 12px; font: 12px ui-monospace, monospace; }}
.grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }}
section {{ background: #1e1e1e; border-radius: 8px; overflow: hidden; }}
h2 {{ margin: 0; padding: 7px 12px; font-size: 13px; color: #fff; background: #333; display: flex; justify-content: space-between; }}
h2 small {{ color: #aaa; font: 11px ui-monospace, monospace; }}
pre {{ margin: 0; padding: 10px 12px; color: #ddd; font: 11.5px/1.45 ui-monospace, Menlo, monospace; white-space: pre-wrap; word-break: break-all; }}
.bad {{ color: #ff7b72; font-weight: 700; }} .good {{ color: #7ee787; font-weight: 700; }} .warn {{ color: #e3b341; font-weight: 700; }}
</style><h1>{html.escape(title)}</h1><div class="sub">{html.escape(header[2:])}{" · repeated ticks and framework stack frames elided" if compact else ""}</div>
<div class="grid">{panes}</div>""")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
