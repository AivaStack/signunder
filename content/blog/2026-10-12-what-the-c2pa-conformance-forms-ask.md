---
title: "What the C2PA conformance forms actually ask"
description: "We are applying to the C2PA Conformance Program ourselves. Here is every field on the expression-of-interest form, what the intake form wants, and the six objectives of the security document."
slug: what-the-c2pa-conformance-forms-ask
type: post
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [c2pa, conformance, build-in-public]
sources:
  - title: "C2PA Conformance Program Expression of Interest form"
    url: https://docs.google.com/forms/d/e/1FAIpQLScERZH5rKfoeSu3y6gGbkllkyeAhmF0G-kXS0eXpb2vR238Rg/viewform
    read: 2026-10-12
  - title: "C2PA Conformance Program, version 0.2: Process Requirements for Applicants"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Conformance%20Program.md
    read: 2026-10-12
  - title: "C2PA Generator Product Security Architecture Document Template, version 0.2"
    url: https://github.com/c2pa-org/conformance-public/blob/main/docs/v0.2/C2PA%20Generator%20Product%20Security%20Architecture%20Document%20Template.md
    read: 2026-10-12
faq:
  - q: "What does the expression-of-interest form ask?"
    a: "Nine fields: your email, the legal organisation name as registered, the registered street address, city, state or province, postal code and country, which role you are applying for (certificate authority, generator product or validator product), and whether to send you a copy. Nothing about the product yet."
  - q: "What comes after the form?"
    a: "A legal agreement through the Linux Foundation's signing service, then an intake form that becomes your public record, then the security architecture document and sample signed files, then review by the Administrator and sign-off by a separate Approver."
related:
  - /guides/c2pa-conformance-program
  - /guides/sign-under-your-own-name
draft: false
---

SignUnder cannot credibly take a customer through the C2PA Conformance Program without having been through it, so our own application is the first thing we did. This is what the forms ask, in order, from the documents and the form itself, read on 12 October 2026. The short version: the first form is a postcard, the real work is the security architecture document, and nothing in it is beyond a careful engineer with a week.

## The expression-of-interest form: nine fields

The Program document says every applicant "SHALL apply to the C2PA Conformance Program using this Expression of Interest Form". It is a Google form, and it asks for exactly this:

1. Email.
2. "Legal Organization Name (the name registered with your jurisdiction)".
3. "Legal Business Street Address (The address registered with your jurisdiction)".
4. City.
5. State or province.
6. Postal code.
7. Country.
8. "Which product or service are you interested in submitting?" with three options: Certification Authority, Generator Product, Validator Product.
9. Whether to send you a copy of your responses.

That is all. Two things in its introduction matter: the C2PA is a Delaware limited liability company, so "entities on the Entity List published by the United States Department of Commerce's Bureau of Industry and Security (BIS) cannot participate", and the legal name you give here is the name that will be on your public record and, later, in the O attribute of your certificate. Use the registered name, exactly.

## The legal agreement

After the form, "the Applicant will be directed to the Linux Foundation's document signing service to execute the Agreement", with separate templates per role; the templates are public in the repository's legal-agreements folder. Your company's signatory signs; for generator products there is no countersignature step described. Once signed, the Program sends the intake form.

## The intake form: your public record

The intake form "is used to build the conformant record on one of our public lists". For a generator product it asks for the legal business information to be published, the product name and minimum version, the media types the product generates and validates, chosen from the most specific type available, the implementation class (edge, backend or distributed), the maximum assurance level you are seeking, and a "Date of Earliest Public Disclosure" that keeps your record off the public list until that date. The Program "strongly recommends" interoperability testing before you submit. What you enter here is what the Conforming Products List and the Conformance Explorer will show, and what your certificate's subject must match.

## The security architecture document: six objectives

This is the work. Generator product applicants "MUST use" the Program's template, and its headings are the evidence list:

- **O.1, certificate enrolment:** how your product proves eligibility when it requests a certificate, and how enrolment secrets are managed.
- **O.2, confidentiality of the claim signing key:** where the key is generated, who can use it, how it rotates. Level 2 adds the key management environment and hardware attestation.
- **O.3, protection of the claim generator:** software composition analysis and SBOM scanning with a 90-day remediation policy; Level 2 adds exploit countermeasures, static analysis and patch recency.
- **O.4, protection of assets and assertions at generation:** the same discipline for the content software.
- **O.5, traffic between subsystems:** TLS 1.3 and cryptographic protocols for distributed and backend classes.
- **O.6, the hosting environment:** IAM, RBAC, cloud resource policies and the OWASP Top 10.

Each objective has base evidence for Levels 1 and 2 and additional evidence for Level 2. For a backend product signing with a key in a cloud KMS under a sign-only policy, O.2 is a description of that policy; for Level 2 it is also the KMS's attestation story.

## Sample evidence

"Generator Product applicants must provide sample output media files of every asserted generate and validate media type ... along with their associated .crjson or .json files for analysis." If you claim to ingest manifests as ingredients, you show that too. The Program keeps a library of conformant sample assets to test against. Our samples come from the same pipeline that will sign production output, which is the point of doing this before launch.

## Review

The Administrator reviews the evidence and "may involve email communication"; then "All pull requests from the staging libraries to the public repository require an approval from someone besides an Administrator." Nonconformities come back to you for remediation. When both are satisfied, a signed Notice of Conformant Status follows and the record goes public on your chosen date. The list's own dates say the median wait is 17 days.

## Where we are

Our form is in, our document is drafted to the template, and our samples are generated. We will publish the day we are listed, and the day the reviewers ask for something we did not expect. Nothing on this site will claim conformance before the list does.

*Not legal advice. The Program documents govern; this is what they say as read on the date shown.*
