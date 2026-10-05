# PIX RPA Community Activities

Open-source custom activities for PIX RPA. The repository contains the source
projects and a shared solution; release binaries are published through GitHub
Releases.

## Compatibility

- .NET 10 SDK
- PIX Studio 3.2.1
- Windows for activities that use Office, Outlook, WMI, performance counters
  or other Windows APIs

A successful build confirms compilation only. Validate loading and behavior in
your PIX Studio environment before using the activities in production.

## Build

Set the PIX installation directory and build the solution:

~~~powershell
$env:PIX_INSTALL_DIR = 'C:\Program Files\PIX'
dotnet build .\CommunityActivities.sln -c Release
~~~

You can also pass the location without changing the environment:

~~~powershell
dotnet build .\src\Activities.StringCommunity\Activities.StringCommunity.csproj -c Release -p:PixInstallDir='C:\Program Files\PIX'
~~~

The PIX assemblies are required for compilation and are not redistributed by
this project. See [AGENTS.md](AGENTS.md) for the complete build and release
procedure.

## Install

Download a release archive, verify its SHA-256 manifest and copy the DLLs
from its `dll` directory to the location used by PIX Studio.

The archive intentionally contains only `Activities.*Community.dll` files,
`Quartz.dll` and `System.Diagnostics.PerformanceCounter.dll`. Quartz and
PerformanceCounter rely on compatible transitive dependencies supplied by the
PIX/.NET environment. Do not replace other PIX assemblies with DLLs restored
from NuGet, and do not copy `Pix.Activities.dll` from another installation.

## Activities

The solution contains activities for ChromeDriver maintenance, payment
classification, DaData, diagnostics, email, Excel, MIME types, GigaChat, logs,
OpenAI, Outlook, RabbitMQ, scheduling, strings, TOTP and Yandex Vision.

Network activities use the official public service APIs by default. Where an
endpoint is configurable, only HTTPS is accepted unless an activity explicitly
allows loopback HTTP for local RabbitMQ testing. Credentials are supplied by
the user at runtime.

## License

Source code is available under the [MIT License](LICENSE). Third-party
components retain their own licenses; see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Questions and bug reports: ARBikmuhametov@yandex.ru.
