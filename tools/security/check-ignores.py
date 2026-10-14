"""Fails if an entry in .trivyignore.yaml could defer a finding by more than 90 days.

Every entry must have `expired_at`, and it must fall no more than 90 days after today.
An entry added with a 90-day expiry therefore always passes until it expires, and one
that tries to defer for longer never does.
"""

import datetime
import sys

import yaml

MAX_DAYS = 90

entries = (yaml.safe_load(open(sys.argv[1] if len(sys.argv) > 1 else ".trivyignore.yaml")) or {})
problems = []
today = datetime.date.today()
for kind in ("vulnerabilities", "misconfigurations", "secrets", "licenses"):
    for entry in entries.get(kind) or []:
        name = entry.get("id", "?")
        expires = entry.get("expired_at")
        if expires is None:
            problems.append(f"{name}: no expired_at")
            continue
        if isinstance(expires, str):
            expires = datetime.date.fromisoformat(expires[:10])
        if (expires - today).days > MAX_DAYS:
            problems.append(f"{name}: expires {expires}, more than {MAX_DAYS} days away")
        if not entry.get("statement"):
            problems.append(f"{name}: no statement")

for problem in problems:
    print(problem)
print(f"{len(problems)} problem(s) in the ignore file")
sys.exit(1 if problems else 0)
