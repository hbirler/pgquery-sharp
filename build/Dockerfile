# syntax=docker/dockerfile:1.7

# ---------- toolchain: dotnet + protoc + zig ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS toolchain
ARG DEBIAN_FRONTEND=noninteractive
ARG ZIG_VERSION=0.15.1

RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential ca-certificates curl pkg-config make file \
    protobuf-compiler unzip xz-utils git \
  && rm -rf /var/lib/apt/lists/*

# Correct Zig URL (x86_64-linux)
RUN curl -sSLf https://ziglang.org/download/${ZIG_VERSION}/zig-x86_64-linux-${ZIG_VERSION}.tar.xz \
  | tar -xJ --strip-components=1 -C /usr/local \
  && ln -s /usr/local/zig /usr/local/bin/zig

WORKDIR /work

# ---------- src: fetch libpg_query tarball ----------
FROM toolchain AS src
ARG LIBPG_TAG=17-6.1.0
# copy only the script needed for this stage
COPY build/scripts/fetch_source.sh ./scripts/fetch_source.sh
RUN chmod +x ./scripts/fetch_source.sh && ./scripts/fetch_source.sh "$LIBPG_TAG"

# ---------- gen: run protoc ----------
FROM toolchain AS gen
COPY --from=src /src /src
COPY build/scripts/gen_protobuf.sh ./scripts/gen_protobuf.sh
RUN chmod +x ./scripts/gen_protobuf.sh && ./scripts/gen_protobuf.sh /src /generated

# ---------- build-linux-x64 ----------
FROM toolchain AS build-linux-x64
COPY --from=src /src /src
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh x86_64-linux-gnu linux-x64 libpg_query.so /artifacts

# ---------- build-linux-arm64 ----------
FROM toolchain AS build-linux-arm64
COPY --from=src /src /src
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh aarch64-linux-gnu linux-arm64 libpg_query.so /artifacts

# ---------- build-win-x64 (GNU) ----------
FROM toolchain AS build-win-x64
COPY --from=src /src /src
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh x86_64-windows-gnu win-x64 pg_query.dll /artifacts

# ---------- build-win-arm64 (GNU) ----------
FROM toolchain AS build-win-arm64
COPY --from=src /src /src
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh aarch64-windows-gnu win-arm64 pg_query.dll /artifacts

## ---------- macos-sdk (optional) ----------
## macOS SDK from local tarball (no network)
#FROM toolchain AS macos-sdk
## COPY only this file so changes elsewhere don't bust this stage cache
#COPY local/MacOSX*.sdk.tar.xz /tmp/MacOSX.sdk.tar.xz
#RUN mkdir -p /opt/MacOSX.sdk \
# && tar -xJf /tmp/MacOSX.sdk.tar.xz -C /opt/MacOSX.sdk --strip-components=1
#
## ---------- build-osx-arm64 (optional) ----------
#FROM toolchain AS build-osx-arm64
#COPY --from=src /src /src
#COPY --from=macos-sdk /opt/MacOSX.sdk /opt/MacOSX.sdk
#ENV SDKROOT=/opt/MacOSX.sdk
#COPY build/scripts/build_target.sh ./scripts/build_target.sh
#RUN if [ -d "$SDKROOT" ]; then \
#      chmod +x ./scripts/build_target.sh && \
#      ./scripts/build_target.sh aarch64-macos osx-arm64 libpg_query.dylib /artifacts "$SDKROOT"; \
#    else \
#      echo "Skipping osx-arm64 (no SDK)"; \
#    fi

# ---------- pack ----------
FROM toolchain AS pack
# project sources last, so native builds cache even if C# changes
COPY src/PgQuery.Protobuf /work/src/PgQuery.Protobuf
COPY LICENSES /work/LICENSES
# generated files
COPY --from=gen /generated /work/src/PgQuery.Protobuf/Generated
# native artifacts aggregated
COPY --from=build-linux-x64  /artifacts /work/src/PgQuery.Protobuf/runtimes
COPY --from=build-linux-arm64 /artifacts /work/src/PgQuery.Protobuf/runtimes
COPY --from=build-win-x64    /artifacts /work/src/PgQuery.Protobuf/runtimes
COPY --from=build-win-arm64  /artifacts /work/src/PgQuery.Protobuf/runtimes
#COPY --from=build-osx-arm64  /artifacts /work/src/PgQuery.Protobuf/runtimes
# license files from source stage
COPY --from=src /licenses /work/LICENSES

# only this script is copied here; changes only affect pack
COPY build/scripts/pack_nuget.sh ./scripts/pack_nuget.sh
RUN chmod +x ./scripts/pack_nuget.sh \
 && ./scripts/pack_nuget.sh /work/src/PgQuery.Protobuf /work/out

# ---------- out ----------
FROM scratch AS out
COPY --from=pack /work/out/ /
