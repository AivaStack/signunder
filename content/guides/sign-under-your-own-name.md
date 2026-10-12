---
title: "Why hosted C2PA signers sign in their name, and the only way to sign in yours"
description: "The Certificate Policy ties each signing certificate to a listed product and its name, so hosted signers sign as themselves. Read 12 Oct 2026."
slug: sign-under-your-own-name
type: guide
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [c2pa, certificates, trust-list, own-name, cawg]
sources:
  - title: "C2PA Certificate Policy, version 0.2 (31 July 2026): claim signing certificate profile and section 1.5.1"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Certificate%20Policy.pdf
    read: 2026-10-12
  - title: "C2PA Conformance Program, version 0.2: Certificate Policy section"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Conformance%20Program.md
    read: 2026-10-12
  - title: "fal.ai Verify page"
    url: https://fal.ai/verify
    read: 2026-10-12
  - title: "Trufo homepage"
    url: https://trufo.ai/
    read: 2026-10-12
  - title: "SSL.com C2PA Content Credentials product page (free tier terms)"
    url: https://www.ssl.com/products/content-authenticity/content-credentials/c2pa/
    read: 2026-10-12
  - title: "SSL.com, Free C2PA Claim Signing Certificates: Why SSL Is Giving Away Trust (6 August 2026)"
    url: https://www.ssl.com/article/free-c2pa-claim-signing-certificates-why-ssl-is-giving-away-trust/
    read: 2026-10-12
  - title: "Content Authenticity Initiative, Inspect tool documentation (signer display rules)"
    url: https://opensource.contentauthenticity.org/docs/getting-started/aca-inspect
    read: 2026-10-12
faq:
  - q: "Can a hosted signing service sign C2PA manifests in my company's name?"
    a: "Not under the current Certificate Policy. A claim signing certificate is issued only to a product on the Conforming Products List, and its subject must match that listing. A service's certificate names the service, so manifests it signs show the service as signer. Your name on the signature needs your own listed product and your own certificate."
  - q: "What does a CAWG identity assertion do?"
    a: "It puts your identity inside the manifest, backed by a separate identity certificate, as a statement about who is responsible for the content. It does not change the claim signer: verifiers still show the certificate holder as the signer. Whether platforms display the assertion at all is undocumented; we are testing it."
  - q: "Is the free SSL.com certificate really free?"
    a: "The product page read on 12 October 2026 lists a free tier at $0: one Level 1 claim signing certificate valid for one year, and it says you need to apply with a valid C2PA conformance record ID. The page states 2,500 timestamps a year for the free tier; SSL.com's article of 6 August 2026 says up to 10,000. Both are SSL.com's own figures and they disagree."
  - q: "How often does the certificate need renewing?"
    a: "At least every 366 days at Assurance Level 1, which is the maximum validity the Certificate Policy allows for a claim signing certificate."
  - q: "Does the signer's name matter to a regulator?"
    a: "Article 50(2) puts the marking duty on the provider of the system. A manifest naming your supplier records that your supplier generated the content. We know of no guidance that says a supplier's mark discharges your duty, and no verifier that shows your name from it."
related:
  - /guides/c2pa-conformance-program
  - /guides/why-content-credentials-say-unrecognized
  - /platforms
  - /law/eu-ai-act-article-50
draft: false
---

Every hosted C2PA signing service signs in its own name because the rules leave it no other option: a claim signing certificate can only be issued to a product on the Conforming Products List, and the certificate's subject must match that listing. fal signs as fal, Trufo as Trufo, SSL.com's hosted API as SSL.com. The only way for a verifier to show your company as the signer is your own listed product with your own certificate, and that is a free listing plus a certificate renewed every year.

- Rule: claim signing certificates **only to listed generator products**, subject name **must match the list entry** (Certificate Policy v0.2).
- Consequence: a hosted signer's manifests name **the hosted signer**.
- Identity assertions (CAWG) add your name **inside** the manifest; they do not change who signed it.
- Your own name: your own listing (free, median 17 days) and a certificate from a Trust List CA (SSL.com: one Level 1 certificate free; Trufo and DigiCert sell them).
- Validity: **at most 366 days** at Level 1, then renew.

## What does the Certificate Policy actually say?

Three provisions, from version 0.2 of 31 July 2026, read 12 October 2026:

> "Certificates issued under this CP are to be used exclusively by instances of the Generator Products named as the subjects of the certificates, which have been implemented by the Subscribers, and which are enumerated on the C2PA Conforming Products List" — Certificate Policy v0.2, section 1.5.1, Intended Use

> "Subject Name: MUST match C2PA Conforming Products List (CPL) entry. Requires C, O, CN attributes." — Certificate Policy v0.2, claim signing certificate profile

> "Validity Period: Max 366 days (Assurance Level 1)" — same profile

The Program document says the same from the other side: claim signing certificates "SHALL only be issued to instances of Generator Products that have successfully passed the C2PA Conformance Program" (Program v0.2, Certificate Policy section). Allowed algorithms are RSA (PSS or SHA-2 variants), ECDSA (SHA-2 variants) and Ed25519; RSA keys of 2048 bits or more, or P-256, P-384 or P-521.

The O attribute is what verifiers display. The Content Authenticity Initiative's Inspect tool "shows the organization name only if the signing certificate includes the 'O' or Organization Name attribute", and only for certificates on the C2PA Trust List (Inspect documentation, read 12 October 2026). So the name a verifier shows is the organisation in the certificate, which is the organisation on the list, which is the product's own conformance record.

## Why can't a service just sign for me?

Because its certificate is its certificate. A service that has listed its own generator product holds a certificate whose subject is that product and that organisation. When it signs your output, the manifest's claim signature chains to the service's certificate, and every verifier that reads it reports the service. Nothing in the Policy allows the service to hold a certificate in your name, because the subject must match a list entry, and the entry would have to be yours.

This is not a gap a vendor can close with a product feature. It is the trust model: a signature means "this listed product, operated by this organisation, generated this". For your name to appear, your product has to be the listed product.

## What do the hosted signers actually show?

Read on 12 October 2026 from each vendor's own pages:

- **fal.ai** signs media from its hosted applications "with Content Credentials (C2PA)" and its verifier checks "whether fal was the signer". The signer is fal.
- **Trufo** "issues both C2PA certificates for signing and CAWG certificates for identity" and is "one of the original CAs on the C2PA Trust List". Its build-your-own path tells you to "Pass the C2PA conformance program" and get your certificate from Trufo; its managed APIs offer "Verified organization identity for CAWG assertions". Signing through the managed service puts Trufo's certificate on the signature; your own name needs your own listing and a certificate from Trufo as the CA.
- **SSL.com** sells certificates as a Trust List CA and offers a free tier: "one Level 1 Claim Signing Certificate valid for 1 year", "Issuance via the SSL.com Portal", and "You need to apply with a valid C2PA conformance record ID". Timestamps: 2,500 a year on the product page, "up to 10,000" in the 6 August 2026 article; SSL.com's own figures disagree. Premium certificates are listed at $500 per Level 1 certificate a year.

A vendor that is not on the list at all, whatever it markets, produces signatures that chain to nothing on the Trust List and display as "Unrecognized"; the [Unrecognized guide](/guides/why-content-credentials-say-unrecognized) explains that case.

## What about a CAWG identity assertion?

The Creator Assertions Working Group's identity assertion lets a manifest carry a verified statement of who is responsible for the content, backed by an identity certificate separate from the claim signing certificate. Trufo issues such certificates. It is the right tool for "this content is ours" when a service signs on your behalf, and we use it ourselves for interim signing before a customer's own listing.

What it does not do is change the signer. The claim signature still chains to the service's certificate, and the verifiers we have read display the claim signer's organisation, not the identity assertion. Whether TikTok, Meta, LinkedIn or Google surface the assertion anywhere is undocumented as of 12 October 2026; we are testing it with real uploads and will publish what displays.

## So what is the route to your own name?

1. **Get your product listed.** Free, a median of 17 days, a security architecture document and sample files. The [conformance guide](/guides/c2pa-conformance-program) has the steps.
2. **Request a certificate from a Trust List CA** against your conformance record ID: SSL.com's free Level 1 certificate, or a paid one from SSL.com, Trufo or DigiCert.
3. **Generate the key where the certificate will live**, in a key management service or hardware, and never let it out; the Policy's Level 2 evidence is about exactly this.
4. **Sign with your own key under your own certificate**, with the O attribute set to your organisation, and verifiers show your name.
5. **Renew within 366 days**, every year, with a new key and certificate; old manifests stay valid against the retired certificate's validity period.

SignUnder exists to run those five steps for you and keep them running: the listing from your answers, the certificate request, the key ceremony in a KMS, signing under your key, and the renewal clock. It is pre-launch; the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published, from Certificate Policy v0.2 and the Program document, and the vendors' pages read that day.

*Not legal advice, and not a certificate authority: the C2PA documents govern. Vendor facts are from the vendors' own pages on the dates shown; tell us if one has changed.*
