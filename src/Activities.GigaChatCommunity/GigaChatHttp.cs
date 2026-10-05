using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security;
using System.Text.Json;

namespace Activities.GigaChatCommunity;

internal static class GigaChatHttp
{
    private const int MaxResponseBytes = 10 * 1024 * 1024;
    private static readonly Uri ApiBaseUri = new("https://api.giga.chat/v1/");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(90) };

    internal static JsonElement GetJson(SecureString accessToken, string relativePath) =>
        SendJson(accessToken, HttpMethod.Get, relativePath, null);

    internal static JsonElement PostJson(SecureString accessToken, string relativePath, object? body) =>
        SendJson(accessToken, HttpMethod.Post, relativePath, body);

    internal static JsonElement SendJson(
        SecureString accessToken,
        HttpMethod method,
        string relativePath,
        object? body)
    {
        using var request = new HttpRequestMessage(method, new Uri(ApiBaseUri, relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Reveal(accessToken));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
            request.Content = JsonContent.Create(body);

        using HttpResponseMessage response = Client.Send(request);
        byte[] responseBytes = ReadBounded(response);
        if (!response.IsSuccessStatusCode)
            throw CreateHttpException(response);

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBytes);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GigaChat вернул некорректный JSON.", ex);
        }
    }

    internal static JsonElement Upload(SecureString accessToken, string filePath, string mimeType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ApiBaseUri, "files"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Reveal(accessToken));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var form = new MultipartFormDataContent();
        using var stream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));
        form.Add(new StringContent("general"), "purpose");
        request.Content = form;

        using HttpResponseMessage response = Client.Send(request);
        byte[] responseBytes = ReadBounded(response);
        if (!response.IsSuccessStatusCode)
            throw CreateHttpException(response);

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBytes);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GigaChat вернул некорректный JSON.", ex);
        }
    }

    internal static string Reveal(SecureString value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new System.Net.NetworkCredential(string.Empty, value).Password;
    }

    internal static HttpRequestException CreateHttpException(HttpResponseMessage response)
    {
        string requestId = response.Headers.TryGetValues("x-request-id", out var values)
            ? $", request id: {values.FirstOrDefault()}"
            : string.Empty;
        return new HttpRequestException(
            $"GigaChat вернул HTTP {(int)response.StatusCode} ({response.ReasonPhrase}){requestId}.",
            null,
            response.StatusCode);
    }

    internal static byte[] ReadBounded(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentLength is long length && length > MaxResponseBytes)
            throw new InvalidDataException("Ответ GigaChat превышает допустимый размер 10 МБ.");

        using Stream input = response.Content.ReadAsStream();
        using var output = new MemoryStream();
        input.CopyTo(output);
        if (output.Length > MaxResponseBytes)
            throw new InvalidDataException("Ответ GigaChat превышает допустимый размер 10 МБ.");
        return output.ToArray();
    }
}