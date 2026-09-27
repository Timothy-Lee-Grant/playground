#!/usr/bin/env python3
"""Draws an SVG timeline of the idle period from wstap logs, one row pair per experiment.

Time zero for each hop is the echoed 'hello' (the last application message); everything after it is keep-alive
traffic or teardown. Usage (from sample/):

    python3 tools/timeline_svg.py out.svg 003-defaults-dotnet-client-survives "E3 · caption" [name "caption" ...]
"""
import re
import sys
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent.parent / "evidence"
LINE = re.compile(r"\+\s*([\d.]+)s\s+\[[^\]]+\]\s+(──►|◄──)\s+(WS|TCP)\s+(\S+)")
X0, W, SPAN, TIMEOUT = 250, 640, 320, 100  # plot x origin, plot width px, seconds shown, ActivityTimeout


def events(tap_file):
    idle_start, out = None, []
    for line in tap_file.read_text().splitlines():
        m = LINE.search(line)
        if not m:
            continue
        t, arrow, kind, what = float(m[1]), m[2], m[3], m[4]
        if kind == "WS" and what == "TEXT" and arrow == "◄──":
            idle_start = t
            continue
        if idle_start is not None:
            out.append((t - idle_start, arrow, "FIN" if kind == "TCP" else what))
    return out


def x(t):
    return X0 + min(t, SPAN) / SPAN * W


def main():
    out, pairs = sys.argv[1], list(zip(sys.argv[2::2], sys.argv[3::2]))
    row_h, top = 64, 56
    height = top + row_h * len(pairs) + 56
    s = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{X0 + W + 40}" height="{height}" '
         f'font-family="-apple-system,Segoe UI,Helvetica,Arial,sans-serif" font-size="12">',
         f'<rect width="100%" height="100%" fill="#ffffff"/>',
         f'<text x="16" y="24" font-size="15" font-weight="600" fill="#1d1d1b">'
         f'What crossed the wire after the last application message (captured by wstap)</text>']
    # grid + timeout line
    grid_bottom = top + row_h * len(pairs)
    for sec in range(0, SPAN + 1, 30):
        s.append(f'<line x1="{x(sec)}" y1="{top - 6}" x2="{x(sec)}" y2="{grid_bottom}" stroke="#e6e6e6"/>')
        s.append(f'<text x="{x(sec)}" y="{grid_bottom + 16}" text-anchor="middle" fill="#666">{sec}s</text>')
    s.append(f'<line x1="{x(TIMEOUT)}" y1="{top - 14}" x2="{x(TIMEOUT)}" y2="{grid_bottom}" stroke="#c62828" '
             f'stroke-dasharray="5,4" stroke-width="1.5"/>')
    s.append(f'<text x="{x(TIMEOUT) + 4}" y="{top - 16}" fill="#c62828">YARP ActivityTimeout = 100 s</text>')
    s.append(f'<text x="{X0 + W / 2}" y="{grid_bottom + 36}" text-anchor="middle" fill="#444">'
             f'seconds after the echoed "hello" (idle time)</text>')

    for i, (name, caption) in enumerate(pairs):
        y = top + i * row_h
        s.append(f'<rect x="8" y="{y}" width="{X0 + W + 24}" height="{row_h - 4}" rx="6" '
                 f'fill="{"#fafaf8" if i % 2 else "#f3f3ef"}"/>')
        title, _, sub = caption.partition("|")
        s.append(f'<text x="16" y="{y + 22}" font-weight="600" fill="#1d1d1b">{title.strip()}</text>')
        s.append(f'<text x="16" y="{y + 40}" fill="#555">{sub.strip()}</text>')
        for j, (hop, label) in enumerate([("client-proxy", "client ↔ YARP"), ("proxy-echo", "YARP ↔ server")]):
            ly = y + 18 + j * 24
            s.append(f'<text x="{X0 - 8}" y="{ly + 4}" text-anchor="end" fill="#888" font-size="11">{label}</text>')
            s.append(f'<line x1="{X0}" y1="{ly}" x2="{X0 + W}" y2="{ly}" stroke="#bbb"/>')
            f = EVIDENCE / f"{name}-tap-{hop}.txt"
            if not f.exists():
                continue
            fins = [e for e in events(f) if e[2] == "FIN"]
            for t, arrow, what in events(f):
                if what == "FIN":
                    continue
                # ──► is toward the server side of this hop; draw it above the line, ◄── below
                color = {"PING": "#1f6feb", "PONG": "#1a7f37", "CLOSE": "#8250df"}.get(what, "#444")
                dy = -5 if arrow == "──►" else 5
                s.append(f'<circle cx="{x(t):.1f}" cy="{ly + dy}" r="4" fill="{color}"><title>{what} {arrow} '
                         f'at {t:.1f}s</title></circle>')
            if fins:
                t = fins[0][0]
                s.append(f'<text x="{x(t):.1f}" y="{ly + 5}" text-anchor="middle" font-size="15" font-weight="700" '
                         f'fill="#c62828">✕<title>TCP closed at {t:.1f}s</title></text>')
                s.append(f'<text x="{x(t) + 9:.1f}" y="{ly + 4}" font-size="11" fill="#c62828">closed {t:.1f}s</text>')
            else:
                s.append(f'<text x="{X0 + W + 4}" y="{ly + 4}" font-size="13" fill="#1a7f37">→</text>')
    # legend
    lx, ly = X0 + W - 330, 40
    for k, (label, color) in enumerate([("Pong", "#1a7f37"), ("Ping", "#1f6feb"), ("TCP closed", "#c62828")]):
        s.append(f'<circle cx="{lx + k * 110}" cy="{ly - 4}" r="4" fill="{color}"/>'
                 f'<text x="{lx + k * 110 + 8}" y="{ly}" fill="#444">{label}</text>')
    s.append(f'<text x="16" y="{height - 6}" fill="#888" font-size="11">Dots above a line travel toward the server; '
             f'below travel toward the client. → = still open when the run ended.</text>')
    s.append("</svg>")
    Path(out).write_text("\n".join(s))
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
