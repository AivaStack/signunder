# Build context is the repository root: docker build -f src/sign-sidecar/Dockerfile .
FROM rust:1.99-bookworm AS build
RUN apt-get update \
    && apt-get install -y --no-install-recommends protobuf-compiler \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /repo
COPY proto/ proto/
COPY src/sign-sidecar/ src/sign-sidecar/
RUN --mount=type=cache,target=/usr/local/cargo/registry \
    --mount=type=cache,target=/repo/src/sign-sidecar/target \
    cargo build --release --locked --manifest-path src/sign-sidecar/Cargo.toml \
    && cp src/sign-sidecar/target/release/sign-sidecar /usr/local/bin/sign-sidecar

# The development signer is not in this image; docker-compose mounts it.
FROM debian:bookworm-slim
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --system --no-create-home sidecar
COPY --from=build /usr/local/bin/sign-sidecar /usr/local/bin/sign-sidecar
EXPOSE 50051
USER sidecar
ENTRYPOINT ["sign-sidecar"]
