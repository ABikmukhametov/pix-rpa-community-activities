using System.Net;
using System.Net.Http.Json;
using System.Security;
using System.Text.Json;

namespace Activities.DaDataCommunity;

[BR.Core.Attributes.ScreenName("Стандартизация адреса DaData")]
[BR.Core.Attributes.Representation("Стандартизировать адрес через DaData")]
[BR.Core.Attributes.Description("Отправляет один адрес в DaData Cleaner и возвращает JSON-ответ.")]
[BR.Core.Attributes.Path("Custom activities.DaData")]
[BR.Core.Attributes.Image(typeof(AddressEntities), "Activities.DaDataCommunity.Icon.png")]
public class AddressEntities : BR.Core.Activity
{
    private const int MaxResponseBytes = 5 * 1024 * 1024;
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    [BR.Core.Attributes.ScreenName("API-ключ (SecureString):")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString ApiKey { get; set; }

    [BR.Core.Attributes.ScreenName("Секретный ключ (SecureString):")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString SecretKey { get; set; }

    [BR.Core.Attributes.ScreenName("Адрес:")]
    [BR.Core.Attributes.IsRequired]
    public required string Address { get; set; }

    [BR.Core.Attributes.ScreenName("Ответ сервиса (JSON):")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        Response = default;
        if (string.IsNullOrWhiteSpace(Address))
            throw new ArgumentException("Адрес не указан.", nameof(Address));

        string apiKey = ToPlainText(ApiKey, nameof(ApiKey));
        string secretKey = ToPlainText(SecretKey, nameof(SecretKey));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://cleaner.dadata.ru/api/v1/clean/address");
        request.Headers.TryAddWithoutValidation("Authorization", "Token " + apiKey);
        request.Headers.TryAddWithoutValidation("X-Secret", secretKey);
        request.Content = JsonContent.Create(new[] { Address });

        HttpResponseMessage response;
        try
        {
            response = Client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException("Превышено время ожидания ответа DaData.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException("Не удалось выполнить запрос DaData.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"DaData вернул HTTP {(int)response.StatusCode}.",
                    null,
                    response.StatusCode);
            }
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
            {
                throw new InvalidDataException("Ответ DaData превышает допустимый размер.");
            }

            using Stream stream = response.Content.ReadAsStream();
            using MemoryStream limited = ReadLimited(stream, MaxResponseBytes);
            try
            {
                using JsonDocument document = JsonDocument.Parse(limited);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException("Ответ DaData имеет неожиданную структуру.");
                }
                Response = document.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("DaData вернул повреждённый JSON.", ex);
            }
        }
    }

    private static string ToPlainText(SecureString value, string parameter)
    {
        string text = new NetworkCredential(string.Empty, value).Password;
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Ключ DaData не указан.", parameter);
        return text;
    }

    private static MemoryStream ReadLimited(Stream source, int limit)
    {
        var result = new MemoryStream();
        byte[] buffer = new byte[81920];
        int total = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > limit)
            {
                result.Dispose();
                throw new InvalidDataException("Ответ DaData превышает допустимый размер.");
            }
            result.Write(buffer, 0, read);
        }
        result.Position = 0;
        return result;
    }
}
