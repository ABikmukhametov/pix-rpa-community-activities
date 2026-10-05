using System.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security;
using System.Text.Json;
using BR.Core;

namespace Activities.YandexVisionCommunity;

[BR.Core.Attributes.ScreenName("Распознать текст (OCR)")]
[BR.Core.Attributes.Representation("Распознать одностраничный файл через Yandex Vision OCR")]
[BR.Core.Attributes.Description("Отправляет JPG, PNG или PDF в Yandex Vision OCR.")]
[BR.Core.Attributes.Path(@"Custom activities.Yandex Vision")]
[BR.Core.Attributes.Image(typeof(YandexVisionRecognizeByModel), "Activities.YandexVisionCommunity.YandexVision.png")]
public class YandexVisionRecognizeByModel : Activity
{
    private const int MaxFileBytes = 20 * 1024 * 1024;
    private const int MaxResponseBytes = 20 * 1024 * 1024;
    private static readonly Uri Endpoint = new("https://ocr.api.cloud.yandex.net/ocr/v1/recognizeText");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(90) };
    private static readonly IReadOnlyDictionary<string, string> MimeTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "JPEG",
            [".jpeg"] = "JPEG",
            [".png"] = "PNG",
            [".pdf"] = "PDF"
        };

    public enum Models
    {
        page,
        passport,
        handwritten,
        table,
        driver_license_front,
        driver_license_back,
        vehicle_registration_front,
        vehicle_registration_back,
        license_plates
    }

    [BR.Core.Attributes.ScreenName("IAM-токен (SecureString):")]
    [BR.Core.Attributes.Description("IAM-токен Yandex Cloud")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString IAMToken { get; set; }

    [BR.Core.Attributes.ScreenName("Folder ID:")]
    [BR.Core.Attributes.Description("Идентификатор каталога Yandex Cloud")]
    [BR.Core.Attributes.IsRequired]
    public required string FolderID { get; set; }

    [BR.Core.Attributes.ScreenName("Модель:")]
    [BR.Core.Attributes.Description("Модель распознавания Yandex Vision")]
    public Models Model { get; set; } = Models.page;

    [BR.Core.Attributes.ScreenName("Языки:")]
    [BR.Core.Attributes.Description("Коды языков через запятую; '*' включает автоматическое определение")]
    public string Languages { get; set; } = "ru,en";

    [BR.Core.Attributes.ScreenName("Одностраничный файл:")]
    [BR.Core.Attributes.Description("Файл JPG, JPEG, PNG или PDF")]
    [BR.Core.Attributes.IsFilePathChooser]
    [BR.Core.Attributes.IsRequired]
    public required string FilePath { get; set; }

    [BR.Core.Attributes.ScreenName("Текст:")]
    [BR.Core.Attributes.Description("Распознанный текст")]
    [BR.Core.Attributes.IsOut]
    public required string RecognizedText { get; set; }

    [BR.Core.Attributes.ScreenName("Таблицы:")]
    [BR.Core.Attributes.Description("Таблицы из ответа модели table")]
    [BR.Core.Attributes.IsOut]
    public List<DataTable> RecognizedTables { get; set; } = [];

    [BR.Core.Attributes.ScreenName("Сущности:")]
    [BR.Core.Attributes.Description("Сущности из ответа документных моделей")]
    [BR.Core.Attributes.IsOut]
    public Dictionary<string, string> Entities { get; set; } = new(StringComparer.Ordinal);

    [BR.Core.Attributes.ScreenName("Ответ сервиса (JSON):")]
    [BR.Core.Attributes.Description("Полный JSON-ответ")]
    [BR.Core.Attributes.IsOut]
    public JsonElement ServiceResponse { get; set; }

    public override void Execute(int? optionID)
    {
        RecognizedText = string.Empty;
        RecognizedTables = [];
        Entities = new(StringComparer.Ordinal);
        ServiceResponse = default;

        string fullPath = System.IO.Path.GetFullPath(
            string.IsNullOrWhiteSpace(FilePath)
                ? throw new ArgumentException("Путь к файлу не указан.")
                : FilePath);
        var file = new FileInfo(fullPath);
        if (!file.Exists)
            throw new FileNotFoundException("Файл для распознавания не найден.", fullPath);
        if (!MimeTypes.TryGetValue(file.Extension, out string? mimeType))
            throw new NotSupportedException($"Формат '{file.Extension}' не поддерживается.");
        if (file.Length == 0)
            throw new InvalidDataException("Файл для распознавания пуст.");
        if (file.Length > MaxFileBytes)
            throw new InvalidDataException("Размер файла превышает лимит активности 20 МБ.");
        if (string.IsNullOrWhiteSpace(FolderID))
            throw new ArgumentException("Folder ID не указан.", nameof(FolderID));

        string[] languageCodes = Languages.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (languageCodes.Length == 0)
            throw new ArgumentException("Не указан язык распознавания.", nameof(Languages));

        byte[] bytes = File.ReadAllBytes(fullPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Reveal(IAMToken));
        request.Headers.Add("x-folder-id", FolderID.Trim());
        request.Content = JsonContent.Create(new
        {
            mimeType,
            languageCodes,
            model = Model.ToString().Replace('_', '-'),
            content = Convert.ToBase64String(bytes)
        });

        using HttpResponseMessage response = Client.Send(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Yandex Vision вернул HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).",
                null,
                response.StatusCode);

        byte[] responseBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        if (responseBytes.Length > MaxResponseBytes)
            throw new InvalidDataException("Ответ Yandex Vision превышает 20 МБ.");

        using JsonDocument document = JsonDocument.Parse(responseBytes);
        ServiceResponse = document.RootElement.Clone();
        RecognizedText = ExtractRecognizedText(document.RootElement);
        RecognizedTables = ParseTables(document.RootElement);
        Entities = ParseEntities(document.RootElement);
    }

    private static string Reveal(SecureString value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new System.Net.NetworkCredential(string.Empty, value).Password;
    }

    private static bool TryGetTextAnnotation(JsonElement root, out JsonElement annotation)
    {
        annotation = default;
        return root.TryGetProperty("result", out JsonElement result)
            && result.TryGetProperty("textAnnotation", out annotation);
    }

    private static string ExtractRecognizedText(JsonElement root)
    {
        return TryGetTextAnnotation(root, out JsonElement annotation)
            && annotation.TryGetProperty("fullText", out JsonElement fullText)
            ? fullText.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    public static List<DataTable> ParseTables(JsonElement root)
    {
        var result = new List<DataTable>();
        if (!TryGetTextAnnotation(root, out JsonElement annotation)
            || !annotation.TryGetProperty("tables", out JsonElement tables)
            || tables.ValueKind != JsonValueKind.Array)
            return result;

        foreach (JsonElement tableElement in tables.EnumerateArray())
        {
            if (!tableElement.TryGetProperty("cells", out JsonElement cellsElement)
                || cellsElement.ValueKind != JsonValueKind.Array)
                continue;

            var cells = cellsElement.EnumerateArray()
                .Select(cell => new
                {
                    Row = ReadIndex(cell, "rowIndex"),
                    Column = ReadIndex(cell, "columnIndex"),
                    Text = cell.TryGetProperty("text", out JsonElement text) ? text.GetString() ?? string.Empty : string.Empty
                })
                .ToList();
            if (cells.Count == 0)
                continue;

            int rowCount = checked(cells.Max(cell => cell.Row) + 1);
            int columnCount = checked(cells.Max(cell => cell.Column) + 1);
            if (rowCount > 100_000 || columnCount > 10_000)
                throw new InvalidDataException("Размер таблицы в ответе Yandex Vision превышает допустимый.");

            var table = new DataTable();
            for (int column = 0; column < columnCount; column++)
                table.Columns.Add($"Column_{column}", typeof(string));
            for (int row = 0; row < rowCount; row++)
                table.Rows.Add(table.NewRow());
            foreach (var cell in cells)
                table.Rows[cell.Row][cell.Column] = cell.Text;
            result.Add(table);
        }

        return result;
    }

    private static int ReadIndex(JsonElement cell, string propertyName)
    {
        if (!cell.TryGetProperty(propertyName, out JsonElement value))
            throw new InvalidDataException($"В ячейке отсутствует {propertyName}.");
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number >= 0)
            return number;
        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), out number)
            && number >= 0)
            return number;
        throw new InvalidDataException($"Некорректный индекс {propertyName}.");
    }

    public static Dictionary<string, string> ParseEntities(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryGetTextAnnotation(root, out JsonElement annotation)
            || !annotation.TryGetProperty("entities", out JsonElement entities)
            || entities.ValueKind != JsonValueKind.Array)
            return result;

        foreach (JsonElement entity in entities.EnumerateArray())
        {
            string? name = entity.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;
            string? text = entity.TryGetProperty("text", out JsonElement textElement) ? textElement.GetString() : null;
            if (!string.IsNullOrWhiteSpace(name) && text is not null)
                result[name] = text;
        }
        return result;
    }
}