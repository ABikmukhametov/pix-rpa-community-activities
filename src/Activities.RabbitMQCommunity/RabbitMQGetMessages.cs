using System.Security;
using System.Text.Json;

namespace Activities.RabbitMQCommunity;

[BR.Core.Attributes.ScreenName("Получить сообщения из RabbitMQ")]
[BR.Core.Attributes.Representation("Получить сообщения через RabbitMQ Management API")]
[BR.Core.Attributes.Path("Custom activities.RabbitMQ")]
[BR.Core.Attributes.Image(typeof(RabbitMQGetMessages), "Activities.RabbitMQCommunity.Icon.png")]
public class RabbitMQGetMessages : BR.Core.Activity
{
    public enum AckModes
    {
        AckRequeueFalse,
        AckRequeueTrue,
        RejectRequeueFalse,
        RejectRequeueTrue
    }

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

    [BR.Core.Attributes.ScreenName("Queue:")]
    [BR.Core.Attributes.IsRequired]
    public string QueueName { get; set; } = string.Empty;

    [BR.Core.Attributes.ScreenName("Count:")]
    public int Count { get; set; } = 1;

    [BR.Core.Attributes.ScreenName("Ack mode:")]
    public AckModes AckMode { get; set; } = AckModes.AckRequeueTrue;

    [BR.Core.Attributes.ScreenName("Разрешить HTTP для удалённого сервера:")]
    public bool AllowInsecureHttp { get; set; }

    [BR.Core.Attributes.ScreenName("Ответ:")]
    [BR.Core.Attributes.IsOut]
    public JsonElement Response { get; set; }

    public override void Execute(int? optionID)
    {
        Response = default;
        if (Count is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(Count), "Count должен быть от 1 до 1000.");

        string path = $"api/queues/{RabbitMqHttp.Segment(VirtualHost, nameof(VirtualHost))}/" +
                      $"{RabbitMqHttp.Segment(QueueName, nameof(QueueName))}/get";
        object body = new
        {
            count = Count,
            ackmode = MapAckMode(AckMode),
            encoding = "auto"
        };
        using JsonDocument document = RabbitMqHttp.Send(
            HttpMethod.Post, Host, path, User, SecurePassword, AllowInsecureHttp, body);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Ответ RabbitMQ get должен быть JSON-массивом.");
        Response = document.RootElement.Clone();
    }

    private static string MapAckMode(AckModes mode) => mode switch
    {
        AckModes.AckRequeueFalse => "ack_requeue_false",
        AckModes.AckRequeueTrue => "ack_requeue_true",
        AckModes.RejectRequeueFalse => "reject_requeue_false",
        AckModes.RejectRequeueTrue => "reject_requeue_true",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
