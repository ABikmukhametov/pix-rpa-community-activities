using System.Diagnostics;

namespace Activities.DiagnosticsCommunity;

[BR.Core.Attributes.ScreenName("Завершить процессы по списку наименований (только для текущего пользователя)")]
[BR.Core.Attributes.Representation("Завершить процессы текущей сессии")]
[BR.Core.Attributes.Description("Сначала формирует список кандидатов, исключает текущий процесс и затем завершает найденные процессы.")]
[BR.Core.Attributes.Path("Custom activities.Diagnostics")]
[BR.Core.Attributes.Image(typeof(ProcessKiller), "Activities.DiagnosticsCommunity.ProcessKill.png")]
public class ProcessKiller : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Имена процессов:")]
    [BR.Core.Attributes.IsRequired]
    public required List<string> ProcessNames { get; set; }

    [BR.Core.Attributes.ScreenName("Результат выполнения:")]
    [BR.Core.Attributes.IsOut]
    public string ResultInfo { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        ResultInfo = string.Empty;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Активность поддерживается только в Windows.");
        if (ProcessNames is null || ProcessNames.Count == 0)
            throw new ArgumentException("Не указан список процессов.", nameof(ProcessNames));

        HashSet<string> names = ProcessNames.Select(NormalizeName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int currentId = Environment.ProcessId;
        int currentSessionId;
        using (Process current = Process.GetCurrentProcess())
        {
            currentSessionId = current.SessionId;
        }

        var candidates = new List<Process>();
        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                if (process.Id != currentId &&
                    process.SessionId == currentSessionId &&
                    names.Contains(process.ProcessName))
                {
                    candidates.Add(process);
                }
                else
                {
                    process.Dispose();
                }
            }
            catch
            {
                process.Dispose();
            }
        }

        var completed = new List<string>();
        var failures = new List<Exception>();
        foreach (Process process in candidates)
        {
            using (process)
            {
                try
                {
                    string name = process.ProcessName;
                    int id = process.Id;
                    process.Kill();
                    if (!process.WaitForExit(5000))
                    {
                        throw new TimeoutException($"Процесс {name}.exe не завершился за 5 секунд.");
                    }
                    completed.Add($"{name}.exe (ID: {id})");
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
        }

        ResultInfo = completed.Count == 0
            ? "Ни один процесс не был завершён."
            : $"Завершено процессов: {completed.Count}\n" + string.Join("\n", completed);
        if (failures.Count > 0)
        {
            throw new AggregateException("Не все выбранные процессы удалось завершить.", failures);
        }
    }

    private static string NormalizeName(string value)
    {
        string name = value?.Trim() ?? string.Empty;
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }
        if (name.Length == 0 ||
            name.IndexOfAny(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar, '*', '?' }) >= 0)
        {
            throw new ArgumentException("Имя процесса пусто или содержит недопустимые символы.");
        }
        return name;
    }
}
