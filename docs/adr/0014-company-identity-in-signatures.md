# ADR 0014 — The company's name in its signatures (CAWG identity assertion)

- **Status:** Accepted
- **Date:** 19 October 2026
- **Deciders:** Eoin

## Context

Until a company has its own C2PA certificate, its outputs are signed with SignUnder's certificate (ADR 0013), so a Content Credentials viewer names SignUnder, not the company. The point of the product is the company's own name on what it publishes. The Creator Assertions Working Group's identity assertion (CAWG identity 1.1) lets a second party, named in a credential of its own, put its name inside a manifest someone else signs. c2pa-rs 0.91.2 writes and validates one with an X.509 credential (`cawg.x509.cose`).

## Decision

- **A company proves its domain, and Eoin approves its name.** In the dashboard it enters its domain and publishes a TXT record `signunder-verification=<token>` there. `POST /v1/account/domain/check` looks the record up over DNS-over-HTTPS (Cloudflare's resolver, `Identity:DnsResolver`), since .NET has no TXT lookup. Eoin then checks that the company's name belongs to whoever controls the domain and runs `tools/approve-identity.py SLUG`. A new domain starts over; a renamed company stops being named until it is approved again.
- **The credential is an X.509 certificate from the SignUnder Identity CA**, a root of SignUnder's own whose key is a Cloud KMS HSM key. `identity-ca root` makes the root certificate (`deploy/identity-ca/root.pem`). `identity-ca issue` makes each company's certificate: subject O = the approved name, CN and DNS SAN = the domain; not a CA; key usage digital signature; extended key usages email protection and document signing (1.3.6.1.5.5.7.3.36), which c2pa-rs's certificate profile check accepts.
- **One identity-signing key for every company**, a second Cloud KMS HSM key (`IDENTITY_SIGNING_KEY`). Each company's certificate binds that key to its name; the sidecar checks that the certificate's public key is the key it signs with.
- **The assertion** has role `cawg.publisher` and references the hard binding (always, by c2pa-rs), `c2pa.actions` and `c2pa.actions.v2`. The sidecar wraps its usual signer (`WithIdentity`) rather than using c2pa-rs's `IdentityAssertionSigner`, which drops the time-stamp.
- **Wording:** "Published by Acme Ltd (acme.com, verified by SignUnder)". The registry keeps who was named on each record (`published_by`); the public record and the verify page show it, and the company lookup returns `verified_domain` while the identity is active.

## Consequences

- Validators report the assertion as well-formed (`cawg.identity.well-formed`) and the identity as untrusted (`cawg.x509.credential.untrusted`): no shared trust list for identity credentials includes our CA. Tested in the sidecar with the production certificate profile. Only the verify page is sure to show the name; whether TikTok, Meta, LinkedIn or Google display a CAWG identity is undocumented (SIGNUNDER.md §8, question 1).
- Two more HSM key versions at €2.20 a month each, about €4.40 a month (ADR 0011's price, read 15 Oct 2026), and one HSM signature per signed output for the assertion.
- **Created 19 Oct 2026:** HSM keys `identity-ca` and `identity-signing` (`EC_SIGN_P256_SHA256`, version 1 each) in key ring `signunder`, `europe-west3`. `signunder-signer` has `roles/cloudkms.signerVerifier` and `roles/cloudkms.publicKeyViewer` on `identity-signing` only; `identity-ca` has no key-level grants, so only project-level roles (Eoin's) reach it. The root (O=SignUnder, CN=SignUnder Identity CA, path length 0, to 2036) is `deploy/identity-ca/root.pem`. A test certificate issued through both keys chained to it and carried `identity-signing`'s public key. Staging migrated the same day (`TenantIdentity`).
- Tested end to end on staging on 20 Oct 2026 by Eoin with the test company VersionR: domain versionr.ai verified by its TXT record from the dashboard, name approved with `tools/approve-identity.py`, a JPEG signed with VersionR's key (record `01M5C6N72GXAKM76Q26TTH476C`). The manifest carried `cawg.identity` naming O=VersionR, CN=versionr.ai, role `cawg.publisher`, with `cawg.x509.signature.validated` and `cawg.identity.well-formed`; the failures were only `signingCredential.untrusted` (the test claim certificate) and `cawg.x509.credential.untrusted`. The DigiCert time-stamp, watermark and fingerprint were all present, and the verify page showed "Published by VersionR (versionr.ai, verified by SignUnder)".
- Approval is by hand, so Eoin is in the path of every company that wants its name shown; fine for the first customers. Since 20 Oct 2026 `.github/workflows/identity-reviews.yml` reads the view `registry.identity_awaiting_review` daily at 07:05 UTC and keeps one GitHub issue labelled `identity-review` listing who is waiting: opened when someone is, commented on when the list changes, closed when nobody is. It connects as `review_watch`, a role that may read only that view (secret `REVIEW_WATCH_DATABASE_URL`, through the Supabase pooler).
- A company's own C2PA certificate, when it has one, replaces this as the strongest form of its name; the identity assertion can stay alongside.

## Open

- Revocation: certificates are issued for a year (`--days`, default 365) and there is no revocation list yet.
- A CAWG trust list, if one appears, and getting the Identity CA onto it.
- Whether an identity claims aggregator (CAWG's other credential type) would be recognised more widely.
