# Development certificates for internal mutual TLS — PUBLIC, local use only

Made by `../generate.sh` for the local stack (`deploy/docker-compose.yml`). The private
keys are committed on purpose so the local stack and tests run the TLS code path; they
protect nothing. Never use them, or this CA, outside local development. ADR 0010.

| File | Who presents it | Names |
|---|---|---|
| `ca.pem` | trusted by all three services | SignUnder internal services CA |
| `signunder-api.pem`, `.key` | the API, as a client | `signunder-api` |
| `sign-sidecar.pem`, `.key` | the sign sidecar, as a server | `sign-sidecar`, `localhost` |
| `wm-worker.pem`, `.key` | the watermark worker, as a server | `wm-worker`, `localhost` |
| `other-client.pem`, `.key` | tests only: a valid client certificate that is not the API's | `other-client` |

Regenerate with `EXTRA_CLIENTS=other-client ../generate.sh .` from this directory.
