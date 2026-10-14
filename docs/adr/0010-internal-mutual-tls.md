# ADR 0010 — Mutual TLS between the API, the sidecar and the watermark worker

- **Status:** Accepted
- **Date:** 14 October 2026
- **Deciders:** Eoin

## Context

The C2PA Generator Product Security Requirements v0.2 (read 14 Oct 2026), at Assurance Level 1 for a Backend product:

- **O.2:** subsystems "SHALL be mutually authenticated; the role(s) of each subsystem across all communication channels SHALL be appropriate role(s), and SHALL be authenticated by the other".
- **O.5:** "Network communication channels between the subsystems … SHALL be protected using TLS v1.3 (or higher) or an equivalent protocol."

The API called the sign sidecar and the watermark worker over plain gRPC, relying on the private network alone.

## Decision

- **A private CA used for nothing else** issues three certificates: `signunder-api` (client authentication), `sign-sidecar` and `wm-worker` (server authentication, with their service names and `localhost`). ECDSA P-256, about 27 months' validity. `deploy/internal-tls/generate.sh` makes them and discards the CA's private key, so replacing a certificate means a new CA and a new set.
- **Servers** present their certificate, require a client certificate from the CA, and accept only one whose subject alternative name is `signunder-api`; any other is refused with `PERMISSION_DENIED`.
  - The sidecar does this with tonic's rustls TLS (`tls-ring`) and an interceptor on both its services.
  - The worker does it with Python gRPC's SSL server credentials (`require_client_auth`) and a check of the auth context in each method.
- **The API** uses a `SocketsHttpHandler` per gRPC client that presents its certificate, accepts only a server certificate chaining to the CA and matching the dialled host name, and enables only TLS 1.3.
- **Switching on.** TLS is used when a service's configured address is `https://`. The API refuses to start if such an address has no certificates configured. The sidecar refuses to start if only some of `SIDECAR_TLS_*` are set; the worker likewise for `WM_TLS_*`.
- **Development.** A public development set, plus an `other-client` certificate for tests, is committed in `deploy/internal-tls/dev/`. `deploy/docker-compose.yml` uses it, so the local stack runs the TLS path.
- **Production.** A set generated outside the repository, loaded as Fly.io secrets.
- **One cryptography provider.** rustls's ring provider is installed explicitly at sidecar start-up, because the AWS SDK's HTTPS client also compiles in aws-lc and rustls will not choose between them.

## Consequences

- Tested in both services: the API's certificate is accepted; no client certificate is refused; a server certificate offered as a client is refused at the handshake; a valid client certificate for another name is refused by the name check.
- Against the local stack: plain-text calls fail, and both services negotiate TLSv1.3 with the API's settings.
- **Limit:** the sidecar and worker would also accept TLS 1.2 from a client holding the API's certificate. Neither tonic's `ServerTlsConfig` nor Python gRPC's server credentials expose a minimum protocol version. Every connection the API makes is TLS 1.3. The security architecture document states this.
- No revocation: a leaked internal key is handled by regenerating the whole set.
- Scripts that call the sidecar directly, such as the sample generator, need the API's development certificate.

## Open

- Enforcing TLS 1.3 on the servers themselves: a custom rustls acceptor for the sidecar; for the worker, a TLS-terminating proxy or a gRPC option if one appears.
- Certificate expiry monitoring, and the production regeneration procedure.
