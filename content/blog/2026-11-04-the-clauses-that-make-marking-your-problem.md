---
title: "Replicate, Together, Runware, Baseten: the marking clauses"
description: "Four inference platforms, four sets of terms, one allocation: output and its compliance are the customer's. The clauses, quoted, as read 12 Oct 2026."
slug: the-clauses-that-make-marking-your-problem
type: post
date: 2026-11-04
updated: 2026-11-04
reviewed: 2026-10-12
authors: [eoin]
tags: [platforms, article-50, replicate, together, runware, baseten]
sources:
  - title: "Replicate Terms of Service (1 April 2026)"
    url: https://replicate.com/terms
    read: 2026-10-12
  - title: "Together AI Terms of Service (19 May 2026)"
    url: https://www.together.ai/terms-of-service
    read: 2026-10-12
  - title: "Runware Terms of Service (29 September 2026)"
    url: https://runware.ai/terms
    read: 2026-10-12
  - title: "Baseten Terms and Conditions (15 September 2026)"
    url: https://www.baseten.co/terms-and-conditions/
    read: 2026-10-12
  - title: "European Commission, Guidelines on Article 50 (20 July 2026)"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
faq:
  - q: "Do Replicate, Together, Runware or Baseten mark AI-generated output?"
    a: "No. As read on 12 October 2026, none of the four documents a C2PA manifest or a watermark, and each one's terms place output and its lawful use on the customer."
  - q: "Which platform's terms are the most explicit?"
    a: "Baseten's. They say the customer assumes and shall perform all obligations imposed on the developer, provider, and/or deployer of AI systems, which are the AI Act's own role names."
related:
  - /platforms
  - /law/eu-ai-act-article-50
  - /guides/sign-under-your-own-name
draft: false
---

Replicate, Together, Runware and Baseten attach no manifest and no watermark to what they generate, and each one's terms say that output and its compliance belong to the customer. None of that is hidden. It is in the terms of service, and the four use four different ways of saying it. Here are the clauses, with section numbers, as read on 12 October 2026.

## What does each clause say?

**Replicate** (terms last updated 1 April 2026):

> "CUSTOMER IS ULTIMATELY RESPONSIBLE FOR ITS USE OF ANY MODELS OR OUTPUT" — Section 8.9, Artificial Intelligence

**Together AI** (19 May 2026):

> "You shall be responsible for all of Your Content and Output, including ensuring that it does not violate this Agreement." — Section 3.1, Your Content

**Runware** (29 September 2026):

> "Users are solely responsible for ensuring that their inputs and the use of AI-generated content comply" — Section 12, User Responsibility for Input and Ethical Use

**Baseten** (15 September 2026):

> "Customer assumes and shall perform all obligations imposed on the developer, provider, and/or deployer of AI systems" — Section 3.1, Customer's Responsibilities as Developer, Provider, and/or Deployer

The four run from general to exact. Replicate and Together speak of responsibility for output. Runware names "AI-generated content" and compliance. Baseten uses the roles the AI Act defines and hands their obligations over by name; a second clause, section 4.4(e), repeats it for third-party products: "Customer assumes and shall perform all obligations applicable to a deployer or provider under applicable law".

## What do the terms say about passing output off as human?

Three of the four prohibit it. Replicate's terms bar "representing or misleading people into believing that the use of the Services or outputs are human-generated" (Section 2.7(d)(x)(e)). Runware's bar users who "misrepresent the origin of Outputs" (Section 10). Baseten's require the customer to "not represent AI-generated content as human-generated" (Section 3.2).

These are rules against deception, not marking requirements. They do not tell you to attach anything. But they put you on the wrong side of your supplier's terms if unmarked output from your product is presented as human work, by you or by your users, and a mark you control is how you show it was not.

## Is this the platforms dodging the law?

No. It is the platforms reading the law the way the Commission does. The guidelines on Article 50, adopted on 20 July 2026, say that a company which "provides a generative or interactive AI application ... on the Union market under its own name or trademark" is "a provider responsible for compliance with the transparency obligations in Article 50(1) and/or (2)", regardless of who developed the system. The marking duty in Article 50(2) sits with that provider. An inference platform serving API calls to your product is your supplier, and its terms record that it does not intend to be more.

## Does an EU region change anything?

Runware and Baseten both document EU processing; Together offers it on dedicated endpoints only; Replicate states none (region pages read 8 October 2026). Residency answers a data-protection question. It does not mark an output.

## What this means if you ship on these platforms

Every file that comes back is unmarked, and by contract its compliance is yours. If your users are in the EU and your system was on the market before 2 August 2026, the marking grace ends on 2 December 2026; if you launched later, the duty already applies. If you route between several of these platforms, one mark in your own name across all of them is the only consistent record you will have.

Each platform has [its own page](/platforms) with the full clauses, dates and what to check. SignUnder's API signs output from any of them, and Replicate's directly in proxy mode, with a watermark and a fingerprint on record; it is in free early access ([get a key](/dashboard), [docs](/docs)). Until your company is listed it signs with SignUnder's test certificate, and under your own once it is.

*Not legal advice. Every quotation is from the platform's own terms on the date shown.*
