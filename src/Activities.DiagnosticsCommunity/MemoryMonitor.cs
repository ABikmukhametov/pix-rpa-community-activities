using System.Diagnostics;
using System.Management;

namespace Activities.DiagnosticsCommunity;

[BR.Core.Attributes.ScreenName("Получить нагрузку оперативной памяти")]
[BR.Core.Attributes.Representation("Получить использование оперативной памяти")]
[BR.Core.Attributes.Path("Custom activities.Diagnostics")]
[BR.Core.Attributes.Image(typeof(MemoryMonitor), "Activities.DiagnosticsCommunity.RAM.png")]
public class MemoryMonitor : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Использование памяти:")]
    [BR.Core.Attributes.IsOut]
    public string MemoryUsageState { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        MemoryUsageState = string.Empty;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Активность поддерживается только в Windows.");

        (float totalGb, float usedGb, float usagePercent) = GetMemoryUsage();
        MemoryUsageState = $"{usagePercent:0.00}% ({usedGb:0.00} GB / {totalGb:0.00} GB)";
    }

    private static (float TotalGB, float UsedGB, float UsagePercent) GetMemoryUsage()
    {
        ulong totalBytes = 0;
        using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
        using ManagementObjectCollection objects = searcher.Get();
        foreach (ManagementBaseObject item in objects)
        {
            using (item)
            {
                totalBytes = Convert.ToUInt64(item["TotalPhysicalMemory"]);
            }
        }
        if (totalBytes == 0)
        {
            throw new InvalidOperationException("WMI не вернул общий объём памяти.");
        }

        using var counter = new PerformanceCounter("Memory", "Available MBytes");
        float availableMb = counter.NextValue();
        float totalMb = totalBytes / (1024f * 1024f);
        float usedMb = Math.Max(0, totalMb - availableMb);
        return (totalMb / 1024f, usedMb / 1024f, usedMb / totalMb * 100f);
    }
}
