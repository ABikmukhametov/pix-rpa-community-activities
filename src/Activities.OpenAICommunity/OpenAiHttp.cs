using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security;
using System.Text.Json;

namespace Activities.OpenAICommunity;

internal static class OpenAiHttp
{
    private const int MaxResponseBytes = 10 * 1024 * 1024;
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(90)
    };

    public static JsonDocument Send(HttpMethod method, string endpoint, SecureString token, object? body = null)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Endpoint должен быть абсолютным HTTPS URL.", nameof(endpoint));
        }

        string tokenValue = new System.Net.NetworkCredential(string.Empty, token).Password;
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            throw new ArgumentException("API-токен не указан.", nameof(token));
        }

        using HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        HttpResponseMessage response;
        try
        {
            response = Client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException("Превышено время ожидания ответа OpenAI.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException("Не удалось выполнить запрос OpenAI.", ex);
        }

        using (response)
        {
            string? requestId = response.Headers.TryGetValues("x-request-id", out IEnumerable<string>? values)
                ? values.FirstOrDefault()
                : null;
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"OpenAI вернул HTTP {(int)response.StatusCode}" +
                    (string.IsNullOrWhiteSpace(requestId) ? "." : $", request ID: {requestId}."),
                    null,
                    response.StatusCode);
            }

            if (response.Content.Headers.ContentLength > MaxResponseBytes)
            {
                throw new InvalidDataException("Ответ OpenAI превышает допустимый размер.");
            }

            using Stream stream = response.Content.ReadAsStream();
            using MemoryStream buffer = new();
            byte[] chunk = new byte[81920];
            int total = 0;
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                total += read;
                if (total > MaxResponseBytes)
                {
                    throw new InvalidDataException("Ответ OpenAI превышает допустимый размер.");
                }
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            return JsonDocument.Parse(buffer);
        }
    }
}
