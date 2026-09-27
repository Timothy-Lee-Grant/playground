#!/usr/bin/env python3
"""wstap: a transparent TCP relay that logs every WebSocket frame passing through it.

Put it in front of any hop to see what is actually on the wire:

    IdleClient ──► wstap :5001 ──► Proxy :5000 ──► wstap :5051 ──► EchoServer :5050

It forwards bytes unchanged. For logging only, it skips the HTTP upgrade headers in each direction
and then decodes WebSocket frame headers (RFC 6455 §5.2) to name each frame: TEXT, BINARY, PING,
PONG, CLOSE. It also logs when each side closes (FIN) or resets (RST) its TCP connection.

Usage: python3 wstap.py --listen 5001 --target 5000 --label client-proxy
"""
import argparse
import asyncio
import sys
import time

OPCODES = {0x0: "CONT", 0x1: "TEXT", 0x2: "BINARY", 0x8: "CLOSE", 0x9: "PING", 0xA: "PONG"}
T0 = time.monotonic()


def log(label, msg):
    wall = time.strftime("%H:%M:%S", time.localtime())
    print(f"{wall}  +{time.monotonic() - T0:8.3f}s  [{label}] {msg}", flush=True)


class FrameDecoder:
    """Incrementally decodes one direction of a WebSocket byte stream (logging only)."""

    def __init__(self):
        self.buf = bytearray()
        self.in_http = True  # before the end of the upgrade request/response headers

    def feed(self, data):
        self.buf += data
        out = []
        if self.in_http:
            end = self.buf.find(b"\r\n\r\n")
            if end < 0:
                return out
            head = bytes(self.buf[:end]).decode("latin-1").split("\r\n")[0]
            out.append(f"HTTP  {head}")
            del self.buf[: end + 4]
            self.in_http = False
        while len(self.buf) >= 2:
            b0, b1 = self.buf[0], self.buf[1]
            masked, n, pos = bool(b1 & 0x80), b1 & 0x7F, 2
            if n == 126:
                if len(self.buf) < 4:
                    break
                n, pos = int.from_bytes(self.buf[2:4], "big"), 4
            elif n == 127:
                if len(self.buf) < 10:
                    break
                n, pos = int.from_bytes(self.buf[2:10], "big"), 10
            pos += 4 if masked else 0
            if len(self.buf) < pos + n:
                break
            payload = bytes(self.buf[pos : pos + n])
            if masked:
                key = self.buf[pos - 4 : pos]
                payload = bytes(c ^ key[i % 4] for i, c in enumerate(payload))
            name = OPCODES.get(b0 & 0x0F, f"op{b0 & 0x0F:#x}")
            detail = f"{n} B payload"
            if name == "TEXT":
                detail += f" {payload[:40].decode('utf-8', 'replace')!r}"
            elif name == "CLOSE" and n >= 2:
                detail += f" code={int.from_bytes(payload[:2], 'big')}"
            out.append(f"WS    {name:<6} {'masked' if masked else 'unmasked'}, {detail}")
            del self.buf[: pos + n]
        return out


async def pump(reader, writer, label, arrow):
    dec = FrameDecoder()
    try:
        while data := await reader.read(65536):
            for line in dec.feed(data):
                log(label, f"{arrow}  {line}")
            writer.write(data)
            await writer.drain()
        log(label, f"{arrow}  TCP   FIN (sender closed its side)")
    except (ConnectionResetError, BrokenPipeError) as e:
        log(label, f"{arrow}  TCP   RESET/broken ({type(e).__name__})")
    finally:
        try:
            writer.close()
        except Exception:
            pass


async def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--listen", type=int, required=True)
    ap.add_argument("--target", type=int, required=True)
    ap.add_argument("--label", required=True)
    a = ap.parse_args()

    async def handle(cr, cw):
        log(a.label, f"TCP accepted from {cw.get_extra_info('peername')}; connecting to :{a.target}")
        ur, uw = await asyncio.open_connection("127.0.0.1", a.target)
        await asyncio.gather(pump(cr, uw, a.label, "──►"), pump(ur, cw, a.label, "◄──"))
        log(a.label, "TCP both directions finished")

    server = await asyncio.start_server(handle, "127.0.0.1", a.listen)
    log(a.label, f"listening on :{a.listen}, forwarding to :{a.target}  (──► = toward :{a.target})")
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        sys.exit(0)
