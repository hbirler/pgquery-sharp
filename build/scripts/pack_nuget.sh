#!/usr/bin/env bash
set -euo pipefail
PROJ=${1:?"project path"}
OUT=${2:?"out dir"}
mkdir -p "$OUT"
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet restore "$PROJ"
PACKAGE_VERSION=${PACKAGE_VERSION:-0.1.0}

echo "== contents to be packed =="
find "$PROJ/runtimes" -maxdepth 3 -type f -printf '%P\n' | sort || true

dotnet pack "$PROJ" -c Release -o "$OUT" \
  -p:PackageVersion="$PACKAGE_VERSION" \
  -p:RepositoryUrl="https://github.com/pganalyze/libpg_query" \
  -p:InformationalVersion="libpg_query:${LIBPG_TAG:-unknown}"