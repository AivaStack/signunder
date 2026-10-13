COMPOSE := docker compose -f deploy/docker-compose.yml
SIDECAR := src/sign-sidecar/Cargo.toml

.PHONY: up down logs build test fmt sample

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

fmt:
	dotnet format Signunder.sln
	cargo fmt --manifest-path $(SIDECAR)

sample: ## Sign the fixtures through the running stack into docs/conformance/samples
	tests/conformance/generate-samples.sh
