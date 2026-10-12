---
title: "SignUnder or do it yourself with c2pa-rs and TrustMark"
description: "Open source signs and watermarks well: c2pa-rs, TrustMark, PDQ, free listing and certificate. Left: paperwork, renewal, rules, detection. Read 12 Oct 2026."
slug: diy
type: compare
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [compare, open-source, c2pa-rs, trustmark]
sources:
  - title: "c2pa crate on crates.io (0.91.2, 6 October 2026)"
    url: https://crates.io/crates/c2pa
    read: 2026-10-12
  - title: "TrustMark (Adobe), Apache 2.0"
    url: https://github.com/adobe/trustmark
    read: 2026-10-08
  - title: "C2PA Conformance Program, version 0.2 (no fee; process)"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Conformance%20Program.md
    read: 2026-10-12
  - title: "SSL.com C2PA Content Credentials product page (free Level 1 certificate)"
    url: https://www.ssl.com/products/content-authenticity/content-credentials/c2pa/
    read: 2026-10-12
faq:
  - q: "Can I do all of this with open source?"
    a: "The signing and watermarking, yes: c2pa-rs is the reference implementation, TrustMark is an Apache 2.0 watermark, PDQ an open perceptual hash, the conformance listing is free and SSL.com's Level 1 certificate is free. Several listed products are plainly c2pa-rs plus their own certificate."
  - q: "What is left after the libraries?"
    a: "Your conformance application and security architecture document; a key in a KMS and a certificate renewed every 366 days; a public detection endpoint for every output you have ever produced, interoperable by 2 February 2027; the jurisdiction and platform rules, which change monthly; and measuring whether your watermark survives the platforms."
  - q: "Is SignUnder open source?"
    a: "The core libraries for signing, watermarking and fingerprinting are published as open source, so the do-it-yourself route uses the same code. The hosted service adds the listing, the keys, the endpoints and the rules."
related:
  - /guides/c2pa-conformance-program
  - /guides/sign-under-your-own-name
  - /law/eu-code-of-practice
draft: false
---

You can build this yourself, and we publish our core as open source so that you can. The reference implementation signs, Adobe's TrustMark watermarks, Meta's PDQ fingerprints, the conformance listing is free and SSL.com's Level 1 certificate is free. What the libraries do not give you is the listing itself, a key custody and renewal routine, a detection endpoint that must answer for every output you have ever produced, and seven jurisdictions' and five platforms' rules kept current. That is the part SignUnder sells; this page is honest about where the line falls.

- **Libraries:** c2pa-rs 0.91.2 (6 October 2026), c2pa-python, c2pa-node, c2pa-web; TrustMark (Apache 2.0); PDQ.
- **Free assets:** the conformance listing (median 17 days), one SSL.com Level 1 certificate a year.
- **Not in a library:** the security architecture document and sample evidence; KMS-held keys and 366-day renewal; a public, interoperable detection endpoint with uptime; rule packs; survival measurements.

## What the open-source stack gives you

The C2PA reference implementation, c2pa-rs, at version 0.91.2 on 6 October 2026, builds and validates manifests and is what many listed products run; it has Python, Node and web bindings. TrustMark, Adobe's Apache 2.0 watermark, carries about a hundred bits and survives resizing, JPEG compression and modest crops by Adobe's account. PDQ, Meta's perceptual hash, gives a fingerprint when both metadata and watermark are gone. The Conformance Program charges nothing, publishes its templates, and lists products in a median of 17 days; SSL.com issues one Level 1 certificate a year free against the record ID. Put together, an engineer can sign and watermark images in a week and be listed in a month.

## What is left

1. **The listing.** A security architecture document to the Program's template, sample files with their reports for every media type, and a review. Days of unfamiliar work the first time, and again on any material change ([the conformance guide](/guides/c2pa-conformance-program)).
2. **Keys and renewal.** A signing key that never leaves a KMS or HSM, a certificate whose subject matches your listing, renewal within 366 days every year with a new key, and validation of historic manifests against retired certificates.
3. **Detection with an SLA.** The Code of Practice asks for a detection solution for your own outputs, available in the Union, interoperable by 2 February 2027; California asks for a free public tool with an API. It has to be up, fast, and able to answer for every output you have ever produced, by manifest, watermark or fingerprint.
4. **Rules.** The EU, California, China, South Korea, India and New York, and the labelling policies of five platforms, change monthly; which layers and labels an output needs depends on where it goes.
5. **Measurement.** Vendors' robustness claims are not evidence. Whether your watermark survives TikTok's re-encode is something you measure, and re-measure.

## When to do it yourself

If you have an engineer-week now and an hour a week forever, one jurisdiction, one provider and outputs that are not edited after generation, build it: the stack is good and the listing is yours either way. If you route between providers, edit outputs, ship to several jurisdictions, or need the detection endpoint to be someone's job, that is what SignUnder is for, on the same open code.

## Changes

- 2026-10-12 — First published.

*Not legal advice. Library versions are as published on the dates shown.*
