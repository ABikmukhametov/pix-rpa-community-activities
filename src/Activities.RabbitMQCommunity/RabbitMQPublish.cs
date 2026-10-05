using System.Security;
using System.Text.Json;

namespace Activities.RabbitMQCommunity;

[BR.Core.Attributes.ScreenName("Отправить сообщение в RabbitMQ")]
[BR.Core.Attributes.Representation("Опубликовать сообщение через RabbitMQ Management API")]
[BR.Core.Attributes.Path("Custom activities.RabbitMQ")]
[BR.Core.Attributes.Image(typeof(RabbitMQPublish), "Activities.RabbitMQCommunity.Icon.png")]
public class RabbitMQPublish : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("URL:")]
    [BR.Core.Attributes.IsRequired]
    public required string Host { get; set; } = "http://localhost:15672";

    [BR.Core.Attributes.ScreenName("Virtual Host:")]
    public string VirtualHost { get; set; } = "/";

    [BR.Core.Attributes.ScreenName("User:")]
    [BR.Core.Attributes.IsRequired]
    public required string User { get; set; } = "guest";

    [BR.Core.Attributes.ScreenName("Password:")]
    [BR.Core.Attributes.IsRequired]
    public required SecureString SecurePassword { get; set; }

    [BR.Core.Attributes.ScreenName("Exchange:")]
    public string Exchange { get; set; } = string.Empty;

    [BR.Core.Attributes.ScreenName("Routing Key:")]
    [BR.Core.Attributes.IsRequired]
    public required string RoutingKey { get; set; }

    [BR.Core.Attributes.ScreenName("Message:")]
    [BR.Core.Attributes.IsRequired]
    public required string MessageBody { get; set; }

    [BR.Core.Attributes.ScreenName("Разрешить HTTP для удалённого сервера:")]
    public bool AllowInsecureHttp { get; set; }

    [BR.Core.Attributes.ScreenName("Ответ:")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        Response = default;
        if (string.IsNullOrWhiteSpace(RoutingKey))
            throw new ArgumentException("Routing Key не указан.", nameof(RoutingKey));
        if (MessageBody is null)
            throw new ArgumentNullException(nameof(MessageBody));

        string path = $"api/exchanges/{RabbitMqHttp.Segment(VirtualHost, nameof(VirtualHost))}/" +
                      $"{RabbitMqHttp.Segment(Exchange, nameof(Exchange), true)}/publish";
        object body = new
        {
            properties = new { content_type = "application/json" },
            routing_key = RoutingKey,
            payload = MessageBody,
            payload_encoding = "string"
        };
        using JsonDocument document = RabbitMqHttp.Send(
            HttpMethod.Post, Host, path, User, SecurePassword, AllowInsecureHttp, body);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("routed", out JsonElement routed) ||
            routed.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidDataException("Ответ RabbitMQ publish не содержит routed.");
        }
        Response = document.RootElement.Clone();
    }
}
