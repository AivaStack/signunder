"""Approves a company's name for CAWG identity assertions and issues its certificate (ADR 0014).

    python3 tools/approve-identity.py SLUG            # show the company, ask, then approve
    python3 tools/approve-identity.py SLUG --yes      # approve without asking
    python3 tools/approve-identity.py SLUG --show     # only show

Reads and writes the staging registry through the Supabase management API, as
tools/supabase.sh does, with SUPABASE_ACCESS_TOKEN from .env. Issues the identity
certificate with identity-ca, from IDENTITY_CA_KEY (the CA's Cloud KMS key version),
deploy/identity-ca/root.pem, and IDENTITY_SIGNING_KEY (the key that signs identity
assertions); gcloud application-default credentials must be current.

A company is approved only when its domain is verified. The name approved is the
company's current name: if it later renames itself, signatures stop naming it until
this is run again.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = "lqxxcrkimjbysfqmedmt"
ROOT_CERT = ROOT / "deploy" / "identity-ca" / "root.pem"


def env(name: str) -> str:
    """This project's setting from .env, falling back to the environment. .env comes
    first: the shell may hold another project's Supabase token (see tools/supabase.sh).
    The last line wins, as with the shell tools."""
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
            "User-Agent": "signunder-approve-identity",
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


def main() -> None:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    flags = {a for a in sys.argv[1:] if a.startswith("--")}
    if len(args) != 1:
        sys.exit(__doc__)
    slug = args[0]

    # Three small queries: the API's firewall refuses some longer ones (403).
    rows = sql(f"select id, legal_name, slug, created_at from registry.tenant where slug = {literal(slug)}")
    if not rows:
        sys.exit(f"No company {slug}.")
    company = rows[0]
    tenant_id = literal(company["id"])
    identity = sql(
        "select domain, domain_verified_at, approved_name, approved_at "
        f"from registry.tenant_identity where tenant_id = {tenant_id}"
    )
    company.update(identity[0] if identity else {"domain": None, "domain_verified_at": None, "approved_name": None, "approved_at": None})
    members = sql(f"select email from registry.tenant_member where tenant_id = {tenant_id}")
    company["members"] = ", ".join(m["email"] for m in members)
    print(f"Company:         {company['legal_name']}  ({company['slug']}, since {company['created_at'][:10]})")
    print(f"Signed up by:    {company['members'] or '(no member)'}")
    print(f"Domain:          {company['domain'] or '(none)'}")
    print(f"Verified by DNS: {company['domain_verified_at'] or 'not yet'}")
    print(f"Approved name:   {company['approved_name'] or '(none)'}  {company['approved_at'] or ''}")
    if "--show" in flags:
        return
    if not company["domain_verified_at"]:
        sys.exit("The domain is not verified yet; nothing to approve.")

    name, domain = company["legal_name"], company["domain"]
    print(f"\nSignatures will say: Published by {name} ({domain}, verified by SignUnder)")
    print("Check the name belongs to whoever controls the domain (their website, a company register).")
    if "--yes" not in flags and input("Approve? [y/N] ").strip().lower() != "y":
        sys.exit("Not approved.")

    signing_key = env("IDENTITY_SIGNING_KEY")
    issued = subprocess.run(
        ["cargo", "run", "-q", "--manifest-path", str(ROOT / "src" / "sign-sidecar" / "Cargo.toml"),
         "--bin", "identity-ca", "--", "issue",
         "--ca-key", env("IDENTITY_CA_KEY"), "--ca-cert", str(ROOT_CERT),
         "--subject-key", signing_key, "--organization", name, "--domain", domain],
        capture_output=True, text=True,
    )
    if issued.returncode != 0:
        sys.exit(f"Issuing the certificate failed: {issued.stderr.strip()}")
    chain = issued.stdout

    sql(
        "update registry.tenant_identity set "
        f"approved_name = {literal(name)}, approved_at = now(), cert_chain_pem = {literal(chain)}, "
        f"key_ref = {literal(signing_key)}, updated_at = now() "
        f"where tenant_id = {literal(company['id'])}"
    )
    print(f"Approved. From the next image they sign, {name}'s signatures name it.")


if __name__ == "__main__":
    main()
