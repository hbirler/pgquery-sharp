# PostgresQuery

Lightweight .NET wrapper around `libpg_query`.
Prebuilt native binaries for Linux, Windows, and macOS are included in the NuGet package.
Windows and macOS builds have not been tested, so be warned.

GitHub repository for the original parser: [libpg_query](https://github.com/pganalyze/libpg_query)

## Install

```bash
dotnet add package PostgresQuery
```

## Quick start

```csharp
using PostgresQuery;

var ast = Parser.Parse("SELECT 1 AS value");
Console.WriteLine($"Postgres version: {ast.Version}");

var normalized = Parser.Normalize(" select  1 /* comment */ ");
var fingerprint = Parser.Fingerprint("select 1");
var statements = Parser.Split("select 1; select 2;");
```

## Building and testing

The repository ships with a Docker-based build that produces the NuGet package and runs the consumer tests against it:

```bash
./run.sh            # builds into ./out and runs PostgresQuery.Tests
PACKAGE_VERSION=0.1.0 ./run.sh   # override package version
```

Generated protobuf bindings and NuGet package are produced inside the Docker build and emitted under `out/`.

## License

PostgreSQL server source code, used under the PostgreSQL license.
Portions Copyright (c) 1996-2023, The PostgreSQL Global Development Group
Portions Copyright (c) 1994, The Regents of the University of California

The libpg_query library is licensed under the 3-clause BSD license.
All other parts are licensed under the 3-clause BSD license, see LICENSE file for details.
