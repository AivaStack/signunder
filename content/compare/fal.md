---
title: "SignUnder and fal.ai's Content Credentials, compared"
description: "fal signs its hosted applications' output as fal, with an invisible watermark; API scope is undocumented. A supplier's mark, not yours. Read 12 Oct 2026."
slug: fal
type: compare
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [compare, fal, platforms, c2pa]
sources:
  - title: "fal.ai Verify page"
    url: https://fal.ai/verify
    read: 2026-10-12
  - title: "fal.ai Terms of Service (8 September 2026)"
    url: https://fal.ai/terms
    read: 2026-10-12
  - title: "European Commission, Guidelines on Article 50 (20 July 2026), the provider example"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
faq:
  - q: "Does fal.ai sign the images my app generates through its API?"
    a: "Its Verify page says every piece of media generated through fal's hosted applications is signed with Content Credentials and watermarked, and checks whether fal was the signer. Whether raw API calls are signed is not documented, as of 12 October 2026."
  - q: "If fal signs my output, am I covered under Article 50?"
    a: "The guidelines make you the provider if you offer generation under your own name. A manifest naming fal records that fal generated the content; we know of no guidance that a supplier's mark discharges the provider's duty, and no verifier that shows your name from it."
  - q: "Can I use fal and SignUnder together?"
    a: "Yes. SignUnder's proxy mode sits in front of fal's base URL, keeps any fal manifest as an ingredient, and adds your own signature, watermark and registry record on the way back."
related:
  - /platforms/fal
  - /guides/sign-under-your-own-name
  - /compare
draft: false
---

fal.ai is the one inference platform of the eleven we track that attaches Content Credentials to generated media, and it does so in its own name: its verifier checks "whether fal was the signer". For a company reselling fal generation under its own brand, that is a supplier's mark on the company's output. SignUnder keeps that mark as an ingredient and adds the company's own signature, watermark and record on top; the two are complementary, and the comparison is about whose name a verifier shows.

- **fal:** C2PA manifest signed as fal plus an invisible watermark, for media "generated through fal's hosted applications"; a public verifier; terms that put output at the customer's sole risk.
- **Not documented:** whether raw API output is signed; no EU region stated.
- **SignUnder:** your name on the signature via your own listing, upstream manifests preserved, watermark and fingerprint, per-customer verify and detection, rule packs.

## What fal offers

From its Verify page, read 12 October 2026: "Every piece of media generated through fal's hosted applications is signed with Content Credentials (C2PA)", output is "embedded with an invisible watermark", and the public tool checks "whether fal was the signer" and when the content was generated. It also says that content from other providers cannot be verified, that live generation is not signed at generation time, and that a "Not verified" result does not prove a file is human-made. The page does not say whether API calls or the playground are covered. fal's terms of 8 September 2026 say "USE OF SUCH OUTPUT CONTENT IS AT CUSTOMER'S SOLE RISK" and require lawful use; they do not mention marking.

## Where the difference is

**Whose name.** A verifier reading a fal-signed file shows fal. The Commission's guidelines treat the company offering generation under its own name as the provider responsible under Article 50(2); a supplier's mark is evidence of the supplier's generation, not the provider's marking. SignUnder signs under the provider's own listing and certificate ([the own-name guide](/guides/sign-under-your-own-name)).

**Scope.** fal's documented coverage is its hosted applications. A product calling fal's API, routing between fal and Replicate, and re-encoding or compositing the results has no documented mark on the API path and breaks whatever mark exists on the edits. SignUnder marks after the customer's own pipeline, whichever provider served the request, and re-signs edits as actions.

**Detection and rules.** fal's verifier answers "did fal sign this". The Code's duty is a detection solution for the provider's own system, interoperable by 2 February 2027; SignUnder runs that per customer. fal maintains no rule packs.

## Where fal is stronger

It marks at the source, today, for free, with a watermark, and it was the only one of eleven platforms doing so when we checked. If fal signs your API output as well as its hosted apps, every fal customer starts with a supplier manifest that SignUnder preserves as an ingredient; we have asked fal to document the API scope and will update this page when it is.

Read from fal's own pages on the dates shown. Tell us if this is out of date and we will correct it with the date of the correction.

## Changes

- 2026-10-12 — First published.

*Not legal advice. Every cell comes from the vendor's own pages on the date shown.*
