using System.Security;
using System.Text.Json;

namespace Activities.RabbitMQCommunity;

[BR.Core.Attributes.ScreenName("Получить очереди RabbitMQ")]
[BR.Core.Attributes.Representation("Получить список очередей через RabbitMQ Management API")]
[BR.Core.Attributes.Path("Custom activities.RabbitMQ")]
[BR.Core.Attributes.Image(typeof(RabbitMQGetQueues), "Activities.RabbitMQCommunity.Icon.png")]
public class RabbitMQGetQueues : BR.Core.Activity
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

    [BR.Core.Attributes.ScreenName("Разрешить HTTP для удалённого сервера:")]
    public bool AllowInsecureHttp { get; set; }

    [BR.Core.Attributes.ScreenName("Список очередей (JSON):")]
    [BR.Core.Attributes.IsOut]
    public JsonElement QueuesInfo { get; set; }

    public override void Execute(int? optionID)
    {
        QueuesInfo = default;
        string path = $"api/queues/{RabbitMqHttp.Segment(VirtualHost, nameof(VirtualHost))}";
        using JsonDocument document = RabbitMqHttp.Send(
            HttpMethod.Get, Host, path, User, SecurePassword, AllowInsecureHttp);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Ответ RabbitMQ queues должен быть JSON-массивом.");
        QueuesInfo = document.RootElement.Clone();
    }
}
