---
title: "Interoperable detection by 2 February 2027: what is actually required"
description: "The Code of Practice asks signatories to make a detection solution available now and an interoperable access method or signpost by 2 February 2027. What the text says and what is still undefined. Read 12 Oct 2026."
slug: interoperable-detection-2027
type: guide
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [detection, code-of-practice, interoperability, 2027]
sources:
  - title: "Code of Practice on Transparency of AI-Generated Content, Measures 2.1 and 3.4"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/129555
    read: 2026-10-12
  - title: "California Business and Professions Code section 22757.2 (detection tool), FindLaw's copy current as of 1 January 2026"
    url: https://codes.findlaw.com/ca/business-and-professions-code/bpc-sect-22757-2/
    read: 2026-10-12
faq:
  - q: "What is due on 2 February 2027?"
    a: "An interoperability solution for detection mechanisms: either a publicly available, interoperable industry-standard access method that routes detection queries to the right mechanism, or a publicly readable signpost in the content that tells the public which detection solution to use. Signatories implement one or both."
  - q: "Does the detection solution have to be public?"
    a: "It has to be made available in the Union to deployers, users, integrators, end-users and other legitimate parties, as a public specification, as software, or as a cloud service with an API. California separately requires a free public tool with an API from covered providers."
  - q: "Is the interoperability standard defined yet?"
    a: "No. The Code says standards and best practices are yet to be developed except for digitally signed metadata, which is why the requirement is staged. The access-method form of the duty depends on work that is not public as of 12 October 2026."
related:
  - /law/eu-code-of-practice
  - /guides/durable-content-credentials
  - /law/california-sb-942-ab-853
draft: false
---

Under the EU Code of Practice, a provider of a generative system owes two things on detection: a detection solution for its own outputs, available in the Union now, and an interoperability mechanism for that solution by 2 February 2027. The first is concrete. The second has two permitted forms, an industry-standard access method or a signpost in the content, and the standard behind the first form does not exist yet. This page quotes what the Code requires and marks what remains undefined.

- Now: a **detection solution** for your own system's outputs, as a specification, software or an API, available in the Union (Measure 2.1).
- By **2 February 2027**: an **interoperability solution**, via a standard access method, a signpost in the content, or both (Measure 3.4).
- Defined today: interoperability for **digitally signed metadata**. Not yet: a common detection API.
- California, separately: a **free public detection tool with an API** for covered providers (section 22757.2).

## The detection solution

Measure 2.1, read 12 October 2026: "Signatories will make available a detection solution, composed of one or more detection mechanisms, to enable deployers, users of their generative AI system, third-party integrators, end-users exposed to the content, and other legitimate parties (such as competent authorities, independent researchers, civil society and media organisations) to verify whether content has been generated or manipulated by their AI system". Sub-measure 2.1.1 lists the forms: "(i) a public, ideally standardised, specification allowing any third party to implement a detection mechanism; (ii) a piece of software (e.g., a standalone executable or library); (iii) a cloud-based service accessible to users in the Union through an API."

The scope is first-party. The question the solution answers is whether content came from your system, not whether it is AI at all.

## The interoperability requirement

The Code acknowledges that "relevant interoperability standards and/or best practices are yet to be developed, except for digitally signed metadata", and stages the duty. From 2 August 2026 signatories "adopt relevant established standards or best practices to implement metadata marking and detection, thereby enabling open detection of such markings" and "provide public information to the Commission and relevant stakeholders on how to integrate and access detection solutions for the other type(s) of marking technique(s)". Then:

> "Signatories will implement an interoperability solution for their detection mechanisms by 2 February 2027 by implementing one or more of the following: i) a publicly available interoperable industry standard access method that enables detection queries to be routed to relevant detection mechanisms, and detection results to be interpreted by shared or provider-agnostic detection solutions interacting with endpoints implementing such API ...; ii) a publicly readable signpost or other interoperable mechanism in the AI-generated or manipulated content that will signal to the public which detection [solution to use]" — Code, Measure 3.4, read 12 October 2026

A signed manifest whose soft binding records the watermark identifier and the provider's detection endpoint is a signpost in the sense of (ii), and it exists today. The access method in (i) is the part that waits on a standard.

## California's version

Section 22757.2 of California's AI Transparency Act requires a covered provider's detection tool to be "at no cost to the user", publicly accessible, accepting uploads or a URL, with "an application programming interface that allows a user to invoke the tool without visiting" the provider's site, and collecting no personal information beyond opt-in feedback contacts. It is more specific than the Code and compatible with it: a public endpoint with an API satisfies both.

## What to build, given the uncertainty

A per-provider endpoint that answers "is this one of ours" by manifest, by watermark and by fingerprint, publicly, with an API, satisfies Measure 2.1 and California today and is the natural target for whatever access method the standard specifies. Its response format should be adaptable rather than fixed, and its records must cover every output the provider has ever produced, because the question can arrive years later.

SignUnder runs that endpoint per customer, on the customer's own subdomain, and will adopt the access method when it is specified; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published, from the Code and the Californian section.

*Not legal advice. Quotations are from the Code and the statute copy on the dates shown.*
