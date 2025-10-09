#!/usr/bin/env bash
set -euo pipefail
TRIPLE=${1:? "triple"}
RID=${2:? "rid"}
SONAME=${3:? "soname"}
OUTBASE=${4:? "outbase"}        # e.g., /artifacts
SDKROOT_DIR=${5:-}

SRCROOT=/src
LIBDIR=$(ls -d ${SRCROOT}/libpg_query-*)
BUILD=/tmp/build-${RID}
mkdir -p "$BUILD" "$OUTBASE"
cp -a "$LIBDIR"/. "$BUILD/"
cd "$BUILD"

export CC="zig cc -target ${TRIPLE}"
export AR="zig ar"
export RANLIB="zig ranlib"

EXTRA_CFLAGS="-fPIC"
EXTRA_LDFLAGS=""
MAKE_OS_ARG=""

# Windows: tell the Makefile to use its Win path & link winsock
if [[ "$TRIPLE" == *"windows"* ]]; then
  MAKE_OS_ARG="OS=Windows_NT"
  EXTRA_LDFLAGS+=" -lws2_32"
fi

# macOS: require SDK (passed from Docker stage)
if [[ "$TRIPLE" == *"macos"* ]]; then
  if [ -z "${SDKROOT_DIR:-}" ]; then
    echo "SDKROOT required for macOS"
    exit 1
  fi
  export SDKROOT="$SDKROOT_DIR"
fi

# Clean & build the static lib with the upstream Makefile
make clean || true
make -j"$(nproc)" ${MAKE_OS_ARG} CFLAGS="${EXTRA_CFLAGS}" build

# Produce a shared lib from the static archive
OUTDIR="$OUTBASE/${RID}/native"
mkdir -p "$OUTDIR"

if [[ "$SONAME" == *.so ]]; then
  zig cc -target "$TRIPLE" -shared -o "$OUTDIR/${SONAME}" \
    -Wl,--whole-archive libpg_query.a -Wl,--no-whole-archive \
    -lm -pthread ${EXTRA_LDFLAGS}
elif [[ "$SONAME" == *.dylib ]]; then
  SDKROOT="$SDKROOT_DIR" zig cc -target "$TRIPLE" -shared -o "$OUTDIR/${SONAME}" \
    -Wl,-install_name,@rpath/${SONAME} -Wl,-headerpad_max_install_names \
    -Wl,-all_load libpg_query.a ${EXTRA_LDFLAGS}
else # .dll
  zig cc -target "$TRIPLE" -shared -o "$OUTDIR/${SONAME}" \
    -Wl,--whole-archive libpg_query.a -Wl,--no-whole-archive \
    ${EXTRA_LDFLAGS}
fi

file "$OUTDIR/${SONAME}" || true
