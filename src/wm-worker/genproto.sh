#!/usr/bin/env bash
# Generates the gRPC code for proto/signunder/wm/v1/wm.proto into src/wm_worker/pb.
# Run with the dev dependencies: uv run ./genproto.sh
set -euo pipefail
cd "$(dirname "$0")"
proto=${PROTO_DIR:-../../proto}/signunder/wm/v1
out=src/wm_worker/pb
mkdir -p "$out"
python -m grpc_tools.protoc -I "$proto" --python_out="$out" --grpc_python_out="$out" "$proto/wm.proto"
# protoc writes an absolute import that only works outside a package.
sed -i.bak 's/^import wm_pb2 as/from . import wm_pb2 as/' "$out/wm_pb2_grpc.py" && rm "$out/wm_pb2_grpc.py.bak"
touch "$out/__init__.py"
