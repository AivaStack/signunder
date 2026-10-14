#!/usr/bin/env bash
# Makes a private CA and the certificates for mutual TLS between SignUnder's services:
# the API (client) and the sign sidecar and watermark worker (servers). ADR 0010.
#
#   deploy/internal-tls/generate.sh OUT_DIR [extra server name ...]
#
# EXTRA_CLIENTS="name ..." also issues client certificates with other names, which the
# development set uses to test that the services refuse callers other than the API.
#
# deploy/internal-tls/dev/ holds a committed, public set for the local stack. For
# production, write to a directory outside the repository and load the files as secrets;
# the CA's private key is then deleted, so a lost certificate means a new CA.
# Needs OpenSSL 3.
set -euo pipefail
out=${1:?output directory}
shift
extra=("$@")
mkdir -p "$out"
cd "$out"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

days=825
key() { openssl ecparam -name prime256v1 -genkey -noout | openssl pkcs8 -topk8 -nocrypt -out "$1"; }

key "$tmp/ca.key"
openssl req -new -x509 -key "$tmp/ca.key" -sha256 -days "$days" \
  -subj "/O=SignUnder/CN=SignUnder internal services CA" \
  -addext "basicConstraints=critical,CA:TRUE,pathlen:0" \
  -addext "keyUsage=critical,keyCertSign,cRLSign" -out ca.pem

# issue NAME USAGE SAN...
issue() {
  local name=$1 usage=$2
  shift 2
  local san
  san=$(printf 'DNS:%s,' "$@")
  key "$name.key"
  openssl req -new -key "$name.key" -subj "/O=SignUnder/CN=$name" -out "$tmp/$name.csr"
  openssl x509 -req -in "$tmp/$name.csr" -CA ca.pem -CAkey "$tmp/ca.key" -CAcreateserial \
    -sha256 -days "$days" -out "$name.pem" -extfile <(printf '%s\n' \
      "basicConstraints=critical,CA:FALSE" \
      "keyUsage=critical,digitalSignature" \
      "extendedKeyUsage=$usage" \
      "subjectAltName=${san%,}")
}

issue signunder-api clientAuth signunder-api
for client in ${EXTRA_CLIENTS:-}; do
  issue "$client" clientAuth "$client"
done
issue sign-sidecar serverAuth sign-sidecar localhost ${extra[@]+"${extra[@]}"}
issue wm-worker serverAuth wm-worker localhost ${extra[@]+"${extra[@]}"}
rm -f ca.srl
echo "wrote ca.pem and signunder-api, sign-sidecar, wm-worker (.pem, .key) in $out"
