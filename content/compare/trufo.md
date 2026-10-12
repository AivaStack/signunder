---
title: "SignUnder and Trufo, compared"
description: "Trufo is a Trust List CA selling C2PA and CAWG certificates and managed signing in its own name; conformance is still yours to do. Read 12 Oct 2026."
slug: trufo
type: compare
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [compare, trufo, certificates, cawg]
sources:
  - title: "Trufo homepage"
    url: https://trufo.ai/
    read: 2026-10-12
  - title: "Trufo pricing page (categories; prices read 8 October 2026: certificates from $300 a year)"
    url: https://trufo.ai/pricing
    read: 2026-10-12
  - title: "C2PA Certificate Policy, version 0.2"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Certificate%20Policy.pdf
    read: 2026-10-12
faq:
  - q: "Is Trufo conformant?"
    a: "Yes. Trufo describes itself as one of the original certificate authorities on the C2PA Trust List, and it holds its own conforming product records (three, as read on 8 October 2026). Signing through its managed service produces trusted signatures in Trufo's name."
  - q: "Can Trufo sign in my name?"
    a: "Not through its managed service: the Certificate Policy ties a certificate to a listed product and its name. Trufo can add a CAWG identity assertion naming you inside the manifest, and if your own product is listed, Trufo can issue you a certificate in your name to sign with yourself."
  - q: "Does Trufo get me onto the conforming products list?"
    a: "No. Its build-your-own path tells you to pass the conformance program first; the application is yours to do. SignUnder's onboarding does it from your answers."
related:
  - /guides/sign-under-your-own-name
  - /guides/c2pa-conformance-program
  - /compare
draft: false
---

Trufo is a certificate authority on the C2PA Trust List that sells C2PA signing certificates, CAWG identity certificates and a managed signing API. Signatures from its managed service are trusted, and they name Trufo. It does not take a customer through conformance, and it does not maintain rules or a per-customer detection endpoint. If your own product is listed, Trufo is one of the CAs that can issue your certificate; that is the relationship SignUnder has with CAs too.

- **Trufo:** Trust List CA; C2PA and CAWG certificates; managed APIs for "compliance (AI labeling laws)"; a watermark; certificates from $300 a year (read 8 October 2026).
- **Signer on managed signing:** Trufo's certificate; your identity via a CAWG assertion if you want it.
- **SignUnder:** gets you listed, requests the certificate in your name from a CA such as Trufo or SSL.com, signs under your key, and adds the detection endpoint and rule packs.

## What Trufo offers

From its homepage, read 12 October 2026: "Trufo issues both C2PA certificates for signing and CAWG certificates for identity"; it is "one of the original CAs on the C2PA Trust List". Two paths are described. Build your own: "Pass the C2PA conformance program", "Generate keys and get your certificate (EST)", and "link it up with certificates and timestamping from Trufo". Managed Content APIs, for companies "looking for compliance (AI labeling laws) or for brand enhancement (organization identity)", with "Verified organization identity for CAWG assertions". The pricing page lists C2PA signing certificates, CAWG identity certificates and TSA certificates as annual subscriptions, plus certificate-adjacent APIs; the prices themselves did not render in our read of 12 October, and on 8 October certificates started at $300 a year.

## Where the difference is

**Who does the conformance.** Trufo's instructions begin with "Pass the C2PA conformance program". It is a CA; the application, the security architecture document and the sample evidence are yours. SignUnder's onboarding generates the document from your answers and the samples from your signing path, tracks the application, and runs the certificate request and key ceremony when the listing lands ([the conformance guide](/guides/c2pa-conformance-program)).

**Whose name on managed signing.** The Certificate Policy requires a claim signing certificate's subject to match a Conforming Products List entry, so Trufo's managed service signs with Trufo's certificate. A CAWG identity assertion can name you inside the manifest, and it is the right tool for that; it does not change the signer a verifier displays ([the own-name guide](/guides/sign-under-your-own-name)). SignUnder's interim tier works the same way, labelled as interim; its paid tiers sign under your own listing.

**Detection and rules.** Trufo offers no per-customer detection endpoint for the Code's 2 February 2027 duty and no jurisdiction or platform rule packs.

## Where Trufo is stronger

It is an established CA with its own conformance records and a place on the Trust List, which SignUnder is not and does not intend to be. If you have the engineering to run conformance yourself and want a certificate and timestamps from a CA that understands C2PA, Trufo is a direct answer, and SignUnder customers can bring a Trufo certificate.

Read from Trufo's own pages on the dates shown. Tell us if this is out of date and we will correct it with the date of the correction.

## Changes

- 2026-10-12 — First published.

*Not legal advice and not a certificate authority. Every cell comes from the vendor's own pages on the date shown.*
