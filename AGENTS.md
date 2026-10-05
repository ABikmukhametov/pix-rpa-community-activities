# Working on PIX RPA Community Activities

## Supported toolchain

- Use the .NET 10 SDK.
- Build against PIX Studio 3.2.1.
- Set `PIX_INSTALL_DIR` to the PIX installation directory, or pass
  `-p:PixInstallDir=<path>` to MSBuild. Do not commit a developer-specific path.

## Build commands

Build everything:

~~~powershell
dotnet build .\CommunityActivities.sln -c Release
~~~

Build one activity:

~~~powershell
dotnet build .\src\Activities.StringCommunity\Activities.StringCommunity.csproj -c Release
~~~

Compilation requires `Pix.Activities.dll` and, for some projects, assemblies
installed with PIX. NuGet dependencies are declared in the relevant project.
PIX assemblies must never be copied into the repository or release archive.

## Change policy

- Keep public activity names and PIX attributes stable unless the task requires
  a contract change.
- Critical failures must throw and reach PIX Studio as activity failures.
  Outputs must be cleared before work begins. Do not add an `Error` output that
  turns a failure into a successful activity result.
- Do not log or return tokens, passwords, authorization headers, request or
  response bodies, personal data, or full local paths.
- Do not call live APIs or send credentials during normal verification.
- Excel and TOTP are maintained without functional refactoring for the first
  public release.

## Release package

1. Build from a neutral checkout path with `Release`, `DebugType=None` and
   `DebugSymbols=false`.
2. Include only project activity DLLs and explicitly documented third-party
   runtime dependencies. Never include PIX DLLs.
3. Scan source files and artifacts for drive paths, user profiles, internal
   domains, work email addresses, secrets and real business data.
4. Recalculate `THIRD-PARTY-NOTICES.md` against the final archive.
5. Load the activities in PIX Studio 3.2.1 and run safe manual scenarios.
6. Generate a SHA-256 manifest and publish only that verified archive.

Every release must record that compilation alone does not establish PIX Studio
runtime compatibility.
