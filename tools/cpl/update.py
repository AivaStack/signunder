#!/usr/bin/env python3
"""Regenerate content/guides/conformance-list.md from the public C2PA
Conforming Products List. Standard library only; run weekly (CHECKLIST.md B36):

    python3 tools/cpl/update.py

Reads the list JSON from GitHub, recomputes the statistics, lists the latest
listings, and rewrites the page with today's date in `updated` and `reviewed`.
"""
from __future__ import annotations

import collections
import datetime as dt
import json
import statistics
import urllib.request
from pathlib import Path

URL = "https://raw.githubusercontent.com/c2pa-org/conformance-public/main/conforming-products/conforming-products-list.json"
OUT = Path(__file__).resolve().parents[2] / "content/guides/conformance-list.md"


def fetch() -> list[dict]:
    with urllib.request.urlopen(urllib.request.Request(URL, headers={"User-Agent": "signunder-cpl"}), timeout=60) as r:
        return json.load(r)


def main() -> None:
    items = fetch()
    today = dt.date.today()
    d = lambda s: dt.date.fromisoformat(str(s)[:10])
    rows = [(i, d(i["dates"]["creation"]), d(i["dates"]["conformance"])) for i in items if i["dates"].get("creation") and i["dates"].get("conformance")]
    days = sorted((c - a).days for _, a, c in rows)
    gen = [i for i in items if i["product"].get("productType") == "generatorProduct"]
    val = [i for i in items if i["product"].get("productType") == "validatorProduct"]
    lvl = collections.Counter(i["product"].get("assurance", {}).get("maxAssuranceLevel") for i in gen)
    applicants = {i["applicant"] for i in items}
    last30 = [r for r in rows if (today - r[2]).days <= 30]
    latest = sorted(rows, key=lambda r: r[2], reverse=True)[:20]
    by_month = collections.Counter(c.strftime("%Y-%m") for _, _, c in rows)
    months = sorted(by_month)[-8:]
    iso = today.isoformat()
    fmt = lambda x: x.strftime("%-d %B %Y")

    table = "\n".join(
        f"| {fmt(c)} | {i['applicant']} | {i['product']['DN'].get('CN','')} | {'generator' if i['product'].get('productType')=='generatorProduct' else 'validator'} | {i['product'].get('assurance',{}).get('maxAssuranceLevel') or '–'} |"
        for i, _, c in latest
    )
    months_table = "\n".join(f"| {m} | {by_month[m]} |" for m in months)

    OUT.write_text(f"""---
title: "The C2PA conforming products list, this week"
description: "{len(items)} records from {len(applicants)} applicants, median {statistics.median(days):.0f} days from intake to listing, {len(last30)} listed in the last 30 days. Recomputed from the public list on {fmt(today)}."
slug: conformance-list
type: guide
date: 2026-10-12
updated: {iso}
reviewed: {iso}
authors: [eoin]
tags: [c2pa, conformance, data]
sources:
  - title: "C2PA Conforming Products List (JSON), as synced on {fmt(today)}"
    url: https://github.com/c2pa-org/conformance-public/blob/main/conforming-products/conforming-products-list.json
    read: {iso}
  - title: "C2PA Conformance Explorer"
    url: https://spec.c2pa.org/conformance-explorer/
    read: {iso}
faq:
  - q: "How many products are on the C2PA conforming products list?"
    a: "{len(items)} records from {len(applicants)} applicants on {fmt(today)}: {len(gen)} generator products and {len(val)} validator products."
  - q: "How long does conformance take?"
    a: "Across all records, the gap between a record's creation and its conformance date has a median of {statistics.median(days):.0f} days, a mean of {statistics.mean(days):.0f} and a maximum of {max(days)}, as of {fmt(today)}."
  - q: "How often is this page updated?"
    a: "Weekly, by recomputing every figure from the public JSON; the dates at the top say when. The list itself is synced by the C2PA several times a week."
related:
  - /guides/c2pa-conformance-program
  - /guides/sign-under-your-own-name
draft: false
---

The C2PA Conforming Products List holds {len(items)} records from {len(applicants)} applicants as of {fmt(today)}: {len(gen)} generator products and {len(val)} validator products. The median time from a record's creation to its conformance date is {statistics.median(days):.0f} days. {len(last30)} products were listed in the last 30 days. Every figure on this page is recomputed from the public JSON each week; nothing is estimated.

- Records: **{len(items)}** ({len(gen)} generator, {len(val)} validator) from **{len(applicants)} applicants**.
- Assurance: **{lvl.get(1,0)}** generator products at maximum level 1, **{lvl.get(2,0)}** at level 2.
- Intake to listing: **median {statistics.median(days):.0f} days**, mean {statistics.mean(days):.0f}, maximum {max(days)}.
- Pace: **{len(last30)} listed in the last 30 days**; latest conformance date {fmt(max(c for _,_,c in rows))}.

## Listings by month

| Month | Products deemed conformant |
|---|---|
{months_table}

## The twenty most recent listings

| Conformance date | Applicant | Product | Type | Max level |
|---|---|---|---|---|
{table}

## How to read this

A record's creation date is when the Program opened it, after the expression of interest and legal agreement; the conformance date is when the Approver signed it off; the public disclosure date is when the applicant chose to let it appear. The [conformance guide](/guides/c2pa-conformance-program) walks through the steps between. To cite this page: SignUnder, "The C2PA conforming products list, this week", signunder.com/guides/conformance-list, as of the date at the top.

## Changes

- {iso} — Recomputed from the list.
- 2026-10-12 — First published.

*Figures are computed from the C2PA's public list; the list is the authority. Not legal advice.*
""")
    print(f"wrote {OUT.relative_to(Path.cwd())}: {len(items)} records, median {statistics.median(days):.0f} days, {len(last30)} in 30 days")


if __name__ == "__main__":
    main()
