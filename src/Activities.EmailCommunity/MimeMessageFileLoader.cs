using MimeKit;

namespace Activities.EmailCommunity;

[BR.Core.Attributes.ScreenName("Загрузить электронное письмо из файла")]
[BR.Core.Attributes.Representation("Загрузить MimeMessage из файла EML")]
[BR.Core.Attributes.Description("Загружает письмо из существующего EML-файла.")]
[BR.Core.Attributes.Path("Custom activities.Email")]
[BR.Core.Attributes.Image(typeof(LoadMimeMessageFromFile), "Activities.EmailCommunity.Icon.png")]
public class LoadMimeMessageFromFile : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Путь к файлу")]
    [BR.Core.Attributes.Description("Путь к существующему файлу .eml")]
    [BR.Core.Attributes.IsRequired]
    [BR.Core.Attributes.IsFilePathChooser]
    public required string FilePath { get; set; } = "email_file.eml";

    [BR.Core.Attributes.ScreenName("Письмо (MimeMessage):")]
    [BR.Core.Attributes.Description("Загруженный MimeMessage")]
    [BR.Core.Attributes.IsOut]
    public MimeMessage? LoadedMessage { get; set; }

    public override void Execute(int? optionID)
    {
        LoadedMessage = null;
        string fullPath = System.IO.Path.GetFullPath(FilePath);
        if (!string.Equals(System.IO.Path.GetExtension(fullPath), ".eml", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Файл письма должен иметь расширение .eml.", nameof(FilePath));
        }
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Файл EML не найден.", fullPath);
        }

        using FileStream stream = File.OpenRead(fullPath);
        LoadedMessage = MimeMessage.Load(stream);
    }
}
