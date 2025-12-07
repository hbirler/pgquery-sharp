#!/usr/bin/env bash
set -euo pipefail

PACKAGE_VERSION=${PACKAGE_VERSION:-0.1.1}

echo "Building PostgresQuery package (PACKAGE_VERSION=${PACKAGE_VERSION})..."
DOCKER_BUILDKIT=1 docker build -f build/Dockerfile \
  --target out \
  --output type=local,dest=./out \
  --build-arg PACKAGE_VERSION="${PACKAGE_VERSION}" .

echo "Restoring test project against built package..."
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet restore PostgresQuery.Tests/PostgresQuery.Tests.csproj --nologo \
  --configfile PostgresQuery.Tests/nuget.config \
  -p:PostgresQueryPackageVersion="${PACKAGE_VERSION}"

echo "Running tests against built package..."
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test PostgresQuery.Tests/PostgresQuery.Tests.csproj --nologo --no-restore \
  -p:PostgresQueryPackageVersion="${PACKAGE_VERSION}"
