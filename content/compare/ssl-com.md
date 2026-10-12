---
title: "SignUnder and SSL.com's C2PA certificates, compared"
description: "SSL.com issues the free Level 1 certificate a listed product needs, in your name. A CA, not a conformance service. Read 12 Oct 2026."
slug: ssl-com
type: compare
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [compare, ssl-com, certificates]
sources:
  - title: "SSL.com C2PA Content Credentials product page (free tier and paid tiers)"
    url: https://www.ssl.com/products/content-authenticity/content-credentials/c2pa/
    read: 2026-10-12
  - title: "SSL.com, Free C2PA Claim Signing Certificates: Why SSL Is Giving Away Trust (6 August 2026)"
    url: https://www.ssl.com/article/free-c2pa-claim-signing-certificates-why-ssl-is-giving-away-trust/
    read: 2026-10-12
  - title: "SSL.com, How to Submit to the C2PA Conformance Program"
    url: https://www.ssl.com/article/how-to-submit-to-the-c2pa-conformance-program/
    read: 2026-10-12
faq:
  - q: "Is SSL.com's C2PA certificate free?"
    a: "One Level 1 claim signing certificate, valid for a year, at $0, for products that apply with a valid C2PA conformance record ID, issued through the SSL.com portal. Its product page says 2,500 timestamps a year; its article of 6 August 2026 says up to 10,000. Both are SSL.com's figures."
  - q: "Does SSL.com get me onto the conforming products list?"
    a: "No. It publishes a guide to the process and issues the certificate once you have a record ID. The application is yours; SignUnder does it for you."
  - q: "Is SSL.com a competitor to SignUnder?"
    a: "It is a supplier. SignUnder requests the customer's certificate from a Trust List CA, and SSL.com's free Level 1 certificate is the default choice."
related:
  - /guides/sign-under-your-own-name
  - /guides/c2pa-conformance-program
  - /compare
draft: false
---

SSL.com is a certificate authority on the C2PA Trust List that issues claim signing certificates, including one free Level 1 certificate a year to any product on the Conforming Products List. It is the certificate step of the own-name path, not the conformance step: you bring a record ID, SSL.com issues a certificate in your organisation's name. SignUnder does the conformance, requests exactly that certificate, and signs under it. The two are not alternatives.

- **SSL.com:** Trust List CA; free Level 1 certificate for listed products; Premium at $500 per Level 1 certificate a year with 10,000 timestamps; Enterprise with Level 1 and 2; a sales-led hosted Provenance API.
- **Requires:** "a valid C2PA conformance record ID", so a listing first.
- **SignUnder:** the listing, the certificate request against it, the key ceremony, signing under your name, watermark, verify and detection, rule packs.

## What SSL.com offers

From its product page, read 12 October 2026: a free tier at "$0" with "one Level 1 Claim Signing Certificate valid for 1 year", "plus 2,500 trusted timestamps per year", "Issuance via the SSL.com Portal", and "You need to apply with a valid C2PA conformance record ID". The page dates the free tier to May 2026 in one place and June 2026 in another. Premium is "$500 per Level 1 certificate, buy as many as needed" with "10,000 C2PA compliant timestamps per year included"; Enterprise offers "Level 1 and 2 certificates at volume-based pricing (e.g., for devices)". The 6 August 2026 article describes the free certificate as "Assurance Level 1", "valid for one year", with "up to 10,000 trusted timestamps", offered "to qualify C2PA conformant generator products". SSL.com's hosted Provenance API, which signs in SSL.com's name or with a CAWG assertion, was sales-led when read on 8 October 2026.

SSL.com also publishes the most-read guide to submitting to the Conformance Program, which is why it ranks for that search; the guide is a CA's view of the process and does not do the application for you.

## Where the difference is

**Conformance.** SSL.com's certificate requires a record ID; obtaining one is the paperwork SignUnder exists to do ([the conformance guide](/guides/c2pa-conformance-program)).

**Signing in your name.** Once you hold an SSL.com certificate for your listed product, you can sign under it yourself; the key management, the signing pipeline, the watermark, the renewal within 366 days and the historic validation are yours to build and run. SignUnder runs them.

**Detection and rules.** A CA issues certificates. The Code of Practice's detection duty and the jurisdiction and platform rules are outside its scope.

## Where SSL.com is stronger

It is the CA. If you have your own listing and your own engineering, SSL.com's free Level 1 certificate is the cheapest route to a trusted, own-name signature there is, and SignUnder recommends it for exactly that step.

Read from SSL.com's own pages on the dates shown. Tell us if this is out of date and we will correct it with the date of the correction.

## Changes

- 2026-10-12 — First published.

*Not legal advice and not a certificate authority. Every cell comes from the vendor's own pages on the date shown.*
