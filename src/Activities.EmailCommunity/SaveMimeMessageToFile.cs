using MimeKit;

namespace Activities.EmailCommunity;

[BR.Core.Attributes.ScreenName("Сохранить электронное письмо в файл")]
[BR.Core.Attributes.Representation("Сохранить MimeMessage в файл EML")]
[BR.Core.Attributes.Description(
    "Сохраняет MimeMessage в EML, создаёт родительский каталог и по умолчанию не перезаписывает существующий файл.")]
[BR.Core.Attributes.Path("Custom activities.Email")]
[BR.Core.Attributes.Image(typeof(SaveMimeMessageToFile), "Activities.EmailCommunity.Icon.png")]
public class SaveMimeMessageToFile : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Письмо (MimeMessage):")]
    [BR.Core.Attributes.Description("Объект MimeMessage для сохранения")]
    [BR.Core.Attributes.IsRequired]
    public required MimeMessage MessageToSave { get; set; }

    [BR.Core.Attributes.ScreenName("Путь к файлу")]
    [BR.Core.Attributes.Description("Путь к файлу с расширением .eml")]
    [BR.Core.Attributes.IsRequired]
    [BR.Core.Attributes.IsFilePathChooser]
    public required string FilePath { get; set; } = "email_file.eml";

    [BR.Core.Attributes.ScreenName("Перезаписывать существующий файл")]
    public bool OverwriteExisting { get; set; }

    public override void Execute(int? optionID)
    {
        string fullPath = System.IO.Path.GetFullPath(FilePath);
        if (!string.Equals(System.IO.Path.GetExtension(fullPath), ".eml", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Файл письма должен иметь расширение .eml.", nameof(FilePath));
        }

        string? directory = System.IO.Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Не удалось определить каталог файла EML.");
        }
        Directory.CreateDirectory(directory);

        FileMode mode = OverwriteExisting ? FileMode.Create : FileMode.CreateNew;
        using FileStream stream = new(fullPath, mode, FileAccess.Write, FileShare.None);
        MessageToSave.WriteTo(stream);
    }
}
