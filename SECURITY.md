# Security

Please report a vulnerability privately, by email to **hello@signunder.com** with "Security" in the subject, rather than in a public issue. Say what you found, how to reproduce it, and what it affects; we reply within three working days.

In scope: the code in this repository, the API at `signunder-staging-api.fly.dev` (staging), and `signunder.com`. Please don't test against other customers' data or degrade the service for others.

How the service is secured is described in [`docs/conformance/security-architecture.md`](docs/conformance/security-architecture.md) and [`docs/conformance/owasp-review.md`](docs/conformance/owasp-review.md); dependency scanning is in [ADR 0009](docs/adr/0009-vulnerability-scanning.md).

The development keys and certificates committed under `deploy/internal-tls/dev/` and `src/sign-sidecar/dev-signer/` are public on purpose, for the local stack and tests (ADRs 0003 and 0010). Staging and production use their own, which are not in this repository.
