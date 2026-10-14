#!/usr/bin/env bash
# Runs the C2PA Conformance Program's own asset rubric over every sample report in
# docs/conformance/samples (except the deliberately tampered one) and fails on any
# failing trait. The rubric and its evaluator are fetched from c2pa-org/conformance-public
# at the pinned commit below, so a run is repeatable.
#
#   make conformance
#
# Needs uv, curl and network access.
set -euo pipefail
cd "$(dirname "$0")/../.."

# c2pa-org/conformance-public; Conformance Program v0.2, Asset Rubrics 1.0.
commit=${RUBRIC_COMMIT:-main}
spec=${SPEC_VERSION:-2.4}
rubric="asset-rubric-conformance0.2-spec${spec}.yml"
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

base="https://raw.githubusercontent.com/c2pa-org/conformance-public/${commit}/asset-rubrics"
curl -fsSL -o "$work/evaluator.py" "$base/c2pa_conformance_rubric_evaluator.py"
curl -fsSL -o "$work/rubric.yml" "$base/$rubric"

cat > "$work/summarise.py" <<'PY'
import json
import sys

text = sys.stdin.read()
result = json.loads(text[text.index("{"):])


def traits(outcome):
    block = result.get(outcome, {})
    groups = block.values() if isinstance(block, dict) else [block]
    return [item for group in groups for item in group]


failures = traits("false")
print("%d pass, %d fail, %d warn" % (len(traits("true")), len(failures), len(traits("warning"))))
for item in failures:
    print("    FAIL %s: %s" % (item["trait"], item["reportText"]))
sys.exit(1 if failures else 0)
PY

failed=0
for report in docs/conformance/samples/*.crjson; do
  case "$report" in *tampered*) continue ;; esac
  result=$(cd "$work" && uv run -q --no-project --with PyYAML \
    --with "git+https://github.com/adobe/json-formula-py.git" \
    python -I evaluator.py rubric.yml "$OLDPWD/$report" --spec-version "$spec" 2>/dev/null)
  summary=$(python3 "$work/summarise.py" <<<"$result") || failed=1
  echo "$(basename "$report"): $summary"
done
exit $failed
