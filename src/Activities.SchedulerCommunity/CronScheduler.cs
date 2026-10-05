using Quartz;

namespace Activities.SchedulerCommunity;

[BR.Core.Attributes.ScreenName("Расчёт времени запуска по CRON")]
[BR.Core.Attributes.Representation("Расчёт следующего запуска по CRON и часовому поясу")]
[BR.Core.Attributes.Description("Возвращает ближайшее время строго после LastRun.")]
[BR.Core.Attributes.Path("Custom activities.Schedule")]
[BR.Core.Attributes.Image(typeof(CronScheduler), "Activities.SchedulerCommunity.Icon.png")]
public class CronScheduler : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("CRON-выражение:")]
    [BR.Core.Attributes.Description("Quartz CRON: секунды, минуты, часы, день, месяц, день недели, необязательный год")]
    [BR.Core.Attributes.IsRequired]
    public required string Expression { get; set; }

    [BR.Core.Attributes.ScreenName("Время последнего запуска:")]
    [BR.Core.Attributes.Description("Точка отсчёта; следующий запуск будет строго позже")]
    [BR.Core.Attributes.IsRequired]
    public required DateTime LastRun { get; set; }

    [BR.Core.Attributes.ScreenName("Идентификатор часового пояса:")]
    [BR.Core.Attributes.Description("Системный TimeZoneId; пустое значение означает локальный часовой пояс")]
    public string? TimeZoneId { get; set; }

    [BR.Core.Attributes.ScreenName("Ближайший запуск:")]
    [BR.Core.Attributes.IsOut]
    public DateTime NextRun { get; set; }

    public override void Execute(int? optionID)
    {
        NextRun = default;
        if (string.IsNullOrWhiteSpace(Expression) || !CronExpression.IsValidExpression(Expression))
        {
            throw new ArgumentException("Некорректное Quartz CRON-выражение.", nameof(Expression));
        }

        TimeZoneInfo zone = string.IsNullOrWhiteSpace(TimeZoneId)
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        DateTimeOffset baseUtc = ToUtc(LastRun, zone);
        var cron = new CronExpression(Expression) { TimeZone = zone };
        DateTimeOffset? next = cron.GetNextValidTimeAfter(baseUtc);
        if (!next.HasValue)
        {
            throw new InvalidOperationException("Следующий запуск для заданного расписания не найден.");
        }
        if (next.Value <= baseUtc)
        {
            throw new InvalidOperationException("Планировщик вернул время не позже LastRun.");
        }

        NextRun = TimeZoneInfo.ConvertTime(next.Value, zone).DateTime;
    }

    private static DateTimeOffset ToUtc(DateTime value, TimeZoneInfo zone)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(value);
        }
        if (value.Kind == DateTimeKind.Local)
        {
            return new DateTimeOffset(value).ToUniversalTime();
        }

        if (zone.IsInvalidTime(value))
        {
            throw new ArgumentException("LastRun попадает в отсутствующий интервал перехода на летнее время.");
        }
        if (zone.IsAmbiguousTime(value))
        {
            throw new ArgumentException("LastRun неоднозначен из-за перехода с летнего времени.");
        }

        return new DateTimeOffset(value, zone.GetUtcOffset(value)).ToUniversalTime();
    }
}
