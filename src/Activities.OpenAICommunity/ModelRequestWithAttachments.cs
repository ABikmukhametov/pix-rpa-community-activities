using System.Security;
using System.Text.Json;

namespace Activities.OpenAICommunity;

[BR.Core.Attributes.ScreenName("Запрос на генерацию OpenAI")]
[BR.Core.Attributes.Representation("Запрос Chat Completions с изображениями и PDF")]
[BR.Core.Attributes.Description("Отправляет запрос в Chat Completions API с ограниченными по размеру вложениями.")]
[BR.Core.Attributes.Path("Custom activities.Модели.OpenAI")]
[BR.Core.Attributes.Image(typeof(GetModels), "Activities.OpenAICommunity.Icon.png")]
public class ModelRequestWithAttachments : BR.Core.Activity
{
    private const int MaxAttachments = 10;
    private const long MaxAttachmentBytes = 20L * 1024 * 1024;
    private const long MaxTotalAttachmentBytes = 50L * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> ImageMimeTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".bmp"] = "image/bmp"
        };

    [BR.Core.Attributes.ScreenName("Токен авторизации:")]
    [BR.Core.Attributes.Description("API-токен OpenAI или совместимого HTTPS-шлюза")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString Token { get; set; }

    [BR.Core.Attributes.ScreenName("Имя модели:")]
    public string Model { get; set; } = "gpt-4.1";

    [BR.Core.Attributes.ScreenName("Endpoint:")]
    [BR.Core.Attributes.Description("HTTPS endpoint Chat Completions API")]
    [BR.Core.Attributes.IsRequired]
    public required string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

    [BR.Core.Attributes.ScreenName("System prompt:")]
    [BR.Core.Attributes.IsRequired]
    public required string SystemPrompt { get; set; } = "Отвечай точно и кратко, соблюдая формат, указанный пользователем.";

    [BR.Core.Attributes.ScreenName("User prompt:")]
    [BR.Core.Attributes.IsRequired]
    public required string UserPrompt { get; set; } = "Опиши переданные данные.";

    [BR.Core.Attributes.ScreenName("Вложения (пути к файлам):")]
    public List<string> Attachments { get; set; } = new();

    [BR.Core.Attributes.ScreenName("Temperature:")]
    public double Temperature { get; set; } = 1.0;

    [BR.Core.Attributes.ScreenName("Max tokens:")]
    public int MaxTokens { get; set; } = 3000;

    [BR.Core.Attributes.ScreenName("Ответ модели:")]
    [BR.Core.Attributes.IsOut]
    public string? ModelAnswer { get; set; }

    [BR.Core.Attributes.ScreenName("Списано токенов:")]
    [BR.Core.Attributes.IsOut]
    public int? TotalTokens { get; set; }

    [BR.Core.Attributes.ScreenName("Ответ сервиса (JSON):")]
    [BR.Core.Attributes.IsOut]
    public JsonElement ServiceResponse { get; set; }

    public override void Execute(int? optionID = null)
    {
        ModelAnswer = null;
        TotalTokens = null;
        ServiceResponse = default;
        ValidateInputs();

        var userContent = new List<object>
        {
            new { type = "text", text = UserPrompt }
        };
        long totalBytes = 0;
        foreach (string filePath in Attachments)
        {
            string fullPath = System.IO.Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Вложение не найдено.", fullPath);
            }

            FileInfo file = new(fullPath);
            if (file.Length <= 0 || file.Length > MaxAttachmentBytes)
            {
                throw new InvalidDataException("Размер вложения выходит за допустимые пределы.");
            }
            totalBytes += file.Length;
            if (totalBytes > MaxTotalAttachmentBytes)
            {
                throw new InvalidDataException("Суммарный размер вложений превышает допустимый предел.");
            }

            string extension = file.Extension;
            string base64 = Convert.ToBase64String(File.ReadAllBytes(fullPath));
            if (ImageMimeTypes.TryGetValue(extension, out string? imageMime))
            {
                userContent.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:{imageMime};base64,{base64}" }
                });
            }
            else if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                userContent.Add(new
                {
                    type = "file",
                    file = new
                    {
                        filename = System.IO.Path.GetFileName(fullPath),
                        file_data = $"data:application/pdf;base64,{base64}"
                    }
                });
            }
            else
            {
                throw new NotSupportedException("Формат вложения не поддерживается.");
            }
        }

        object payload = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userContent }
            },
            temperature = Temperature,
            max_tokens = MaxTokens
        };

        using JsonDocument document = OpenAiHttp.Send(HttpMethod.Post, Endpoint, Token, payload);
        ServiceResponse = document.RootElement.Clone();
        if (document.RootElement.TryGetProperty("usage", out JsonElement usage) &&
            usage.TryGetProperty("total_tokens", out JsonElement tokens) &&
            tokens.TryGetInt32(out int totalTokens))
        {
            TotalTokens = totalTokens;
        }

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0 ||
            !choices[0].TryGetProperty("message", out JsonElement message) ||
            !message.TryGetProperty("content", out JsonElement content) ||
            content.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException("Ответ OpenAI не содержит текст модели.");
        }
        ModelAnswer = content.GetString();
    }

    private void ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new ArgumentException("Имя модели не указано.", nameof(Model));
        }
        if (string.IsNullOrWhiteSpace(SystemPrompt) || string.IsNullOrWhiteSpace(UserPrompt))
        {
            throw new ArgumentException("System prompt и User prompt обязательны.");
        }
        if (Temperature is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(Temperature), "Temperature должна быть от 0 до 2.");
        }
        if (MaxTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxTokens), "Max tokens должен быть положительным.");
        }
        if (Attachments.Count > MaxAttachments)
        {
            throw new ArgumentException($"Допускается не более {MaxAttachments} вложений.", nameof(Attachments));
        }
    }
}
