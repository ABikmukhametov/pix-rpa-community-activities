using System.Text.Json;
using System.Text.Json.Serialization;
using BR.Core;

namespace Activities.GigaChatCommunity;

[BR.Core.Attributes.ScreenName("Запрос на генерацию GigaChat")]
[BR.Core.Attributes.Representation("Запрос на генерацию GigaChat")]
[BR.Core.Attributes.Description("Отправляет сообщения в GigaChat и возвращает ответ модели.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(GigaChatGenerator), "Activities.GigaChatCommunity.GigaChat.png")]
public class GigaChatGenerator : Activity
{
    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsRequired]
    public required System.Security.SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("Модель:")]
    [BR.Core.Attributes.Description("Модель GigaChat")]
    public Models Model { get; set; } = Models.GigaChat;

    [BR.Core.Attributes.ScreenName("System prompt:")]
    [BR.Core.Attributes.Description("Необязательная системная инструкция")]
    public string? SystemPrompt { get; set; }

    [BR.Core.Attributes.ScreenName("User prompt:")]
    [BR.Core.Attributes.Description("Сообщение пользователя")]
    [BR.Core.Attributes.IsRequired]
    public required string UserPrompt { get; set; }

    [BR.Core.Attributes.ScreenName("Attachment ID:")]
    [BR.Core.Attributes.Description("Необязательный идентификатор файла из хранилища GigaChat")]
    public string? AttachmentId { get; set; }

    [BR.Core.Attributes.ScreenName("Temperature:")]
    [BR.Core.Attributes.Description("Температура выборки от 0 до 2")]
    public double Temperature { get; set; } = 0.7;

    [BR.Core.Attributes.ScreenName("Max tokens:")]
    [BR.Core.Attributes.Description("Максимальное число токенов ответа")]
    public int MaxTokens { get; set; } = 1024;

    [BR.Core.Attributes.ScreenName("Ответ модели:")]
    [BR.Core.Attributes.Description("Содержимое ответа")]
    [BR.Core.Attributes.IsOut]
    public string? Content { get; set; }

    [BR.Core.Attributes.ScreenName("Списано токенов:")]
    [BR.Core.Attributes.Description("Общее число тарифицируемых токенов")]
    [BR.Core.Attributes.IsOut]
    public int TotalTokens { get; set; }

    [BR.Core.Attributes.ScreenName("Ответ сервиса:")]
    [BR.Core.Attributes.Description("Полный JSON-ответ")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        Content = null;
        TotalTokens = 0;
        Response = default;

        if (string.IsNullOrWhiteSpace(UserPrompt))
            throw new ArgumentException("Сообщение пользователя не указано.", nameof(UserPrompt));
        if (Temperature is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(Temperature), "Temperature должна быть от 0 до 2.");
        if (MaxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxTokens), "MaxTokens должен быть положительным.");

        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
            messages.Add(new ChatMessage { Role = "system", Content = SystemPrompt });

        var userMessage = new ChatMessage { Role = "user", Content = UserPrompt };
        if (!string.IsNullOrWhiteSpace(AttachmentId))
            userMessage.Attachments = [AttachmentId.Trim()];
        messages.Add(userMessage);

        Response = GigaChatHttp.PostJson(
            AccessToken,
            "chat/completions",
            new ChatRequest
            {
                Model = Model.ToString().Replace("_", "-", StringComparison.Ordinal),
                Temperature = Temperature,
                MaxTokens = MaxTokens,
                Messages = messages
            });

        ChatResponse parsed = Response.Deserialize<ChatResponse>()
            ?? throw new InvalidDataException("GigaChat вернул пустой ответ генерации.");
        Content = parsed.Choices.FirstOrDefault()?.Message.Content
            ?? throw new InvalidDataException("В ответе GigaChat отсутствует текст модели.");
        TotalTokens = parsed.Usage?.TotalTokens ?? 0;
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Attachments { get; set; }
    }

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = [];

        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public MessageInfo Message { get; set; } = new();
    }

    private sealed class MessageInfo
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class UsageInfo
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}