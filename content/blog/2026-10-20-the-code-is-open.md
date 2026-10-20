---
title: "Open-source C2PA signing: SignUnder's code is on GitHub"
description: "Since 20 October 2026 SignUnder's signing service is open source under Apache-2.0: the API, the C2PA signer, the watermark worker and the rule packs."
slug: the-code-is-open
type: post
date: 2026-10-20
updated: 2026-10-20
reviewed: 2026-10-20
authors: [eoin]
tags: [c2pa, open-source, self-hosting]
sources:
  - title: "AivaStack/signunder on GitHub"
    url: https://github.com/AivaStack/signunder
    read: 2026-10-20
  - title: "contentauth/c2pa-rs, the C2PA reference implementation in Rust"
    url: https://github.com/contentauth/c2pa-rs
    read: 2026-10-20
  - title: "adobe/trustmark"
    url: https://github.com/adobe/trustmark
    read: 2026-10-20
faq:
  - q: "Is SignUnder open source?"
    a: "Yes. Since 20 October 2026 the code of the service (API, C2PA signer, watermark and fingerprint worker, rules engine and rule packs, and the .NET and Python SDKs) is on GitHub at AivaStack/signunder under the Apache-2.0 licence."
  - q: "Can I run SignUnder myself?"
    a: "Yes. The repository includes a Docker Compose stack that runs the whole service locally on a development key. Output signed that way is valid but shows as unrecognised in Content Credentials viewers; recognised signing needs your key in a KMS and a certificate from a certificate authority on the C2PA trust list."
related:
  - /docs
  - /guides/sign-under-your-own-name
  - /checker
draft: false
---

SignUnder's code is public on GitHub at [AivaStack/signunder](https://github.com/AivaStack/signunder), under the Apache-2.0 licence, since 20 October 2026. It is the code the hosted service runs: the API, the C2PA signer, the watermark and fingerprint worker, the rules engine with its rule packs, and the .NET and Python SDKs.

## What is in the repository?

- **The API** (.NET 10): signing, verification, the registry of signed outputs, accounts and the public verify routes.
- **The signer** (Rust), a thin service around [c2pa-rs](https://github.com/contentauth/c2pa-rs), the C2PA reference implementation, pinned to an exact version so the manifests it writes do not drift. It signs through a cloud KMS, and can add a CAWG identity assertion naming the company that publishes the content.
- **The watermark and fingerprint worker** (Python): an invisible [TrustMark](https://github.com/adobe/trustmark) watermark bound into each manifest, and a PDQ perceptual hash, so an output can still be recognised after a platform strips its metadata.
- **The rules engine and eight rule packs**: EU Article 50, the Code of Practice, California, mainland China, South Korea, India, New York and the platforms' labelling rules. Every rule cites its primary source and the date it was read; the [free checker](/checker) runs on them.
- **The architecture and every technical decision**, as decision records, and the evidence prepared for the C2PA conformance program.

## Why open it?

A signature on AI-generated content is a claim about where the content came from, and the rules that decide which marks a company needs are a reading of the law. Both are easier to trust when anyone can read how they are made: what the manifest says, what the watermark carries, which source each rule rests on. Opening the code also lets a company check exactly what runs on its behalf, or run it itself.

## Can I run it myself?

Yes. `make up` starts the whole service in Docker, signing with a development key and certificate that are committed for the purpose; the repository's README has the steps. Content signed that way is valid but shows as unrecognised in Content Credentials viewers, because the development certificate is not on the C2PA trust list. Recognised signing in your own name needs two things the code cannot supply: your key in a KMS or HSM, and a certificate from a certificate authority on that list, issued once your product passes the C2PA conformance program. The [own-name guide](/guides/sign-under-your-own-name) explains why that name cannot be delegated.

## What does the hosted service add?

The keys held in an HSM, the conformance listing and certificate, the public verify page for each company, the identity checks behind the "verified by SignUnder" line, and rule packs kept current against their sources every week. The code is the same; the hosted service is the operation around it. [The docs](/docs) show how to start.
