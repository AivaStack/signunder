---
title: "Why a certificate alone cannot put your name on a manifest"
description: "A C2PA signing certificate is issued only to a listed product and must match its list entry. The listing comes first, and it is free."
slug: why-a-certificate-alone-is-not-enough
type: post
date: 2026-11-19
updated: 2026-11-19
reviewed: 2026-10-12
authors: [eoin]
tags: [c2pa, certificates, conformance, own-name]
sources:
  - title: "C2PA Certificate Policy, version 0.2 (31 July 2026): claim signing certificate profile and section 1.5.1"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Certificate%20Policy.pdf
    read: 2026-10-12
  - title: "C2PA Conformance Program, version 0.2: Certificate Policy section and fee schedule"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Conformance%20Program.md
    read: 2026-10-12
  - title: "SSL.com C2PA Content Credentials product page (free tier terms)"
    url: https://www.ssl.com/products/content-authenticity/content-credentials/c2pa/
    read: 2026-10-12
  - title: "Content Authenticity Initiative, Inspect tool documentation (signer display rules)"
    url: https://opensource.contentauthenticity.org/docs/getting-started/aca-inspect
    read: 2026-10-12
  - title: "SignUnder, the conforming products list this week (timing statistics computed from the list on 12 October 2026)"
    url: https://signunder.com/guides/conformance-list
    read: 2026-10-12
faq:
  - q: "Can I buy a C2PA certificate and start signing in my company's name?"
    a: "Not by itself. Under the C2PA Certificate Policy v0.2, claim signing certificates are issued only to generator products on the Conforming Products List, and the certificate's subject must match the list entry. Your product has to be listed first."
  - q: "What happens if I sign with a certificate that is not on the C2PA Trust List?"
    a: "The signature can be mathematically valid, but a verifier will not vouch for the signer. Adobe's Inspect tool shows the organisation name only for certificates on the C2PA Trust List; otherwise the result is Unrecognized."
  - q: "How much does the listing cost?"
    a: "Nothing to the C2PA. The Program document says there is no application fee and no fee for adding a record to the Conforming Products List."
related:
  - /guides/sign-under-your-own-name
  - /guides/c2pa-conformance-program
  - /guides/why-content-credentials-say-unrecognized
draft: true
---

A certificate does not put your company's name on a Content Credentials manifest. A listing does, and the certificate follows from it. Under the C2PA's rules a claim signing certificate can be issued only to a product that is already on the Conforming Products List, and its subject must match that entry. People reach for the certificate first because that is how TLS works. Here it is the second step, and the first one is free.

## What goes wrong if you start with the certificate?

One of three things, depending on where you got it.

**A Trust List certificate authority will not issue it.** SSL.com's free tier is "one Level 1 Claim Signing Certificate valid for 1 year", and the condition is on the same page: "You need to apply with a valid C2PA conformance record ID" (read 12 October 2026). No record, no certificate.

**A certificate from anywhere else is not trusted.** You can sign a manifest with a certificate from an ordinary public CA, or one you made yourself. The signature will be valid mathematics. A verifier will still not show your name, because that certificate does not chain to the C2PA Trust List. The Content Authenticity Initiative's Inspect tool "shows the organization name only if the signing certificate includes the 'O' or Organization Name attribute", and only for certificates on that list. What your customer sees instead is "Unrecognized"; [the guide to that message](/guides/why-content-credentials-say-unrecognized) covers the case.

**A hosted signer's certificate is the hosted signer's.** If you skip certificates altogether and call a signing service, the manifest is signed with the service's certificate and names the service. fal's verifier checks "whether fal was the signer". That is the system working as designed.

## What do the rules say?

The Certificate Policy, version 0.2 of 31 July 2026, in two lines (read 12 October 2026):

> "Certificates issued under this CP are to be used exclusively by instances of the Generator Products named as the subjects of the certificates, which have been implemented by the Subscribers, and which are enumerated on the C2PA Conforming Products List" — section 1.5.1, Intended Use

> "Subject Name: MUST match C2PA Conforming Products List (CPL) entry. Requires C, O, CN attributes." — claim signing certificate profile

The Program document says it from the other side: claim signing certificates "SHALL only be issued to instances of Generator Products that have successfully passed the C2PA Conformance Program".

Put together: the certificate names a product and an organisation; the product must be on the list; the organisation attribute is what a verifier displays. The name on screen is the name on the list.

## Why is it built this way?

Because the signature is a statement about software, not about a company's identity alone. A TLS certificate says "this server belongs to this organisation". A C2PA claim signature says "this listed product, operated by this organisation, produced this manifest", and the conformance review is what stands behind "listed": someone has looked at how the product makes manifests and how it keeps its key. A certificate handed to an unreviewed product would carry the name and none of the assurance.

## Does an identity assertion get round it?

No. A CAWG identity assertion adds a verified statement of who is responsible for the content inside the manifest, with its own certificate. It is useful, and it does not change who signed. The verifiers we have read display the claim signer's organisation. Whether TikTok, Meta, LinkedIn or Google show the assertion anywhere was undocumented when we looked on 12 October 2026.

## So what is the order?

1. **List your product.** There is "no application fee" and no fee for a record on the list (Program v0.2). Across the 248 records on the list on 12 October 2026, the median time from a record's creation to conformance was 17 days, the mean 34 and the longest 277. The work is a security architecture document and sample signed files.
2. **Request the certificate** from a Trust List CA against your conformance record ID.
3. **Keep the key where it was generated**, in a key management service or hardware.
4. **Sign**, with the organisation attribute set to your company.
5. **Renew.** The profile caps validity at "Max 366 days (Assurance Level 1)", so this is a yearly task.

The [conformance guide](/guides/c2pa-conformance-program) has the forms; the [own-name guide](/guides/sign-under-your-own-name) has what each vendor shows today.

## What this means if you want your name on your output

Budget for a short document and a few weeks of waiting, not for a purchase. The certificate is the cheap part, and one is free. The listing is the part nobody can do in your place, though the paperwork can be prepared for you.

SignUnder prepares the listing from your answers, requests the certificate, holds the key in a KMS and keeps the renewal clock; it is pre-launch, and the waitlist is on the homepage.

*Not legal advice, and not a certificate authority: the C2PA documents govern. Vendor facts are from the vendors' own pages on the dates shown.*
