using System.Net.NetworkInformation;
using System.Text;

namespace Activities.DiagnosticsCommunity;

[BR.Core.Attributes.ScreenName("Получить состояние сети")]
[BR.Core.Attributes.Representation("Проверить доступность узла через системный Ping")]
[BR.Core.Attributes.Description("Отправляет ICMP-пакеты без запуска внешнего процесса.")]
[BR.Core.Attributes.Path("Custom activities.Diagnostics")]
[BR.Core.Attributes.Image(typeof(NetworkInfo), "Activities.DiagnosticsCommunity.NetworkInfo.png")]
public class NetworkInfo : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Целевой ресурс:")]
    [BR.Core.Attributes.IsRequired]
    public string Target { get; set; } = "example.com";

    [BR.Core.Attributes.ScreenName("Количество пакетов:")]
    public int PacketCount { get; set; } = 4;

    [BR.Core.Attributes.ScreenName("Размер пакета:")]
    public int PacketSize { get; set; } = 32;

    [BR.Core.Attributes.ScreenName("Таймаут одного пакета, мс:")]
    public int TimeoutMilliseconds { get; set; } = 3000;

    [BR.Core.Attributes.ScreenName("Результат выполнения:")]
    [BR.Core.Attributes.IsOut]
    public string Result { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        Result = string.Empty;
        if (string.IsNullOrWhiteSpace(Target))
            throw new ArgumentException("Не указан целевой ресурс.", nameof(Target));
        if (PacketCount is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(PacketCount), "Количество пакетов должно быть от 1 до 100.");
        if (PacketSize is < 0 or > 65500)
            throw new ArgumentOutOfRangeException(nameof(PacketSize), "Размер пакета должен быть от 0 до 65500.");
        if (TimeoutMilliseconds is < 100 or > 30000)
            throw new ArgumentOutOfRangeException(nameof(TimeoutMilliseconds), "Таймаут должен быть от 100 до 30000 мс.");

        byte[] buffer = new byte[PacketSize];
        var replies = new List<PingReply>(PacketCount);
        using var ping = new Ping();
        for (int index = 0; index < PacketCount; index++)
        {
            replies.Add(ping.Send(Target.Trim(), TimeoutMilliseconds, buffer));
        }

        PingReply[] successful = replies.Where(reply => reply.Status == IPStatus.Success).ToArray();
        long average = successful.Length == 0 ? 0 : (long)successful.Average(reply => reply.RoundtripTime);
        var output = new StringBuilder()
            .AppendLine($"Отправлено: {PacketCount}")
            .AppendLine($"Получено: {successful.Length}")
            .AppendLine($"Потеряно: {PacketCount - successful.Length}");
        if (successful.Length > 0)
        {
            output.Append($"Среднее время: {average} мс");
        }
        else
        {
            output.Append("Успешных ответов нет.");
        }
        Result = output.ToString();
    }
}
