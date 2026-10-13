COMPOSE := docker compose -f deploy/docker-compose.yml
SIDECAR := src/sign-sidecar/Cargo.toml

.PHONY: up down logs build test test-kms fmt sample

up: ## Build and start the local stack
	$(COMPOSE) up --build -d --wait

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f

build: ## Native build (needs the Brewfile toolchains)
	dotnet build Signunder.sln
	cargo build --manifest-path $(SIDECAR)

test:
	dotnet test Signunder.sln
	cargo test --manifest-path $(SIDECAR)

test-kms: ## Sidecar tests that sign through the local-kms emulator (needs Docker)
	$(COMPOSE) up -d --wait local-kms
	AWS_ENDPOINT_URL=http://localhost:4599 AWS_REGION=eu-central-1 \
	AWS_ACCESS_KEY_ID=local AWS_SECRET_ACCESS_KEY=local \
	cargo test --manifest-path $(SIDECAR) -- --ignored

fmt:
	dotnet format Signunder.sln
	cargo fmt --manifest-path $(SIDECAR)

sample: ## Sign the fixtures through the running stack into docs/conformance/samples
	tests/conformance/generate-samples.sh
