#!/usr/bin/env bash
# Signs the sidecar's fixture images through the running local stack and saves each
# signed file with its crJSON validation report in docs/conformance/samples.
#
#   SIGNUNDER_TSA_URL=http://timestamp.digicert.com make up && make sample
#
# Needs the stack from `make up`, curl, python3 and Docker (for grpcurl).
set -euo pipefail
cd "$(dirname "$0")/../.."

api=${SIGNUNDER_API:-http://localhost:8080}
sidecar=${SIGNUNDER_SIDECAR:-host.docker.internal:50051}
# The public development key the API seeds for the demo tenant (appsettings.Development.json).
api_key=${SIGNUNDER_API_KEY:-su_test_local_development_only_public_key_0000}
fixtures=src/sign-sidecar/tests/fixtures
out=docs/conformance/samples
mkdir -p "$out"

# validate <file> <mime type>: the sidecar's Validate RPC, report on stdout.
validate() {
  python3 -c 'import base64, json, sys
print(json.dumps({"asset": base64.b64encode(open(sys.argv[1], "rb").read()).decode(), "mime_type": sys.argv[2]}))' "$1" "$2" \
    | docker run --rm -i -v "$PWD/proto:/proto:ro" fullstorydev/grpcurl:v1.9.3 \
        -plaintext -max-msg-sz 67108864 -import-path /proto -proto signunder/sign/v1/sign.proto \
        -d @ "$sidecar" signunder.sign.v1.SignService/Validate \
    | python3 -c 'import json, sys
print(json.dumps(json.loads(json.load(sys.stdin)["validationReportJson"]), indent=2))'
}

for sample in "sample.jpg image/jpeg" "sample.png image/png"; do
  read -r name mime <<<"$sample"
  curl --fail-with-body -sS -o "$out/$name" -H "Authorization: Bearer $api_key" \
    -F "file=@$fixtures/$name;type=$mime" "$api/v1/sign"
  validate "$out/$name" "$mime" > "$out/$name.crjson"
  echo "wrote $out/$name and $out/$name.crjson"
done

# Evidence that the hard binding does its job: one flipped byte of image data.
python3 -c 'import sys
data = bytearray(open(sys.argv[1], "rb").read())
data[-16] ^= 0xFF
open(sys.argv[2], "wb").write(data)' "$out/sample.jpg" "$out/sample.tampered.jpg"
validate "$out/sample.tampered.jpg" image/jpeg > "$out/sample.tampered.jpg.crjson"
echo "wrote $out/sample.tampered.jpg and $out/sample.tampered.jpg.crjson"
