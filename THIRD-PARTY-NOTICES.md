# Third-party notices

The release archive intentionally redistributes only the two third-party
assemblies listed below. Versions are taken from the generated `.deps.json`
files.

| Component | Version | License | Used by |
| --- | --- | --- | --- |
| Quartz | 3.14.0 | Apache-2.0 | Scheduler |
| System.Diagnostics.PerformanceCounter | 10.0.12 | MIT | Diagnostics |

`Quartz.dll` references `Microsoft.Extensions.Logging.Abstractions`, and
`System.Diagnostics.PerformanceCounter.dll` references
`System.Configuration.ConfigurationManager`. These transitive assemblies are
not redistributed to avoid replacing versions installed with PIX Studio.
The target PIX/.NET environment must provide compatible versions.

The source projects declare additional NuGet and platform dependencies for
building Email, Outlook, Diagnostics and other activities. Those assemblies
are not part of the release archive. In particular, do not copy restored
`MimeKit.dll`, NetOffice assemblies or other NuGet runtime files over the
libraries installed with PIX Studio.

`Pix.Activities.dll`, `Microsoft.Office.Interop.Excel.dll`, `NLog.dll`,
`Newtonsoft.Json.dll` and Office COM interop assemblies are also not
redistributed.

The source repository is licensed under MIT. Third-party components retain
their respective licenses.
