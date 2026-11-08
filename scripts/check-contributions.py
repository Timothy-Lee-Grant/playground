#!/usr/bin/env python3
"""Check CONTRIBUTIONS.md against GitHub.

Lists every PR authored by the user outside their own repos, with its live state, and
flags PRs that CONTRIBUTIONS.md doesn't mention or whose row disagrees about the state.
Usage: python3 scripts/check-contributions.py [path/to/CONTRIBUTIONS.md]
Optional: set GITHUB_TOKEN to avoid the unauthenticated rate limit (10 searches/min).
"""
import json, os, re, sys, urllib.request

USER = "Timothy-Lee-Grant"
LEDGER = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "CONTRIBUTIONS.md")

def get(url):
    req = urllib.request.Request(url, headers={"Accept": "application/vnd.github+json", "User-Agent": "contributions-check"})
    if os.environ.get("GITHUB_TOKEN"):
        req.add_header("Authorization", "Bearer " + os.environ["GITHUB_TOKEN"])
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.load(r)

items = get(f"https://api.github.com/search/issues?q=author:{USER}+type:pr+-user:{USER}&per_page=100")["items"]
text = open(LEDGER, encoding="utf-8").read()
# Only rows of the §2 table count as "recorded"
section2 = text.split("## 2.", 1)[1].split("\n## ", 1)[0] if "## 2." in text else text

problems = 0
for it in sorted(items, key=lambda i: i["created_at"]):
    repo = it["repository_url"].split("/repos/")[1]
    num = it["number"]
    merged = (it.get("pull_request") or {}).get("merged_at")
    state = "merged" if merged else it["state"]
    when = (merged or it.get("closed_at") or it["created_at"])[:10]
    row = next((l for l in section2.splitlines() if l.startswith("|") and f"{repo}/pull/{num})" in l), None)
    if row is None:
        flag = "NOT IN LEDGER"
    else:
        says_merged = re.search(r"\|\s*\d{4}-\d{2}-\d{2}\s*\|", row) is not None  # merged date in column 2
        flag = "ok" if (state == "merged") == says_merged else f"LEDGER DISAGREES (row says {'merged' if says_merged else 'not merged'})"
    if flag != "ok":
        problems += 1
    print(f"{repo}#{num:<6} {state:<7} {when}  {flag:<30} {it['title']}")

print(f"\n{len(items)} upstream PRs, {problems} needing attention.")
sys.exit(1 if problems else 0)
