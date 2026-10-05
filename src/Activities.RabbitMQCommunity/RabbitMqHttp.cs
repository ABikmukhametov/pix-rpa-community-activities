using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security;
using System.Text;
using System.Text.Json;

namespace Activities.RabbitMQCommunity;

internal static class RabbitMqHttp
{
    private const int MaxResponseBytes = 10 * 1024 * 1024;
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static JsonDocument Send(
        HttpMethod method,
        string host,
        string relativePath,
        string user,
        SecureString password,
        bool allowInsecureHttp,
        object? body = null)
    {
        Uri baseUri = ValidateHost(host, allowInsecureHttp);
        if (string.IsNullOrWhiteSpace(user))
            throw new ArgumentException("Имя пользователя RabbitMQ не указано.", nameof(user));
        string passwordText = new NetworkCredential(string.Empty, password).Password;
        if (string.IsNullOrEmpty(passwordText))
            throw new ArgumentException("Пароль RabbitMQ не указан.", nameof(password));

        using var request = new HttpRequestMessage(method, new Uri(baseUri, relativePath));
        string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{passwordText}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
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
            throw new TimeoutException("Превышено время ожидания RabbitMQ Management API.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException("Не удалось выполнить запрос RabbitMQ Management API.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"RabbitMQ Management API вернул HTTP {(int)response.StatusCode}.",
                    null,
                    response.StatusCode);
            }
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                throw new InvalidDataException("Ответ RabbitMQ превышает допустимый размер.");

            using Stream input = response.Content.ReadAsStream();
            using MemoryStream output = new();
            byte[] buffer = new byte[81920];
            int total = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > MaxResponseBytes)
                    throw new InvalidDataException("Ответ RabbitMQ превышает допустимый размер.");
                output.Write(buffer, 0, read);
            }
            output.Position = 0;
            try
            {
                return JsonDocument.Parse(output);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("RabbitMQ вернул повреждённый JSON.", ex);
            }
        }
    }

    public static string Segment(string value, string parameter, bool allowEmpty = false)
    {
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Значение сегмента RabbitMQ не указано.", parameter);
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    private static Uri ValidateHost(string host, bool allowInsecureHttp)
    {
        if (!Uri.TryCreate(host?.TrimEnd('/') + "/", UriKind.Absolute, out Uri? uri))
            throw new ArgumentException("Некорректный URL RabbitMQ.", nameof(host));
        if (uri.Scheme == Uri.UriSchemeHttps)
            return uri;
        if (uri.Scheme != Uri.UriSchemeHttp)
            throw new ArgumentException("RabbitMQ URL должен использовать HTTP или HTTPS.", nameof(host));

        bool loopback = uri.IsLoopback ||
                        string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        if (!loopback && !allowInsecureHttp)
            throw new InvalidOperationException("Удалённый RabbitMQ требует HTTPS; HTTP разрешается только через AllowInsecureHttp.");
        return uri;
    }
}
