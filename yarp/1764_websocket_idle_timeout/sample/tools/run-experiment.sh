#!/usr/bin/env bash
# Runs one idle-WebSocket experiment end to end and saves every process's output to sample/evidence/.
#
#   IdleClient ──► wstap :5001 ──► Proxy (YARP) :5000 ──► wstap :5051 ──► EchoServer :5050
#
# Usage (from sample/):
#   tools/run-experiment.sh <NNN-name> [--no-tap] [--no-client] [--server-ka S] [--client-ka S]
#                           [--activity-timeout HH:MM:SS] [--max-idle S]
#
#   --server-ka S         EchoServer KeepAliveInterval (WS_KEEPALIVE_SECONDS). Omitted = 2 min default.
#   --client-ka S         IdleClient KeepAliveInterval (WS_CLIENT_KEEPALIVE_SECONDS). Omitted = 30 s default. 0 = off.
#   --activity-timeout T  Proxy ActivityTimeout override. Omitted = appsettings.json (00:01:40 = 100 s).
#   --max-idle S          Stop the client after S idle seconds if the connection is still open (default 310).
#   --no-tap              Connect client → proxy → echo directly (control run, no relays in the path).
#   --no-client           Start the servers (and taps) and wait for Enter; drive it with another client (browser).
#
# The apps run from their build output (`dotnet <dll>`), not `dotnet run`, so there is exactly one process per
# app to stop and no child process can outlive this script (see lecture 001 §3). Build first: `dotnet build`.
set -euo pipefail

SAMPLE="$(cd "$(dirname "$0")/.." && pwd)"
EVIDENCE="$SAMPLE/evidence"
NAME="$1"; shift
TAP=1; CLIENT=1; SERVER_KA=""; CLIENT_KA=""; ACTIVITY=""; MAX_IDLE=310
while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-tap) TAP=0 ;;
    --no-client) CLIENT=0 ;;
    --server-ka) SERVER_KA="$2"; shift ;;
    --client-ka) CLIENT_KA="$2"; shift ;;
    --activity-timeout) ACTIVITY="$2"; shift ;;
    --max-idle) MAX_IDLE="$2"; shift ;;
    *) echo "unknown option $1"; exit 2 ;;
  esac
  shift
done

ports_in_use() { lsof -nP -iTCP:5000 -iTCP:5001 -iTCP:5050 -iTCP:5051 -sTCP:LISTEN 2>/dev/null || true; }
if [[ -n "$(ports_in_use)" ]]; then echo "Ports busy before start:"; ports_in_use; exit 1; fi

mkdir -p "$EVIDENCE"
PIDS=()
cleanup() {
  for p in "${PIDS[@]:-}"; do [[ -n "$p" ]] && kill "$p" 2>/dev/null || true; done
  sleep 1
  local left; left="$(ports_in_use)"
  echo "# after cleanup, listeners on 5000/5001/5050/5051: ${left:-none}" | tee -a "$EVIDENCE/$NAME-client.txt"
}
trap cleanup EXIT

PROXY_DEST="http://localhost:5050/"; CLIENT_URL="ws://localhost:5000/ws"
if [[ $TAP -eq 1 ]]; then PROXY_DEST="http://localhost:5051/"; CLIENT_URL="ws://localhost:5001/ws"; fi

header() { # $1 = role, $2 = command
  cat <<EOF
# experiment: $NAME ($1)
# date:       $(date -u +%Y-%m-%dT%H:%M:%SZ)
# os:         macOS $(sw_vers -productVersion) ($(uname -m))
# dotnet:     SDK $(cd "$SAMPLE" && dotnet --version), runtime $(dotnet --list-runtimes | grep 'NETCore.App 10' | tail -1 | awk '{print $2}'), Yarp.ReverseProxy 2.3.0
# repo:       $(git -C "$SAMPLE" rev-parse --short HEAD) (+ uncommitted sample changes, if any)
# settings:   proxy ActivityTimeout=${ACTIVITY:-100 s (appsettings 00:01:40)} · server KeepAliveInterval=${SERVER_KA:+${SERVER_KA} s}${SERVER_KA:-2 min (default)} · client KeepAliveInterval=${CLIENT_KA:+${CLIENT_KA} s}${CLIENT_KA:-30 s (ClientWebSocket default)} · taps=$([[ $TAP -eq 1 ]] && echo on || echo off)
# command:    $2
#
EOF
}

# 1. EchoServer (destination)
ECHO_CMD="${SERVER_KA:+WS_KEEPALIVE_SECONDS=$SERVER_KA }dotnet bin/Debug/net10.0/EchoServer.dll   (cwd sample/EchoServer)"
header "EchoServer" "$ECHO_CMD" > "$EVIDENCE/$NAME-echo.txt"
( cd "$SAMPLE/EchoServer" && exec env ${SERVER_KA:+WS_KEEPALIVE_SECONDS=$SERVER_KA} dotnet bin/Debug/net10.0/EchoServer.dll ) >> "$EVIDENCE/$NAME-echo.txt" 2>&1 &
PIDS+=($!)

# 2. Tap between proxy and echo server
if [[ $TAP -eq 1 ]]; then
  header "wstap proxy→echo" "python3 tools/wstap.py --listen 5051 --target 5050 --label proxy→echo" > "$EVIDENCE/$NAME-tap-proxy-echo.txt"
  python3 -u "$SAMPLE/tools/wstap.py" --listen 5051 --target 5050 --label "proxy→echo" >> "$EVIDENCE/$NAME-tap-proxy-echo.txt" 2>&1 &
  PIDS+=($!)
fi

# 3. Proxy (YARP)
PROXY_ENV=("ReverseProxy__Clusters__echoCluster__Destinations__echo-destination__Address=$PROXY_DEST")
[[ -n "$ACTIVITY" ]] && PROXY_ENV+=("ReverseProxy__Clusters__echoCluster__HttpRequest__ActivityTimeout=$ACTIVITY")
header "Proxy" "env ${PROXY_ENV[*]} dotnet bin/Debug/net10.0/Proxy.dll   (cwd sample/Proxy)" > "$EVIDENCE/$NAME-proxy.txt"
( cd "$SAMPLE/Proxy" && exec env "${PROXY_ENV[@]}" dotnet bin/Debug/net10.0/Proxy.dll ) >> "$EVIDENCE/$NAME-proxy.txt" 2>&1 &
PIDS+=($!)

# 4. Tap between client and proxy
if [[ $TAP -eq 1 ]]; then
  header "wstap client→proxy" "python3 tools/wstap.py --listen 5001 --target 5000 --label client→proxy" > "$EVIDENCE/$NAME-tap-client-proxy.txt"
  python3 -u "$SAMPLE/tools/wstap.py" --listen 5001 --target 5000 --label "client→proxy" >> "$EVIDENCE/$NAME-tap-client-proxy.txt" 2>&1 &
  PIDS+=($!)
fi

# Wait until everything is listening
for _ in $(seq 1 40); do
  n=$(ports_in_use | grep -c LISTEN || true)
  [[ $n -ge $([[ $TAP -eq 1 ]] && echo 4 || echo 2) ]] && break
  sleep 0.5
done
echo "listeners:"; ports_in_use

# 5. Client
CLIENT_CMD="${CLIENT_KA:+WS_CLIENT_KEEPALIVE_SECONDS=$CLIENT_KA }IDLE_MAX_SECONDS=$MAX_IDLE dotnet IdleClient/bin/Debug/net10.0/IdleClient.dll $CLIENT_URL"
if [[ $CLIENT -eq 1 ]]; then
  header "IdleClient" "$CLIENT_CMD" > "$EVIDENCE/$NAME-client.txt"
  env ${CLIENT_KA:+WS_CLIENT_KEEPALIVE_SECONDS=$CLIENT_KA} IDLE_MAX_SECONDS="$MAX_IDLE" \
    dotnet "$SAMPLE/IdleClient/bin/Debug/net10.0/IdleClient.dll" "$CLIENT_URL" 2>&1 \
    | while IFS= read -r line; do printf '%s  %s\n' "$(date +%H:%M:%S)" "$line"; done | tee -a "$EVIDENCE/$NAME-client.txt"
  sleep 2   # let the proxy and taps log the teardown
else
  header "external client" "open ${CLIENT_URL} from another client (e.g. the browser page in tools/)" > "$EVIDENCE/$NAME-client.txt"
  echo "Servers are up. Connect your client to $CLIENT_URL, then press Enter here to stop."
  read -r _
fi
