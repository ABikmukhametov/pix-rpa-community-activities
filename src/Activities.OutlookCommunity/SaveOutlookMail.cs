using NetOffice.OutlookApi;
using NetOffice.OutlookApi.Enums;

namespace Activities.OutlookCommunity;

[BR.Core.Attributes.ScreenName("Сохранить письмо Outlook в *.msg")]
[BR.Core.Attributes.Representation("Сохранить письмо Outlook")]
[BR.Core.Attributes.Description("Сохраняет письмо Outlook в новый файл MSG.")]
[BR.Core.Attributes.Path(@"Custom activities.Outlook")]
[BR.Core.Attributes.Image(typeof(SaveOutlookMail), "Activities.OutlookCommunity.Icon.png")]
public class SaveOutlookMail : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Письмо:")]
    [BR.Core.Attributes.Description("Письмо для сохранения")]
    [BR.Core.Attributes.IsRequired]
    public required MailItem Mail { get; set; }

    [BR.Core.Attributes.ScreenName("Путь сохранения:")]
    [BR.Core.Attributes.Description("Полный путь к новому файлу с расширением .msg")]
    [BR.Core.Attributes.IsFilePathChooser]
    [BR.Core.Attributes.IsRequired]
    public required string FullPath { get; set; }

    public override void Execute(int? optionID)
    {
        ArgumentNullException.ThrowIfNull(Mail);
        if (string.IsNullOrWhiteSpace(FullPath))
            throw new ArgumentException("Путь сохранения не указан.", nameof(FullPath));

        string path = System.IO.Path.GetFullPath(FullPath.Trim());
        if (!string.Equals(System.IO.Path.GetExtension(path), ".msg", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Путь сохранения должен иметь расширение .msg.", nameof(FullPath));
        if (File.Exists(path))
            throw new IOException($"Файл уже существует: {path}");

        string? directory = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Не удалось определить каталог сохранения.", nameof(FullPath));
        Directory.CreateDirectory(directory);
        Mail.SaveAs(path, OlSaveAsType.olMSG);
    }
}