#!/usr/bin/env bash
set -euo pipefail
TAG=${1:?"usage: fetch_source.sh <libpg_query_tag>"}
mkdir -p /src /licenses
cd /src
curl -sSLf -o libpg_query.tar.gz "https://github.com/pganalyze/libpg_query/archive/refs/tags/${TAG}.tar.gz"
tar -xzf libpg_query.tar.gz
rm libpg_query.tar.gz
# capture license files for later packaging
LIBDIR=$(echo libpg_query-*)
if [ -f "/src/${LIBDIR}/LICENSE" ]; then cp "/src/${LIBDIR}/LICENSE" /licenses/LICENSE.libpg_query || true; fi
if [ -f "/src/${LIBDIR}/src/postgres/COPYRIGHT" ]; then cp "/src/${LIBDIR}/src/postgres/COPYRIGHT" /licenses/LICENSE.POSTGRESQL || true; fi
