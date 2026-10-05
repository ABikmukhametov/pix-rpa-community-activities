using System.Text.Json;
using BR.Core;

namespace Activities.GigaChatCommunity;

[BR.Core.Attributes.ScreenName("Загрузить файл GigaChat")]
[BR.Core.Attributes.Representation("Загрузить файл GigaChat")]
[BR.Core.Attributes.Description("Потоково загружает поддерживаемый файл в хранилище GigaChat.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(UploadFile), "Activities.GigaChatCommunity.GigaChat.png")]
public class UploadFile : Activity
{
    private static readonly IReadOnlyDictionary<string, (string MimeType, long MaxBytes)> Formats =
        new Dictionary<string, (string, long)>(StringComparer.OrdinalIgnoreCase)
        {
            [".txt"] = ("text/plain", 40L * 1024 * 1024),
            [".doc"] = ("application/msword", 40L * 1024 * 1024),
            [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", 40L * 1024 * 1024),
            [".pdf"] = ("application/pdf", 40L * 1024 * 1024),
            [".epub"] = ("application/epub+zip", 40L * 1024 * 1024),
            [".ppt"] = ("application/vnd.ms-powerpoint", 40L * 1024 * 1024),
            [".pptx"] = ("application/vnd.openxmlformats-officedocument.presentationml.presentation", 40L * 1024 * 1024),
            [".xlsx"] = ("application/vnd.ms-excel", 40L * 1024 * 1024),
            [".png"] = ("image/png", 15L * 1024 * 1024),
            [".jpg"] = ("image/jpeg", 15L * 1024 * 1024),
            [".jpeg"] = ("image/jpeg", 15L * 1024 * 1024),
            [".tiff"] = ("image/tiff", 15L * 1024 * 1024),
            [".bmp"] = ("image/bmp", 15L * 1024 * 1024)
        };

    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsRequired]
    public required System.Security.SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("Файл:")]
    [BR.Core.Attributes.Description("Путь к поддерживаемому файлу")]
    [BR.Core.Attributes.IsFilePathChooser]
    [BR.Core.Attributes.IsRequired]
    public required string FilePath { get; set; }

    [BR.Core.Attributes.ScreenName("ID файла:")]
    [BR.Core.Attributes.Description("Идентификатор загруженного файла")]
    [BR.Core.Attributes.IsOut]
    public required string FileId { get; set; }

    [BR.Core.Attributes.ScreenName("Ответ сервиса:")]
    [BR.Core.Attributes.Description("Полный JSON-ответ")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        FileId = string.Empty;
        Response = default;
        string fullPath = System.IO.Path.GetFullPath(
            string.IsNullOrWhiteSpace(FilePath)
                ? throw new ArgumentException("Путь к файлу не указан.")
                : FilePath);
        var file = new FileInfo(fullPath);
        if (!file.Exists)
            throw new FileNotFoundException("Файл для загрузки не найден.", fullPath);

        string extension = file.Extension;
        if (!Formats.TryGetValue(extension, out var format))
            throw new NotSupportedException($"Формат '{extension}' не поддерживается активностью.");
        if (file.Length == 0)
            throw new InvalidDataException("Нельзя загрузить пустой файл.");
        if (file.Length > format.MaxBytes)
            throw new InvalidDataException($"Размер файла превышает лимит {format.MaxBytes / 1024 / 1024} МБ.");

        Response = GigaChatHttp.Upload(AccessToken, fullPath, format.MimeType);
        if (!Response.TryGetProperty("id", out JsonElement id) || string.IsNullOrWhiteSpace(id.GetString()))
            throw new InvalidDataException("В ответе GigaChat отсутствует идентификатор файла.");
        FileId = id.GetString()!;
    }
}