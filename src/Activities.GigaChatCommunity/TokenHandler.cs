using System.Net.Http.Headers;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using BR.Core;

namespace Activities.GigaChatCommunity;

public enum Scopes
{
    GIGACHAT_API_PERS,
    GIGACHAT_API_B2B,
    GIGACHAT_API_CORP
}

public enum Models
{
    GigaChat,
    GigaChat_Pro,
    GigaChat_Max
}

[BR.Core.Attributes.ScreenName("Получить токен авторизации GigaChat")]
[BR.Core.Attributes.Representation("Получить токен авторизации GigaChat")]
[BR.Core.Attributes.Description("Обменивает Authorization key на токен доступа GigaChat.")]
[BR.Core.Attributes.Path(@"Custom activities.Модели.GigaChat")]
[BR.Core.Attributes.Image(typeof(TokenHandler), "Activities.GigaChatCommunity.GigaChat.png")]
public class TokenHandler : Activity
{
    private static readonly Uri TokenEndpoint = new("https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    [BR.Core.Attributes.ScreenName("Authorization key (SecureString):")]
    [BR.Core.Attributes.Description("Base64-ключ авторизации из настроек проекта GigaChat API.")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString ClientSecret { get; set; }

    [BR.Core.Attributes.ScreenName("Scope:")]
    [BR.Core.Attributes.Description("Версия доступа к API")]
    [BR.Core.Attributes.IsRequired]
    public required Scopes Scope { get; set; } = Scopes.GIGACHAT_API_PERS;

    [BR.Core.Attributes.ScreenName("Access token (SecureString):")]
    [BR.Core.Attributes.Description("Токен доступа GigaChat")]
    [BR.Core.Attributes.IsOut]
    public required SecureString AccessToken { get; set; }

    [BR.Core.Attributes.ScreenName("Время действия токена:")]
    [BR.Core.Attributes.Description("Локальное время окончания действия токена")]
    [BR.Core.Attributes.IsOut]
    public required DateTime ExpiresAt { get; set; }

    public override void Execute(int? optionID)
    {
        string authorizationKey = GigaChatHttp.Reveal(ClientSecret);
        if (string.IsNullOrWhiteSpace(authorizationKey))
            throw new ArgumentException("Authorization key не указан.", nameof(ClientSecret));

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
        request.Headers.Add("RqUID", Guid.NewGuid().ToString());
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("scope", Scope.ToString())
        ]);

        using HttpResponseMessage response = Client.Send(request);
        byte[] bytes = GigaChatHttp.ReadBounded(response);
        if (!response.IsSuccessStatusCode)
            throw GigaChatHttp.CreateHttpException(response);

        TokenResponse tokenResponse;
        try
        {
            tokenResponse = JsonSerializer.Deserialize<TokenResponse>(bytes)
                ?? throw new InvalidDataException("GigaChat вернул пустой ответ.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GigaChat вернул некорректный ответ токена.", ex);
        }

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            throw new InvalidDataException("В ответе GigaChat отсутствует access_token.");

        AccessToken = ToSecureString(tokenResponse.AccessToken);
        ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(tokenResponse.ExpiresAt).LocalDateTime;
    }

    private static SecureString ToSecureString(string value)
    {
        var result = new SecureString();
        foreach (char character in value)
            result.AppendChar(character);
        result.MakeReadOnly();
        return result;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }
    }
}