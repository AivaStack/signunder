---
title: "Your company's name in Content Credentials: the CAWG identity assertion"
description: "How SignUnder puts a verified company name inside every C2PA manifest with a CAWG identity assertion, what viewers show today, and how it differs from your own certificate. 20 Oct 2026."
slug: company-name-in-content-credentials
type: guide
date: 2026-10-20
updated: 2026-10-20
reviewed: 2026-10-20
authors: [eoin]
tags: [c2pa, cawg, identity, own-name, verify]
sources:
  - title: "Creator Assertions Working Group, Identity Assertion 1.1 (DIF Ratified Specification, 16 May 2025)"
    url: https://cawg.io/identity/1.1/
    read: 2026-10-20
  - title: "contentauth/c2pa-rs, the C2PA reference implementation (identity module)"
    url: https://github.com/contentauth/c2pa-rs
    read: 2026-10-20
  - title: "SignUnder decision record 0014: the company's name in its signatures"
    url: https://github.com/AivaStack/signunder/blob/main/docs/adr/0014-company-identity-in-signatures.md
    read: 2026-10-20
faq:
  - q: "What is a CAWG identity assertion?"
    a: "A statement inside a C2PA manifest, defined by the Creator Assertions Working Group, that lets a credential holder \"prove control over a digital identity and to use that identity to document the named actor's role(s) in the C2PA asset's lifecycle\" (Identity Assertion 1.1). It is signed with its own credential, separate from the manifest's signature."
  - q: "Does the identity assertion make my company the signer?"
    a: "No. The manifest is still signed by the service's certificate; the assertion adds a second, separately signed statement naming your company. Signing under your own certificate needs a listing in the C2PA conformance program."
  - q: "Will TikTok, Instagram or LinkedIn show my company's name?"
    a: "Unknown. None of them documents displaying CAWG identity assertions, as of 20 October 2026. Your SignUnder verify page shows the name; general Content Credentials viewers may not."
  - q: "How does SignUnder check that the name is mine?"
    a: "You prove you control your domain by publishing a DNS TXT record, and we check by hand that the company name belongs to whoever controls that domain, for example from your website or a company register. Only then is a certificate issued naming both."
related:
  - /guides/sign-under-your-own-name
  - /guides/why-content-credentials-say-unrecognized
  - /docs
draft: false
---

Every image SignUnder signs can carry your company's verified name inside its Content Credentials: "Published by Acme Ltd (acme.com, verified by SignUnder)". It is done with a CAWG identity assertion, a separately signed statement in the C2PA manifest, after you prove your domain and we check your name. It does not make your company the manifest's signer; that needs your own certificate.

## What is a CAWG identity assertion?

The Creator Assertions Working Group's [Identity Assertion 1.1](https://cawg.io/identity/1.1/), ratified on 16 May 2025, lets a credential holder "prove control over a digital identity and to use that identity to document the named actor's role(s) in the C2PA asset's lifecycle". It sits inside the C2PA manifest next to the other assertions, and is signed with its own credential, separate from the certificate that signs the manifest as a whole. The specification recommends a set of roles; the one for a company putting out AI-generated content is `cawg.publisher`: "The named actor is a publisher of this C2PA asset."

## How does SignUnder add it?

1. **You prove your domain.** In the dashboard, under "Your name in signatures", you enter your company's domain and publish the TXT record it shows, `signunder-verification=…`, where your DNS is managed.
2. **We check the name.** Once the record is found, we check by hand that your company name belongs to whoever controls the domain, usually within one working day.
3. **A certificate names you.** The SignUnder Identity CA issues an X.509 certificate with your company as the organisation and your domain as the subject, its key held in a hardware security module.
4. **Every image you sign from then on carries the assertion**, with the role `cawg.publisher`, bound to the manifest's own contents so it cannot be lifted onto another image. Your public verify page and record show "Published by Acme Ltd (acme.com, verified by SignUnder)".

If you change your company's name or domain, the assertion stops until the new one is checked.

## What do viewers show today?

We signed a test image this way on 20 October 2026 and read it back:

- **The C2PA reference implementation** (c2pa-rs) validated the assertion's signature and reported it as well-formed (`cawg.identity.well-formed`, which the specification defines as an evaluation that "could not identify any root of trust for the named actor") and the identity credential as untrusted (`cawg.x509.credential.untrusted`). No shared trust list for identity credentials includes our identity CA yet, so this is the expected result.
- **Adobe's Content Credentials viewer** showed the manifest, "generated with an AI tool", but did not show the company name in the part of the panel we checked. Our test image was signed with a test certificate, which the viewer marks "Unrecognized"; the result may differ once signing uses a certificate on the C2PA Trust List.
- **Social platforms** (TikTok, Meta, LinkedIn, Google) do not document showing CAWG identity assertions at all.

So the place your name reliably appears today is your SignUnder verify page, and inside the file for any tool that reads identity assertions.

## How is this different from signing under your own certificate?

The manifest's signature is what verifiers name first. With the identity assertion, that signature is still SignUnder's: the [C2PA Certificate Policy](/guides/sign-under-your-own-name) ties a claim-signing certificate to a listed product and its owner, so a service can only sign as itself. The assertion adds your name as a second, verified statement inside.

Signing under your own certificate, so that verifiers name your company as the signer, needs a listing in the C2PA conformance program. That is part of the Growth plan once SignUnder is listed; until then, the identity assertion is how your name gets into the file.

*Every specification quotation is from Identity Assertion 1.1, read on 20 October 2026.*
