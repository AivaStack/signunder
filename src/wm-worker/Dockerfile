# Build context is the repository root: docker build -f src/wm-worker/Dockerfile .
FROM python:3.12-slim-trixie AS build
COPY --from=ghcr.io/astral-sh/uv:0.9 /uv /usr/local/bin/uv
# pdqhash has no wheel for every platform and then compiles from source.
RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential \
    && rm -rf /var/lib/apt/lists/*
ENV UV_COMPILE_BYTECODE=1 UV_LINK_MODE=copy
WORKDIR /app
COPY proto/ /proto/
COPY src/wm-worker/pyproject.toml src/wm-worker/uv.lock src/wm-worker/genproto.sh ./
COPY src/wm-worker/src/ src/
# Generate the gRPC code with the dev tools, then keep only the runtime dependencies.
RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync --frozen \
    && PROTO_DIR=/proto uv run --no-sync ./genproto.sh \
    && uv sync --frozen --no-dev
# Bake the TrustMark model weights into the image so start-up needs no network.
RUN .venv/bin/python -c "from wm_worker.marking import Marker; Marker()"

FROM python:3.12-slim-trixie
# Pick up any OS security updates released since the base image was built.
RUN apt-get update \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --system --no-create-home worker
COPY --from=build /app /app
ENV PATH="/app/.venv/bin:$PATH" PYTHONUNBUFFERED=1
EXPOSE 50052
USER worker
ENTRYPOINT ["wm-worker"]
