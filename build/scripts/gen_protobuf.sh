#!/usr/bin/env bash
set -euo pipefail
SRCROOT=${1:?"/path/to/src"}
OUTDIR=${2:?"/path/to/generated"}
mkdir -p "$OUTDIR"
LIBDIR=$(ls -d ${SRCROOT}/libpg_query-*)
protoc -I "$LIBDIR/protobuf" --csharp_out="$OUTDIR" \
  --csharp_opt=file_extension=.g.cs \
  "$LIBDIR/protobuf/pg_query.proto"
