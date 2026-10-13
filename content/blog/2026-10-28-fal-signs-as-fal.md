---
title: "fal signs as fal: what your verify page will show, and why"
description: "fal.ai attaches Content Credentials to media from its hosted apps, signed as fal. A verifier shows your supplier, not you, and the rules explain why."
slug: fal-signs-as-fal
type: post
date: 2026-10-28
updated: 2026-10-28
reviewed: 2026-10-12
authors: [eoin]
tags: [fal, c2pa, platforms, own-name]
sources:
  - title: "fal.ai Verify page"
    url: https://fal.ai/verify
    read: 2026-10-12
  - title: "fal.ai Terms of Service (8 September 2026)"
    url: https://fal.ai/terms
    read: 2026-10-12
  - title: "C2PA Certificate Policy v0.2 (31 July 2026), section 1.5.1 and the claim signing certificate profile, as quoted in our own-name guide"
    url: https://signunder.com/guides/sign-under-your-own-name
    read: 2026-10-12
  - title: "European Commission, Guidelines on Article 50 (20 July 2026)"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
faq:
  - q: "Whose name does a verifier show on an image generated through fal?"
    a: "fal's, where the image is signed at all. fal's Verify page says its tool checks whether fal was the signer, and a C2PA verifier displays the organisation in the signing certificate, which is fal's."
  - q: "Can fal sign in my company's name instead?"
    a: "Not under the C2PA Certificate Policy. A claim signing certificate's subject must match an entry on the Conforming Products List, so a certificate in your name needs your own listed product."
related:
  - /platforms/fal
  - /compare/fal
  - /guides/sign-under-your-own-name
draft: true
---

If your product generates images through fal and a customer drops one into a Content Credentials verifier, the name they see is fal's, not yours. That is not a setting fal has left off. It follows from how C2PA certificates are issued, and it is the same for every hosted signer. fal is the only one of the eleven inference platforms we read that marks output at all, so this is a post about what the best case looks like.

## What does fal attach?

fal's Verify page says: "Every piece of media generated through fal's hosted applications is signed with Content Credentials (C2PA)" and that output is "embedded with an invisible watermark" (read 12 October 2026). Its public verifier checks "whether fal was the signer" and when the content was generated.

Two limits are on the same page or missing from it. The statement covers fal's hosted applications; whether calls to the raw API are signed is not documented anywhere we could find, on 8 October or on 12 October. And the page says content from other providers cannot be verified there, and that a "Not verified" result does not prove a file is human-made.

## Why does the verifier say fal?

Because the signature chains to fal's certificate, and a verifier displays the organisation named in the certificate. The C2PA Certificate Policy, version 0.2, sets two rules that settle it (read 12 October 2026):

> "Certificates issued under this CP are to be used exclusively by instances of the Generator Products named as the subjects of the certificates" — Certificate Policy v0.2, section 1.5.1

> "Subject Name: MUST match C2PA Conforming Products List (CPL) entry." — Certificate Policy v0.2, claim signing certificate profile

A certificate names a listed product and the organisation behind it. fal's certificate names fal. There is no provision for fal to hold a certificate in a customer's name, because the name would have to match a list entry, and the entry would have to be the customer's. The [own-name guide](/guides/sign-under-your-own-name) goes through the Policy and what Trufo and SSL.com show in the same position.

## Does fal's mark cover your Article 50 duty?

The Commission's guidelines of 20 July 2026 say a company that provides a generative AI application "on the Union market under its own name or trademark" is "a provider responsible for compliance with the transparency obligations in Article 50(1) and/or (2)", whoever built the model. fal's terms of 8 September 2026 agree from the other side: "USE OF SUCH OUTPUT CONTENT IS AT CUSTOMER'S SOLE RISK."

A manifest naming fal is evidence that fal generated the file. We know of no guidance saying a supplier's mark discharges the provider's own duty, and the guidelines do not answer the question. What is certain is narrower: if the mark is the only one on your output, the record a regulator, a platform or a customer reads points at your supplier.

## What happens to the mark in your pipeline?

Three ordinary things can remove or break it:

- **The API path.** If raw API output is unsigned, there is no mark to begin with. Undocumented.
- **Your own edits.** Upscaling, cropping, compositing or re-encoding after generation changes the bytes the manifest is bound to. Unless the edit is re-signed as an action, the binding fails.
- **Routing.** A product that sends some requests to fal and others to a platform that marks nothing ships a mix of supplier-marked and unmarked output.

## What this means if you build on fal

You start ahead of anyone on the other ten platforms: where fal signs, you have a supplier manifest and a watermark at no cost. Keep it. The step that remains is yours: a signature in your own name on what leaves your product, after your edits, whichever provider served the request, with fal's manifest preserved inside it as an ingredient.

SignUnder's proxy mode sits in front of fal's API and does that; it is pre-launch, and the waitlist is on the homepage. We have asked fal to document its API scope and will update the [fal page](/platforms/fal) when it does.

*Not legal advice. Quotations are from fal's own pages and the C2PA Certificate Policy on the dates shown.*
