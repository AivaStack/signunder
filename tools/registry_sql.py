"""The staging registry through the Supabase management API, for the hand-run tools
(approve-identity.py, set-plan.py). Settings come from .env first, then the environment:
the shell may hold another project's Supabase token (see tools/supabase.sh).
"""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = "lqxxcrkimjbysfqmedmt"


def env(name: str) -> str:
    """This project's setting from .env, falling back to the environment. The last line wins,
    as with the shell tools."""
    values = [
        line.split("=", 1)[1].strip()
        for line in (ROOT / ".env").read_text().splitlines()
        if line.startswith(f"{name}=")
    ]
    value = values[-1] if values and values[-1] else os.environ.get(name)
    if not value:
        sys.exit(f"{name} is not set (.env or environment)")
    return value


def sql(query: str) -> list[dict]:
    request = urllib.request.Request(
        f"https://api.supabase.com/v1/projects/{PROJECT}/database/query",
        data=json.dumps({"query": query}).encode(),
        headers={
            "Authorization": f"Bearer {env('SUPABASE_ACCESS_TOKEN')}",
            "Content-Type": "application/json",
            # The API refuses Python's default client name.
            "User-Agent": "signunder-tools",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.loads(response.read())
    except urllib.error.HTTPError as error:
        sys.exit(f"Supabase answered {error.code}: {error.read().decode(errors='replace')[:300]}")


def literal(value: str) -> str:
    """A SQL string literal: the value with single quotes doubled."""
    return "'" + value.replace("'", "''") + "'"
