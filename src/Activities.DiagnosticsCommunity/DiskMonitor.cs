namespace Activities.DiagnosticsCommunity;

[BR.Core.Attributes.ScreenName("Получить состояние дисков")]
[BR.Core.Attributes.Representation("Получить информацию о подключённых дисках")]
[BR.Core.Attributes.Description("Возвращает сведения о готовых дисках.")]
[BR.Core.Attributes.Path("Custom activities.Diagnostics")]
[BR.Core.Attributes.Image(typeof(DiskMonitor), "Activities.DiagnosticsCommunity.Disks.png")]
public class DiskMonitor : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Состояние дисков:")]
    [BR.Core.Attributes.IsOut]
    public string MemoryInfo { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        MemoryInfo = string.Empty;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Активность поддерживается только в Windows.");

        var output = new System.Text.StringBuilder();
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }
            output.AppendLine($"Диск: {drive.Name}")
                .AppendLine($"Метка: {drive.VolumeLabel}")
                .AppendLine($"Всего места: {drive.TotalSize / 1024d / 1024 / 1024:0.00} GB")
                .AppendLine($"Свободно: {drive.TotalFreeSpace / 1024d / 1024 / 1024:0.00} GB")
                .AppendLine($"Файловая система: {drive.DriveFormat}")
                .AppendLine();
        }
        MemoryInfo = output.ToString();
    }
}
