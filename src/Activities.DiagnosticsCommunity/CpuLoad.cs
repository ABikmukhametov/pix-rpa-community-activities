using System.Diagnostics;

namespace Activities.DiagnosticsCommunity;

[BR.Core.Attributes.ScreenName("Получить нагрузку процессора")]
[BR.Core.Attributes.Representation("Получить текущую нагрузку процессора")]
[BR.Core.Attributes.Path("Custom activities.Diagnostics")]
[BR.Core.Attributes.Image(typeof(CpuLoad), "Activities.DiagnosticsCommunity.CpuLoad.png")]
public class CpuLoad : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Нагрузка процессора:")]
    [BR.Core.Attributes.IsOut]
    public string CentralProcessState { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        CentralProcessState = string.Empty;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Активность поддерживается только в Windows.");

        using var counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        counter.NextValue();
        Thread.Sleep(1000);
        CentralProcessState = $"{counter.NextValue():0.00}%";
    }
}
