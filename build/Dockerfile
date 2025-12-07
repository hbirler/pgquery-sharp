# syntax=docker/dockerfile:1.7
ARG PACKAGE_VERSION=0.1.0

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
 && ./scripts/build_target.sh aarch64-linux linux-arm64 libpg_query.so /artifacts

# ---------- build-win-x64 (GNU) ----------
FROM toolchain AS build-win-x64
COPY --from=src /src /src
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh x86_64-windows win-x64 pg_query.dll /artifacts

# ---------- build-win-arm64 (GNU) ----------
FROM toolchain AS build-win-arm64
COPY --from=src /src /src
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh aarch64-windows-gnu win-arm64 pg_query.dll /artifacts

# ---------- build-osx-arm64 (optional) ----------
FROM toolchain AS build-osx-arm64
COPY --from=src /src /src
#COPY --from=macos-sdk /opt/MacOSX.sdk /opt/MacOSX.sdk
#ENV SDKROOT=/opt/MacOSX.sdk
COPY build/scripts/build_target.sh ./scripts/build_target.sh
RUN chmod +x ./scripts/build_target.sh \
 && ./scripts/build_target.sh aarch64-macos osx-arm64 libpg_query.dylib /artifacts;

# ---------- pack ----------
FROM toolchain AS pack
ARG PACKAGE_VERSION
ENV PACKAGE_VERSION=${PACKAGE_VERSION}
# docs + license for packaging metadata
COPY README.md /work/README.md
COPY LICENSE /work/LICENSE
# project sources last, so native builds cache even if C# changes
COPY src/PostgresQuery /work/src/PostgresQuery
# generated files
COPY --from=gen /generated /work/src/PostgresQuery/Generated
# native artifacts aggregated
COPY --from=build-linux-x64  /artifacts /work/src/PostgresQuery/runtimes
COPY --from=build-linux-arm64 /artifacts /work/src/PostgresQuery/runtimes
COPY --from=build-win-x64    /artifacts /work/src/PostgresQuery/runtimes
COPY --from=build-win-arm64  /artifacts /work/src/PostgresQuery/runtimes
COPY --from=build-osx-arm64  /artifacts /work/src/PostgresQuery/runtimes
# license files from source stage
COPY --from=src /licenses /work/LICENSES

# only this script is copied here; changes only affect pack
COPY build/scripts/pack_nuget.sh ./scripts/pack_nuget.sh
RUN chmod +x ./scripts/pack_nuget.sh \
 && ./scripts/pack_nuget.sh /work/src/PostgresQuery /work/out
RUN cp /work/src/PostgresQuery/Generated/PgQuery.g.cs /work/out/PgQuery.g.cs

# ---------- out ----------
FROM scratch AS out
COPY --from=pack /work/out/ /
