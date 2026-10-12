---
title: "How to get on the C2PA conforming products list"
description: "No fee, a median 17 days from intake to listing across 248 records, two assurance levels: the steps, the evidence reviewers ask for. Read 12 Oct 2026."
slug: c2pa-conformance-program
type: guide
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [c2pa, conformance, certificates, trust-list]
sources:
  - title: "C2PA Conformance Program, version 0.2 (docs/v0.2 in the conformance-public repository)"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Conformance%20Program.md
    read: 2026-10-12
  - title: "C2PA Generator Product Security Architecture Document Template, version 0.2"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Generator%20Product%20Security%20Architecture%20Document%20Template.md
    read: 2026-10-12
  - title: "C2PA Conforming Products List (JSON), 248 records as synced 12 October 2026"
    url: https://github.com/c2pa-org/conformance-public/blob/main/conforming-products/conforming-products-list.json
    read: 2026-10-12
  - title: "C2PA Conformance Program page (expression of interest, lists, documents)"
    url: https://c2pa.org/conformance/
    read: 2026-10-12
  - title: "C2PA Certificate Policy, version 0.2 (31 July 2026)"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Certificate%20Policy.pdf
    read: 2026-10-12
faq:
  - q: "Does it cost anything to apply to the C2PA Conformance Program?"
    a: "No. The Program document, version 0.2, states there is no application fee for generator or validator product companies and no fee for adding a record to the Conforming Products List. A certificate afterwards comes from a certificate authority on the Trust List; SSL.com's Level 1 certificate is free for listed products, others charge."
  - q: "How long does it take to get listed?"
    a: "Across the 248 records on the list on 12 October 2026, the median gap between a record's creation and its conformance date is 17 days, the mean 34, the longest 277. Forty-two products were listed in the thirty days before that date."
  - q: "What do the reviewers ask for?"
    a: "A completed intake form, a Generator Product Security Architecture Document written to the Program's template, and sample output files for every media type you claim, each with its .crjson or .json report. An Administrator reviews the evidence and an Approver who is not an Administrator signs off."
  - q: "What is the difference between Assurance Level 1 and 2?"
    a: "Both are implementation-security levels from the Generator Product Security Requirements. Level 1 is the minimum for listing; Level 2 adds hardware-rooted evidence such as attestation of the key environment. Of 220 generator products on the list, 209 have a maximum level of 1 and 11 have level 2."
  - q: "Can a small company get listed?"
    a: "Yes. The list holds 146 distinct applicants, many of them single-product companies, and the program's own sample library and open-source implementations mean the engineering is within reach. The paperwork is the work."
related:
  - /guides/sign-under-your-own-name
  - /guides/why-content-credentials-say-unrecognized
  - /law/eu-ai-act-article-50
draft: false
---

Getting a product onto the C2PA Conforming Products List costs nothing and, by the list's own dates, takes a median of 17 days. The process is: an expression-of-interest form, a legal agreement, an intake form, a security architecture document written to the Program's template, sample signed files for every media type you claim, review by the Program's Administrator and sign-off by a separate Approver, then a signed Notice of Conformant Status and a public record. This guide walks through it from the Program documents and the list itself, as read on 12 October 2026.

- Fee: **none**, for the application and for the listing.
- Time: **median 17 days** from record creation to conformance, mean 34, maximum 277 (n = 248).
- Size of the list: **248 records from 146 applicants**: 220 generator products, 28 validator products.
- Pace: **42 listed in the last 30 days**, 154 since 1 July 2026.
- Why it matters: only a listed product can get a signing certificate from a Trust List CA, and the certificate must name the listed product.

## Why would you want to be listed?

Because the C2PA Certificate Policy makes the list the gate to a real signing certificate. The Program document says claim signing certificates "SHALL only be issued to instances of Generator Products that have successfully passed the C2PA Conformance Program", and the Certificate Policy's profile for those certificates says the subject name "MUST match C2PA Conforming Products List (CPL) entry" (Program v0.2 and Certificate Policy v0.2, read 12 October 2026). Without a listing, your signatures chain to nothing a verifier trusts and display as "Unrecognized"; with one, verifiers display your organisation's name. The [own-name guide](/guides/sign-under-your-own-name) covers the consequence; this page covers the listing.

## What are the steps?

The Program document's "Process Requirements for Applicants" section lays out the path (read 12 October 2026):

1. **Expression of Interest.** All applicants "SHALL apply to the C2PA Conformance Program using this Expression of Interest Form", a Google form linked from [c2pa.org/conformance](https://c2pa.org/conformance/). It records which role you are applying for: generator product, validator product, or certificate authority.
2. **Legal agreement.** "Prior to accepting Applicants into the C2PA Conformance Program, each Applicant SHALL sign a legal agreement with the C2PA committing to the C2PA Conformance Program requirements." It is executed through the Linux Foundation's document-signing service, with separate templates per role. The templates are public in the repository's `legal-agreements` folder.
3. **Program Intake Form.** After the agreement, you receive an intake form that becomes your public record: legal business information, the product, the media types it generates and validates, the implementation class (edge, backend, distributed), the maximum assurance level you seek, and a "Date of Earliest Public Disclosure" that keeps the record hidden until you say so. The Program "strongly recommends" interoperability testing before you submit.
4. **Security architecture document.** Generator product applicants "are required to submit a Generator Product Security Architecture (GPSA) Document" and "MUST use" the Program's template. The template's headings are the evidence list: certificate enrolment and secret management, confidentiality of the claim signing key (generation, least privilege, rotation), protection of the claim generator (SCA/SBOM scanning, a 90-day remediation policy), protection of assets at generation, TLS 1.3 between subsystems, and hosting-environment controls (IAM, RBAC, OWASP Top 10). Each objective has base evidence for Levels 1 and 2 and additional evidence for Level 2.
5. **Sample evidence.** "Generator Product applicants must provide sample output media files of every asserted generate and validate media type ... along with their associated .crjson or .json files for analysis." If you claim to ingest manifests as ingredients, you show samples of that too. The Program maintains a library of conformant sample assets to test against.
6. **Assessment and approval.** The Administrator reviews the evidence, "may involve email communication", and submits the record; "All pull requests from the staging libraries to the public repository require an approval from someone besides an Administrator." An Approver reviews the record, nonconformities go back to you for remediation, and "a final re[view]" closes it.
7. **Notice and listing.** A "digitally signed Notice of Conformant Status letter" is issued and the record is published to the list, viewable in the [Conformance Explorer](https://spec.c2pa.org/conformance-explorer/). A "material change" to the product later means re-submitting.

## What does it cost?

Nothing to the C2PA. The Program document's fee schedule: "There is no application fee for Certification Authorities, Generator Product companies, or Validator Product companies applying to the C2PA Conformance Program. There is also no fee for adding applicant records to the CA Trust List or the Conforming Products List" (Program v0.2, read 12 October 2026). The certificate afterwards is a separate purchase from a Trust List CA; SSL.com offers one Level 1 certificate free to listed products, and others charge (details on the [own-name guide](/guides/sign-under-your-own-name)).

## How long does it take?

We computed this from the list itself. Each record carries a creation date and a conformance date; across all 248 records on 12 October 2026, the gap is a median of 17 days, a mean of 34, a minimum of 0 and a maximum of 277. The first conformance date on the list is 27 June 2025; the latest is 12 October 2026. The list is synced automatically, several times on busy days.

| Figure | Value (12 October 2026) |
|---|---|
| Records | 248 (220 generator products, 28 validator products) |
| Distinct applicants | 146 |
| Maximum assurance level 1 / 2 | 209 / 11 generator products |
| Program version 0.1 / 0.2 | 172 / 76 records |
| Median days, creation to conformance | 17 |
| Mean / maximum days | 34 / 277 |
| Listed in the last 30 days | 42 |
| Listed since 1 July 2026 | 154 |

The pace is the point: 154 listings in the fifteen weeks before the Article 50 grace period ends, from applicants ranging from Adobe and Google to one-product companies.

## What are the assurance levels?

Two are in operation. "An Applicant must, at a minimum, meet the implementation security requirements associated with Assurance Level 1 in order for their Generator Product to be inclu[ded]"; Level 2 requires the additional evidence in the template, centred on a hardware root of trust and attestation of the key management environment. The level is encoded in the certificate (the `c2pa-al` extension) so a relying party can see it. Dynamic evidence at enrolment can give a particular instance a lower level than the product's maximum (Program v0.2, read 12 October 2026).

## What this means if you ship AI generation

The listing is free, takes weeks rather than months, and is the one asset nobody can give you: not a platform, not a certificate vendor, not a hosted signer. The work is a security architecture document, sample files from your own signing path, and a review. Once listed, you can request a certificate that carries your name and renew it each year.

SignUnder's onboarding does this paperwork from your answers, generates the sample files and reports from the same pipeline that signs your production output, and runs the certificate request and key ceremony when the listing lands; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published, from Program v0.2, the Security Architecture Document template and the list as synced that day.

*Not legal advice; the Program's documents are the authority. Figures are recomputed from the public list each time this page is reviewed.*
