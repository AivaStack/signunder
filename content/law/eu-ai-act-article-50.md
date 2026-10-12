---
title: "EU AI Act Article 50: who must mark AI content, by when, and how"
description: "Article 50(2) applies since 2 August 2026; systems on the market before then have until 2 December 2026. Who is the provider. Read 12 Oct 2026."
slug: eu-ai-act-article-50
type: law
date: 2026-10-12
updated: 2026-10-12
reviewed: 2026-10-12
authors: [eoin]
tags: [eu-ai-act, article-50, law, deadlines]
sources:
  - title: "Regulation (EU) 2024/1689 (the AI Act), Articles 50, 99 and 113, Official Journal text"
    url: https://eur-lex.europa.eu/eli/reg/2024/1689/oj
    read: 2026-10-12
  - title: "Regulation (EU) 2026/1744 of 8 July 2026 (the Digital Omnibus on AI), recital 38 and the new Article 111(4), OJ L of 24 July 2026"
    url: https://eur-lex.europa.eu/eli/reg/2026/1744/oj
    read: 2026-10-12
  - title: "European Commission, Guidelines on the implementation of the transparency obligations for certain AI systems under Article 50 of the AI Act (20 July 2026)"
    url: https://ec.europa.eu/newsroom/dae/redirection/document/131215
    read: 2026-10-12
  - title: "European Commission, FAQ: Transparency obligations under Article 50 AI Act"
    url: https://digital-strategy.ec.europa.eu/en/faqs/transparency-obligations-under-article-50-ai-act
    read: 2026-10-12
  - title: "Article 50 as reproduced by the AI Act Explorer (artificialintelligenceact.eu), used to read the text; EUR-Lex is the authoritative version"
    url: https://artificialintelligenceact.eu/article/50/
    read: 2026-10-12
faq:
  - q: "Does Article 50 apply yet?"
    a: "Yes. The transparency obligations in Article 50 apply from 2 August 2026 under Article 113. One exception: a provider whose generative system was already on the market before 2 August 2026 has until 2 December 2026 to comply with the marking duty in Article 50(2), under Article 111(4) as inserted by Regulation (EU) 2026/1744."
  - q: "I call an image model through someone else's API. Am I the provider?"
    a: "If you offer the generation to your users under your own name or trademark, yes. The Commission's guidelines of 20 July 2026 give exactly that example: a company providing a generative AI application on the Union market under its own name is the provider responsible for Article 50(2), regardless of whose model is behind it."
  - q: "What counts as machine-readable marking?"
    a: "The Act does not name a technology. Article 50(2) asks for marking in a machine-readable format that is detectable, and for solutions that are effective, interoperable, robust and reliable as far as technically feasible. Recital 133, as the guidelines restate, lists watermarks, metadata, cryptographic provenance methods, logging, fingerprints, and combinations of them. The Code of Practice expects signed metadata plus an imperceptible watermark."
  - q: "What is the fine?"
    a: "Under Article 99(4)(g), up to EUR 15 million or 3% of worldwide annual turnover, whichever is higher. For SMEs and start-ups, Article 99(6) applies whichever is lower."
  - q: "Has anyone been fined?"
    a: "Not that we can find. As of 12 October 2026 there is no public enforcement action or warning under Article 50 anywhere in the EU. Enforcement belongs to national market surveillance authorities; at least one Member State had not yet designated its authority as of late August 2026 (secondary source)."
related:
  - /law/eu-code-of-practice
  - /platforms
  - /guides/sign-under-your-own-name
  - /checker
draft: false
---

Article 50 of the EU AI Act requires AI-generated audio, image, video and text to be marked in a machine-readable, detectable way, and it has applied since 2 August 2026. The one relief is for generative systems already on the market before that date: their providers have until 2 December 2026 to comply with the marking duty. This page tracks the article, the dates, who it binds, and what enforcement looks like, from the primary texts, with the date each was read.

- Applies: **since 2 August 2026** (Article 113).
- Marking grace: **until 2 December 2026**, only for systems placed on the market before 2 August 2026, only for Article 50(2) (Article 111(4), inserted by the Digital Omnibus).
- Who is bound by 50(2): the **provider** of the generative system, which includes a business that offers generation under its own name using someone else's model through an API (Commission guidelines, 20 July 2026).
- Penalty ceiling: **EUR 15 million or 3% of worldwide turnover**, whichever is higher; for SMEs, whichever is lower (Article 99).
- Enforcement found as of 12 October 2026: **none**.

## What does Article 50 require?

Article 50 has four substantive duties, two on providers and two on deployers (Article 50, Regulation (EU) 2024/1689, read 12 October 2026):

1. **50(1), providers:** systems that interact directly with people must be designed so those people know they are dealing with an AI system, unless that is obvious.
2. **50(2), providers:** systems that generate synthetic audio, image, video or text must ensure the outputs "are marked in a machine-readable format and detectable as artificially generated or manipulated", with solutions that are "effective, interoperable, robust and reliable as far as this is technically feasible". Standard editing assistance and systems that do not substantially alter the input are excluded.
3. **50(3), deployers:** people exposed to emotion recognition or biometric categorisation must be told.
4. **50(4), deployers:** deepfakes must be disclosed as artificially generated or manipulated, and so must AI-generated text published to inform the public on matters of public interest, unless a person has taken editorial responsibility for it.

50(5) adds that the information must be clear and distinguishable and provided at the latest at first interaction or exposure. 50(7) asks the Commission to encourage codes of practice; the Digital Omnibus removed the Commission's power to turn such a code into binding common rules by implementing act, and its recital says those codes "do not grant a presumption of conformity" (Regulation (EU) 2026/1744, read 12 October 2026).

This page is about 50(2), the marking duty, because that is the one that lands on companies shipping generation. Disclosure under 50(1) and 50(4) is covered on the Code of Practice page.

## When does it apply?

Article 113 of the Act makes Article 50 applicable from 2 August 2026. The Digital Omnibus, Regulation (EU) 2026/1744 of 8 July 2026, published in the Official Journal on 24 July 2026, left Article 50 in place and added one transitional rule, a new Article 111(4):

> "Providers of AI systems, including general-purpose AI systems, generating synthetic audio, image, video or text content, that have been placed on the market before 2 August 2026 shall take the necessary steps in order to comply with Article 50(2) by 2 December 2026." — Regulation (EU) 2026/1744, Article 111(4) as inserted, read 12 October 2026

Recital 38 of the same regulation explains it as "a transitional period of four months for providers who have already placed their systems on the market before the 2 August 2026". The Commission's guidelines call it "a targeted grandfathering rule only with regard to the marking and detection obligations under Article 50(2)", and add that a system which is partly interactive and partly generative gets the extra time only for marking; its 50(1) disclosure duty applied from 2 August (Guidelines, 20 July 2026, read 12 October 2026).

So the dates are:

| Date | What |
|---|---|
| 2 August 2026 | Article 50 applies. Any generative system placed on the market from this date must mark from day one |
| 2 December 2026 | Marking grace ends for systems placed on the market before 2 August 2026 |
| 2 February 2027 | Code of Practice signatories must have an interoperable detection solution in place (Code, Measure 3.4; see the Code page) |

Content generated and published before 2 August 2026 does not have to be marked retroactively; the Commission's FAQ says so (read 12 October 2026).

## Who is the provider when the model is someone else's?

This is the question that matters for anyone building on fal, Replicate, Together or a self-hosted open model. The guidelines answer it with an example:

> "a company provides a generative or interactive AI application (e.g. a chatbot, image generator, AI agent) on the Union market under its own name or trademark to consumers, professional users or other legal entities ... The company is a provider responsible for compliance with the transparency obligations in Article 50(1) and/or (2) AI Act, regardless of whether the AI sys[tem was developed in house or by another party]" — Guidelines, section on providers, read 12 October 2026 (bracketed text is our completion of a line break in the PDF)

The same section says that a company which takes an existing generative system from another provider, modifies it and puts it into service under its own name becomes the provider of the new system. Providers of free and open-source components that are not themselves an AI system are not subject to Article 50, but the guidelines encourage them to design components so that downstream systems can comply.

Two consequences follow. First, the platform that served the request is not your provider for Article 50 purposes; every one of the eleven inference platforms we checked says the same from its side in its terms (see the [platform pages](/platforms)). Second, a manifest signed in your supplier's name is your supplier's mark, not yours; whether that satisfies your own duty is not something the guidelines answer, and no verifier displays your name from it.

## What does "marked in a machine-readable format" mean?

The Act is technology-neutral. The guidelines restate Recital 133's examples: "watermarks, metadata identifications, cryptographic methods for proving provenance and authenticity of content, logging methods, fingerprints or other techniques, and a combination of such techniques" (Guidelines, paragraph 73, read 12 October 2026). They also say that machine-readable markings "that are not perceivable by users at the point of interaction" do not satisfy the human-facing disclosure in 50(1), while being appropriate for 50(2).

The Code of Practice on Transparency of AI-Generated Content, which the Commission and the AI Board found adequate in July 2026, is where the technique becomes concrete: digitally signed metadata plus an imperceptible watermark, a detection solution, and non-removal of markings. The Code is voluntary, and the Commission's FAQ says that "not signing it does not constitute non-compliance with the AI Act"; a non-signatory must show compliance by other adequate means to its market surveillance authority. The [Code page](/law/eu-code-of-practice) goes through the measures.

## What is the penalty?

Article 99(4)(g) sets the ceiling for breaches of Article 50 at "up to EUR 15 000 000 or, if the offender is an undertaking, up to 3 % of its total worldwide annual turnover for the preceding financial year, whichever is higher". Article 99(6) turns that round for SMEs and start-ups: for them the fine is capped at whichever of the two is lower (Article 99, read 12 October 2026). Authorities can also order corrective measures.

## Who enforces it, and has anyone been fined?

Enforcement sits with the national market surveillance authorities of each Member State; the Commission's AI Office has a role only for systems built by providers of general-purpose AI models and for features of very large online platforms. The guidelines are non-binding, but authorities are expected to follow them.

As of 12 October 2026 we can find **no public enforcement action, fine or formal warning under Article 50 anywhere in the EU**. At least one Member State, Belgium, had not designated its competent authority as of late August 2026 (law-firm note; secondary, UNVERIFIED against the Commission's register). We check this monthly and publish the result as the enforcement watch; the honest reason to mark before 2 December is that the date is fixed and the work takes weeks, not that a fine is imminent.

## What this means if you ship AI generation

- **If your users are in the EU and your system was on the market before 2 August 2026**, you have until 2 December 2026 to mark every generated output machine-readably. After that, no relief exists.
- **If you launched on or after 2 August 2026**, the duty already applies.
- **If you route to inference platforms or self-host open models**, none of them marks in your name (one marks in its own), so the marking is yours to add after generation, consistently, whichever provider served the request.
- **If you want the Code's route**, that means signed metadata and a watermark on every output, a detection solution, and an interoperability mechanism by 2 February 2027.

SignUnder is built to do exactly that under your own name; it is pre-launch and the waitlist is on the homepage.

## Changes

- 2026-10-12 — First published. Reads of the Omnibus text (Article 111(4), recital 38), the guidelines PDF, Article 99 and the Commission FAQ; enforcement check found nothing.

*SignUnder is not a law firm and this is not legal advice. Every rule here cites its primary source and the date we read it, so your own counsel can check our reading rather than take our word for it.*
