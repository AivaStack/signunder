---
title: "Why a verifier says \"Unrecognized\" for your Content Credentials, and the fix"
description: "Inspect shows Unrecognized when the signing certificate is not on the C2PA Trust List; MetaWatch calls it untrusted_issuer. Only a listed product's certificate fixes it. Read 12 Oct 2026."
slug: why-content-credentials-say-unrecognized
type: guide
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [c2pa, verify, trust-list, certificates, troubleshooting]
sources:
  - title: "Content Authenticity Initiative, Inspect tool documentation"
    url: https://opensource.contentauthenticity.org/docs/getting-started/aca-inspect
    read: 2026-10-12
  - title: "IPTC MetaWatch, C2PA checker"
    url: https://metawatch.iptc.org/c2pa/
    read: 2026-10-12
  - title: "C2PA Conformance Program page (Trust List, Interim Trust List freeze)"
    url: https://c2pa.org/conformance/
    read: 2026-10-12
  - title: "C2PA Certificate Policy, version 0.2 (31 July 2026)"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Certificate%20Policy.pdf
    read: 2026-10-12
  - title: "C2PA Trust List files (conformance-public repository)"
    url: https://github.com/c2pa-org/conformance-public/tree/main/trust-list
    read: 2026-10-12
faq:
  - q: "Does Unrecognized mean the signature is invalid?"
    a: "No. The manifest can be intact and the signature mathematically valid. Unrecognized means the certificate that signed it is not on the C2PA Trust List, so the verifier will not vouch for who signed. IPTC's checker lists the same case as untrusted_issuer and notes such signers are often legitimate."
  - q: "I used a certificate from a well-known public CA. Why is it still Unrecognized?"
    a: "Because a web or code-signing CA is not the C2PA Trust List. Only certificates chaining to a CA on that list, issued to a product on the Conforming Products List, are recognised. A TLS certificate from any CA will display as Unrecognized."
  - q: "Can I ask the verifier to add my certificate?"
    a: "The Inspect documentation describes a temporary known-certificate list with a request address, used until the official C2PA Trust List took over. Inspect now checks the C2PA Trust List, and the Interim Trust List was frozen on 1 January 2026. The route is a conformance listing and a Trust List certificate, not a request."
  - q: "Will a hosted signing service fix it?"
    a: "It fixes Unrecognized by replacing it with the service's name. The signer shown will be the service, because its certificate is issued to its listed product. If you want your own name, the fix is your own listing."
  - q: "Why does the verifier show no name even though my certificate is trusted?"
    a: "Inspect displays the organisation only if the certificate carries the O (Organization Name) attribute. The Certificate Policy requires C, O and CN in claim signing certificates, so a correctly issued one has it."
related:
  - /guides/sign-under-your-own-name
  - /guides/c2pa-conformance-program
  - /compare
draft: false
---

"Unrecognized" is what the Content Authenticity Initiative's Inspect tool displays when a Content Credential was signed by a certificate that is not on the C2PA Trust List. It is not a verdict on the signature, which may be perfectly valid; it is the verifier declining to say who signed. The only durable fix is a certificate that chains to the Trust List, and the Certificate Policy issues those only to products on the Conforming Products List, in the listed name.

- Inspect: **"Unrecognized"** when the signing certificate "is NOT on the C2PA trust list".
- IPTC MetaWatch: **`untrusted_issuer`**: "structurally fine but the signing certificate isn't in our default trust list".
- Causes: a test or self-signed certificate; a certificate from an ordinary CA; a vendor that is not conformant; the Interim Trust List, frozen since 1 January 2026.
- Fix: a Trust List certificate, which needs **a listed product** and therefore your own conformance record.

## What exactly do the verifiers say?

The Inspect documentation, read 12 October 2026:

> "If the Content Credential was signed by a certificate that is NOT on the C2PA trust list, such as one of the SDK's test certificates, then Inspect displays 'Unrecognized' at the top of [the signing section]. However, if the Content Credential was signed by a certificate on the C2PA trust list, then this section displays the name of the issuer of the claim signature from the `signature_info.issuer` property"

And: "This section shows the organization name only if the signing certificate includes the 'O' or Organization Name attribute". For trusted signers it also shows the time of the claim signature.

IPTC's MetaWatch checker makes the same distinction with a different label. Its `untrusted_issuer` outcome is a manifest that "is structurally fine but the signing certificate isn't in our default trust list", and it adds: "Often legitimate (e.g. Adobe, Canva) — we list them here rather than as outright failures" (read 12 October 2026). Other validators built on the open-source c2pa-rs library report a trust failure the same way when run against the C2PA Trust List.

## Why does it happen?

Four reasons, in rough order of how often we see them:

1. **A test or self-signed certificate.** The SDKs ship test certificates for development. They sign fine and display as Unrecognized by design.
2. **A certificate from a web, email or code-signing CA.** Those CAs are not on the C2PA Trust List, whatever their standing elsewhere. The Trust List is its own list of CAs that have joined the Conformance Program under the Certificate Policy.
3. **A vendor that is not on the Conforming Products List.** A service can market C2PA signing and still not be listed; its signatures then chain to nothing on the Trust List. Our [comparison pages](/compare) note which vendors are listed.
4. **The Interim Trust List.** Before the official list, verifiers used an interim list. "The Interim Trust List (ITL) was frozen on January 1, 2026. Existing certificates will remain valid for legacy support, but no future refreshes or additions will occur" (c2pa.org/conformance, read 12 October 2026). New certificates have to come through the official programme.

## Why can't it be fixed by asking?

Older Inspect documentation described a temporary known-certificate list, with an address to request inclusion for a production application. That list was explicitly a stopgap "until C2PA publishes the official list". The official C2PA Trust List now exists, Inspect checks against it, and membership is not by request: a CA joins the Trust List by meeting the Certificate Policy, and a CA issues a claim signing certificate only to a product that has passed the Conformance Program, with a subject that "MUST match C2PA Conforming Products List (CPL) entry" (Certificate Policy v0.2, read 12 October 2026).

So the chain is: your product is listed; a Trust List CA issues a certificate naming it; your manifests chain to that CA; the verifier shows your organisation. Break any link and you are back to Unrecognized.

## What is the fix?

- **For development:** nothing. Unrecognized is the expected display for test certificates; make sure production never uses one.
- **For production under your own name:** get the product listed (free, a median of 17 days; the [conformance guide](/guides/c2pa-conformance-program)), request a certificate from a Trust List CA against your record ID, sign with it, renew within 366 days. The [own-name guide](/guides/sign-under-your-own-name) has the detail.
- **For production under a service's name:** sign through a listed service. The display becomes the service's organisation, which is a different result from the one most companies want.

One check before you blame the certificate: confirm the O attribute is present. A trusted certificate without it displays no organisation name, which looks like a trust problem and is a naming one.

SignUnder's own verify page says the trust status plainly, including on content signed under our interim certificate, which it labels as interim rather than letting it pass as yours. Pre-launch; waitlist on the homepage.

## Changes

- 2026-10-12 — First published, from the Inspect documentation, MetaWatch, the Conformance Program page and Certificate Policy v0.2.

*Not legal advice and not a certificate authority. The C2PA documents govern; verifier behaviour is quoted from the verifiers' own documentation on the dates shown.*
