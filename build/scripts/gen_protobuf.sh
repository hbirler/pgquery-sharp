#!/usr/bin/env bash
set -euo pipefail
SRCROOT=${1:?"/path/to/src"}
OUTDIR=${2:?"/path/to/generated"}
mkdir -p "$OUTDIR"
LIBDIR=$(ls -d ${SRCROOT}/libpg_query-*)
# Add line `option csharp_namespace = "PostgresQuery"` before the first package line
sed -i.bak '/^package /i option csharp_namespace = "PostgresQuery";' "$LIBDIR/protobuf/pg_query.proto"
# Generate C# code from pg_query.proto
protoc -I "$LIBDIR/protobuf" --csharp_out="$OUTDIR" \
  --csharp_opt=file_extension=.g.cs \
  "$LIBDIR/protobuf/pg_query.proto"
