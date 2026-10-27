# Build context is the repository root: docker build -f src/sign-sidecar/Dockerfile .
FROM rust:1.99-trixie AS build
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

# Distroless: glibc, libgcc and CA certificates only; no shell or package manager, so
# nothing else to patch. OpenSSL is linked into the binary (c2pa's vendored build).
# The development signer is not in this image; docker-compose mounts it.
FROM gcr.io/distroless/cc-debian13:nonroot
COPY --from=build /usr/local/bin/sign-sidecar /usr/local/bin/sign-sidecar
EXPOSE 50051
ENTRYPOINT ["/usr/local/bin/sign-sidecar"]
