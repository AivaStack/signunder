COMPOSE := docker compose -f deploy/docker-compose.yml
SIDECAR := src/sign-sidecar/Cargo.toml

WORKER := src/wm-worker
PYSDK := src/signunder-py

.PHONY: up down logs build test test-kms test-gcp robustness fmt sample conformance scan rules watch watch-accept

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
	cd $(WORKER) && uv run ./genproto.sh && uv run pytest -q
	cd $(PYSDK) && uv run pytest -q

rules: ## Publish the rule packs and the checker's common answers into content/rules
	dotnet run --project tools/rules-export -- content/rules

watch: ## Check the rule packs' primary sources for changes since they were last accepted
	python3 tools/rules-watch/watch.py check

watch-accept: ## After reviewing a change: save what the sources say now as the new baseline
	python3 tools/rules-watch/watch.py accept

robustness: ## Watermark and fingerprint survival table, then its assertions
	cd $(WORKER) && uv run python ../../tests/robustness/robustness.py
	cd $(WORKER) && uv run pytest -q ../../tests/robustness

test-kms: ## Sidecar tests that sign through the local-kms emulator (needs Docker)
	$(COMPOSE) up -d --wait local-kms
	AWS_ENDPOINT_URL=http://localhost:4599 AWS_REGION=eu-central-1 \
	AWS_ACCESS_KEY_ID=local AWS_SECRET_ACCESS_KEY=local \
	cargo test --manifest-path $(SIDECAR) -- --ignored --skip google_cloud

test-gcp: ## Sign through a real Cloud KMS key: SIGNUNDER_TEST_GCP_KEY=projects/.../cryptoKeyVersions/1
	cargo test --manifest-path $(SIDECAR) -- --ignored google_cloud

fmt:
	dotnet format Signunder.sln
	cargo fmt --manifest-path $(SIDECAR)
	cd $(WORKER) && uv run ruff format . ../../tests/robustness
	cd $(PYSDK) && uv run ruff format .

sample: ## Sign the fixtures through the running stack into docs/conformance/samples
	tests/conformance/generate-samples.sh

conformance: ## Run the C2PA program's asset rubric over docs/conformance/samples
	tests/conformance/check-samples.sh

scan: ## Vulnerability scan of lockfiles and the three service images (Trivy)
	uv run --no-project -q --with pyyaml python tools/security/check-ignores.py
	tools/security/scan.sh deps
	for image in signunder-api signunder-sign-sidecar signunder-wm-worker; do tools/security/scan.sh image $$image:latest || exit 1; done
