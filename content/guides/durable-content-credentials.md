---
title: "Durable Content Credentials: manifest, watermark and fingerprint"
description: "No single technique suffices, says the C2PA Deployment Guidance: metadata is stripped, watermarks defeated, fingerprints probabilistic. Together they hold."
slug: durable-content-credentials
type: guide
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [c2pa, durable, watermark, fingerprint, soft-binding]
sources:
  - title: "C2PA, Content Credentials Deployment Guidance 1.0 (8 July 2026)"
    url: https://c2pa.org/wp-content/uploads/sites/33/2026/07/Content-Credentials-Deployment-Guidance.pdf
    read: 2026-10-12
  - title: "Code of Practice on Transparency of AI-Generated Content, Measures 1.1 and 1.2"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/129555
    read: 2026-10-12
  - title: "C2PA Content Credentials Specification 2.x, soft binding assertion"
    url: https://spec.c2pa.org/specifications/specifications/2.3/specs/C2PA_Specification.html
    read: 2026-10-08
faq:
  - q: "What is a durable Content Credential?"
    a: "The C2PA's term for a signed manifest combined with an imperceptible watermark and a fingerprint, so that provenance can be recovered after the manifest is stripped: the watermark carries an identifier back to the record, and the fingerprint matches the content itself."
  - q: "Why is the manifest alone not enough?"
    a: "Because it is metadata. The Deployment Guidance says embedded information can be stripped, and most publishing platforms re-encode uploads. The Code of Practice says no single marking technique suffices and asks for signed metadata plus an imperceptible watermark."
  - q: "What is a soft binding?"
    a: "An assertion in the manifest that records the watermark payload or fingerprint, so manifest and content cross-reference: a verifier that finds the watermark can look up the manifest, and a manifest can be checked against the content it claims."
related:
  - /law/eu-code-of-practice
  - /guides/sign-under-your-own-name
  - /labels
draft: false
---

A durable Content Credential is a signed C2PA manifest plus an imperceptible watermark plus a perceptual fingerprint, bound together so that provenance survives the trip through platforms that strip metadata. The C2PA's own Deployment Guidance of 8 July 2026 puts it plainly: "No single technique is sufficient on its own: embedded information can be stripped, watermarks can be defeated, and fingerprint matching is probabilistic rather than exact." The EU Code of Practice reaches the same conclusion and asks providers for signed metadata and a watermark on every output.

- **Manifest:** signed metadata in the file; the record of who, what, when; stripped by most uploads.
- **Watermark:** a few dozen to a hundred bits hidden in the pixels or samples; survives re-encoding and screenshots in most cases; carries an identifier back to the record.
- **Fingerprint:** a hash of the content itself, stored with the record; nothing in the file; matches a copy even when both other layers are gone.
- **Soft binding:** the manifest assertion that ties the three together.

## What the Deployment Guidance says

The C2PA's guidance, read 12 October 2026, separates the three mechanisms carefully. "Though often conflated, the C2PA technical specification is distinct from watermarking and fingerprinting technology. When combined, they can form what is referred to as 'Durable Content Credentials.'" On watermarks: "a watermark is designed to survive actions that would strip or separate a file from its credentials, such as screen-shotting or re-recording. However, watermarks can only carry a small amount of data, and can in some cases be removed or spoofed. But when present, the watermark serves as a persistent signal that the content is associated with a particular Content Credentials, even if the embedded version is lost." On fingerprints: "Fingerprinting creates a numerical value derived from the content's actual pixels, frames, or audio waveforms ... The fingerprint is stored alongside the Content Credential and can be recomputed later to attempt to match content back to its provenance record, even if the embedded information has been removed or lost."

And the conclusion: "Durable Content Credentials offer the potential to combine all three into a unified, layered approach that ensures provenance information can be recovered no matter where content travels or how it is redistributed."

## What the Code of Practice asks for

Sub-measure 1.1.1 of the Code asks for digitally signed, time-stamped metadata; Sub-measure 1.1.2 for an imperceptible watermark "intended to serve as a robust mechanism to complement the digitally signed metadata". The Code's premise is that "no single marking technique suffices to meet the four requirements in Article 50(2)". The [Code page](/law/eu-code-of-practice) has the measures in full.

## How the layers are bound

The manifest carries a soft binding assertion that records the watermark's payload, or the fingerprint algorithm and value, so that the layers reference each other. A verifier that receives a stripped file decodes the watermark, finds an identifier, and looks up the manifest in the signer's registry; a verifier that receives a screenshot computes the fingerprint and matches it against the same registry. In both cases the answer is the original signed record, with the original signer's name.

This is also why the registry is part of the product and not just the file: durability means somebody is holding the record and answering lookups, which is what a detection endpoint is.

## What survives what

By the platforms' own documentation, every major publishing platform re-encodes uploads, and several are reported to strip metadata; the [label pages](/labels) quote each. Watermark survival under each platform's re-encode, and under crops, resizes and screenshots, is a measurement, not a claim; we run that suite and publish the results with their dates rather than relaying a vendor's figure.

SignUnder's outputs carry all three layers with the soft binding, and its verify and detection endpoints answer by manifest, by watermark, or by fingerprint; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published, from the Deployment Guidance 1.0 and the Code.

*Not legal advice. Quotations are from the C2PA's and the Commission's documents on the dates shown.*
