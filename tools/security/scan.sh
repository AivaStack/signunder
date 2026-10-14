#!/usr/bin/env bash
# Vulnerability scan with Trivy (pinned), as CI runs it.
#
#   tools/security/scan.sh deps          # lockfiles: fails on any HIGH or CRITICAL
#   tools/security/scan.sh image NAME    # an image: fails on HIGH or CRITICAL with a fix,
#                                        # reports those without one
#   tools/security/scan.sh sbom OUT      # CycloneDX SBOM of the lockfiles
#
# Accepted findings live in .trivyignore.yaml, each with a 90-day expiry.
set -euo pipefail
cd "$(dirname "$0")/../.."

trivy_image=aquasec/trivy:0.74.0
cache=${TRIVY_CACHE_DIR:-$HOME/.cache/trivy}
mkdir -p "$cache"
skip=(--skip-dirs '**/bin' --skip-dirs '**/obj' --skip-dirs '**/target' --skip-dirs '**/.venv'
      --skip-dirs web --skip-dirs tools/panel)
trivy() {
  docker run --rm -v "$PWD:/repo" -w /repo -v "$cache:/cache" \
    -v /var/run/docker.sock:/var/run/docker.sock "$trivy_image" --cache-dir /cache "$@"
}

case "${1:-}" in
  deps)
    trivy fs --scanners vuln --ignorefile .trivyignore.yaml "${skip[@]}" \
      --severity HIGH,CRITICAL --exit-code 1 .
    ;;
  image)
    name=${2:?image name}
    echo "Findings with no fix yet (reported, not failing):"
    trivy image --scanners vuln --ignorefile .trivyignore.yaml --severity HIGH,CRITICAL \
      --format table --quiet "$name" || true
    trivy image --scanners vuln --ignorefile .trivyignore.yaml --ignore-unfixed \
      --severity HIGH,CRITICAL --exit-code 1 "$name"
    ;;
  sbom)
    out=${2:?output file}
    trivy fs --format cyclonedx --output "$out" "${skip[@]}" .
    ;;
  *)
    sed -n '2,10p' "$0"
    exit 2
    ;;
esac
