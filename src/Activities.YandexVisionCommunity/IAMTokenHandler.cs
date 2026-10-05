using System.Net.Http.Json;
using System.Security;
using System.Text.Json;
using BR.Core;

namespace Activities.YandexVisionCommunity;

[BR.Core.Attributes.ScreenName("Получить IAM-токен")]
[BR.Core.Attributes.Representation("Получить IAM-токен")]
[BR.Core.Attributes.Description("Обменивает OAuth-токен Яндекса на IAM-токен.")]
[BR.Core.Attributes.Path(@"Custom activities.Yandex Vision")]
[BR.Core.Attributes.Image(typeof(IAMTokenHandler), "Activities.YandexVisionCommunity.YandexVision.png")]
public class IAMTokenHandler : Activity
{
    private static readonly Uri Endpoint = new("https://iam.api.cloud.yandex.net/iam/v1/tokens");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    [BR.Core.Attributes.ScreenName("OAuth-токен (SecureString):")]
    [BR.Core.Attributes.Description("OAuth-токен аккаунта Яндекса")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString OAuthToken { get; set; }

    [BR.Core.Attributes.ScreenName("IAM-токен (SecureString):")]
    [BR.Core.Attributes.Description("IAM-токен для запросов к Yandex Cloud")]
    [BR.Core.Attributes.IsOut]
    public required SecureString IAMToken { get; set; }

    [BR.Core.Attributes.ScreenName("Время действия токена:")]
    [BR.Core.Attributes.Description("Срок действия из ответа сервиса в RFC 3339")]
    [BR.Core.Attributes.IsOut]
    public string? ExpiresAt { get; set; }

    public override void Execute(int? optionID)
    {
        string oauthToken = Reveal(OAuthToken);
        if (string.IsNullOrWhiteSpace(oauthToken))
            throw new ArgumentException("OAuth-токен не указан.", nameof(OAuthToken));

        using HttpResponseMessage response = Client.PostAsJsonAsync(
            Endpoint,
            new { yandexPassportOauthToken = oauthToken }).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Yandex IAM вернул HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).",
                null,
                response.StatusCode);

        byte[] bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        if (bytes.Length > 1024 * 1024)
            throw new InvalidDataException("Ответ Yandex IAM превышает 1 МБ.");

        using JsonDocument document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        string token = root.TryGetProperty("iamToken", out JsonElement tokenElement)
            ? tokenElement.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidDataException("В ответе Yandex IAM отсутствует iamToken.");

        IAMToken = ToSecureString(token);
        ExpiresAt = root.TryGetProperty("expiresAt", out JsonElement expiresAt)
            ? expiresAt.GetString()
            : null;
    }

    private static string Reveal(SecureString value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new System.Net.NetworkCredential(string.Empty, value).Password;
    }

    private static SecureString ToSecureString(string value)
    {
        var result = new SecureString();
        foreach (char character in value)
            result.AppendChar(character);
        result.MakeReadOnly();
        return result;
    }
}