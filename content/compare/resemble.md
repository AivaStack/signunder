---
title: "SignUnder and Resemble AI's Watermarker, compared"
description: "Resemble signs C2PA records and detects SynthID, but is not on the conforming products list, so verifiers show Unrecognized. Read 12 Oct 2026."
slug: resemble
type: compare
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [compare, resemble, watermark, c2pa]
sources:
  - title: "Resemble AI pricing page"
    url: https://www.resemble.ai/pricing/
    read: 2026-10-12
  - title: "Resemble AI changelog: C2PA signing and SynthID detection (15 June 2026)"
    url: https://www.resemble.ai/changelogs/c2pa-signing-and-synthid-detection-2026
    read: 2026-10-12
  - title: "C2PA Conforming Products List (JSON), checked for Resemble"
    url: https://github.com/c2pa-org/conformance-public/blob/main/conforming-products/conforming-products-list.json
    read: 2026-10-12
  - title: "Resemble AI Watermarker page (Article 50 positioning)"
    url: https://www.resemble.ai/
    read: 2026-10-08
faq:
  - q: "Is Resemble AI on the C2PA conforming products list?"
    a: "No. The list of 248 records from 146 applicants, as synced on 12 October 2026, has no Resemble entry. Its C2PA signatures therefore chain to no Trust List certificate and display as Unrecognized in verifiers."
  - q: "What does Resemble charge?"
    a: "On 12 October 2026: Flex at $0 a month pay-as-you-go, Team at $350 a month, Business at $1,000 a month; Watermarker encodes at $0.0005 a call and decodes at $0.0002; image detection from $0.035 an image."
  - q: "Could Resemble fix the listing gap?"
    a: "Yes, and quickly: the application is free and the median time to listing is 17 days. If it does, its signatures become trusted, in Resemble's name. What would remain different is signing under your own name, the conformance onboarding, the per-customer detection endpoint and the rule packs."
related:
  - /guides/why-content-credentials-say-unrecognized
  - /guides/sign-under-your-own-name
  - /compare
draft: false
---

Resemble AI's Watermarker is a good watermarking primitive with a C2PA signing feature added on 15 June 2026, and it is the one self-serve vendor that has marketed Article 50 by name. It is not on the C2PA Conforming Products List as of 12 October 2026, so the manifests it signs do not chain to the Trust List and verifiers show them as Unrecognized; and it signs in its own name, not yours. Those two facts are the comparison.

- **Resemble:** watermark (its PerTh model), SynthID detection, C2PA signing since June 2026, a detection API, from $0 a month plus per-call fees.
- **Not listed:** no conformance record, so no Trust List certificate behind its signatures.
- **SignUnder:** your own listing and certificate, signing under your name, watermark plus fingerprint, a public verify and detection endpoint per customer, and the rule packs.

## What Resemble offers

From its pricing page, read 12 October 2026: Flex at $0 a month with "no subscription fee" and pay-as-you-go credits; Team at $350 a month ($280 billed annually); Business at $1,000 a month; Enterprise on request. Watermarker encodes cost $0.0005 a call and decodes $0.0002 on every tier; image detection is $0.035 an image on Flex and $0.015 on Team and Business. The page describes the Watermarker as "Provenance and C2PA compliance" and the Flex tier as including "Check for SynthID and sign C2PA records". Its changelog of 15 June 2026 says "The Watermarker can now sign outputs with C2PA content credentials" and "It can also detect Google SynthID watermarks".

The pricing page does not mention Article 50 or the EU AI Act; the Watermarker's own page did, by name, when we read it on 8 October 2026. The changelog says nothing about which certificate signs the manifests or whether they validate against the Trust List.

## Where the difference is

**Conformance.** We checked the Conforming Products List on 12 October 2026: 248 records, 146 applicants, no Resemble. Under the Certificate Policy a Trust List CA issues claim signing certificates only to listed products, so whatever Resemble signs with today, a verifier that checks the Trust List reports it as Unrecognized ([why that happens](/guides/why-content-credentials-say-unrecognized)). This is fixable on Resemble's side, for free, in a median of 17 days; we watch the list weekly and will update this page the day it changes.

**Whose name.** Even once listed, Resemble's certificate would name Resemble. Your name on the signature needs your own listing, which no hosted signer does for you ([the own-name guide](/guides/sign-under-your-own-name)). SignUnder's product is that listing, your certificate, your key in a KMS, and signing under them.

**Detection.** Resemble's detection is an authenticated API answering "is this ours or SynthID". The Code of Practice's duty is a detection solution for your own system's outputs, public, with an interoperable mechanism by 2 February 2027; SignUnder runs that per customer, under the customer's subdomain.

**Rules.** Resemble does not maintain jurisdiction or platform rule packs. SignUnder evaluates each output against versioned packs and records which version applied.

## Where Resemble is stronger

A proprietary, multimodal watermark with years of production use, an audio heritage that SignUnder does not have, a text watermark, and prices for the primitive that are the market's reference. If what you need is a watermark and a SynthID check, with a signature in the vendor's name that you accept will display as Unrecognized until Resemble is listed, it is a reasonable choice.

Read from Resemble's own pages on the dates shown. Tell us if this is out of date and we will correct it with the date of the correction.

## Changes

- 2026-10-12 — First published.

*Not legal advice. Every cell comes from the vendor's own pages on the date shown.*
