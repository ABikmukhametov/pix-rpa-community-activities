using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace Activities.ChromeDriverCommunity;

[BR.Core.Attributes.ScreenName("Обновить chromedriver")]
[BR.Core.Attributes.Representation("Безопасно установить последний стабильный chromedriver для Win x64")]
[BR.Core.Attributes.Description(
    "Скачивает и проверяет ChromeDriver во временном каталоге, затем заменяет рабочую версию. " +
    "При ошибке восстанавливает предыдущий драйвер.")]
[BR.Core.Attributes.Image(typeof(ChromeDriverUpdater), "Activities.ChromeDriverCommunity.Icon.png")]
[BR.Core.Attributes.Path("Custom activities.Web")]
public class ChromeDriverUpdater : BR.Core.Activity
{
    private const long MaxArchiveBytes = 200L * 1024 * 1024;
    private static readonly Uri VersionsUri = new(
        "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json");
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    [BR.Core.Attributes.ScreenName("Статус:")]
    [BR.Core.Attributes.Description("Статус обновления без локальных путей")]
    [BR.Core.Attributes.IsOut]
    public string StatusLog { get; set; } = string.Empty;

    public override void Execute(int? optionID)
    {
        StatusLog = string.Empty;
        string targetDirectory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PIX",
            "WebDriver");
        string stagingDirectory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "PIX",
            "ChromeDriverUpdate",
            Guid.NewGuid().ToString("N"));
        string backupDirectory = System.IO.Path.Combine(stagingDirectory, "backup");
        string extractedDirectory = System.IO.Path.Combine(stagingDirectory, "extract");
        string archivePath = System.IO.Path.Combine(stagingDirectory, "chromedriver.zip");
        string targetDriver = System.IO.Path.Combine(targetDirectory, "chromedriver.exe");
        string targetVersion = System.IO.Path.Combine(targetDirectory, "chromedriver.txt");
        string backupDriver = System.IO.Path.Combine(backupDirectory, "chromedriver.exe");
        string backupVersion = System.IO.Path.Combine(backupDirectory, "chromedriver.txt");
        bool driverBackedUp = false;
        bool versionBackedUp = false;
        bool installationStarted = false;

        try
        {
            Directory.CreateDirectory(stagingDirectory);
            Directory.CreateDirectory(extractedDirectory);
            Directory.CreateDirectory(backupDirectory);

            StatusLog = "Получение информации о стабильной версии...";
            DownloadInfo download = GetDownloadInfo();
            DownloadArchive(download.Url, archivePath);
            string extractedDriver = ExtractAndValidateDriver(
                archivePath,
                extractedDirectory,
                download.Version);

            Directory.CreateDirectory(targetDirectory);
            driverBackedUp = BackupFile(targetDriver, backupDriver);
            versionBackedUp = BackupFile(targetVersion, backupVersion);
            installationStarted = true;

            File.Copy(extractedDriver, targetDriver, true);
            File.WriteAllText(targetVersion, download.Version);
            ValidateInstalledDriver(targetDriver, download.Version);

            Directory.Delete(stagingDirectory, true);
            StatusLog = $"ChromeDriver {download.Version} успешно обновлён.";
        }
        catch (Exception originalError)
        {
            Exception? rollbackError = null;
            try
            {
                if (installationStarted)
                {
                    RestoreFile(targetDriver, backupDriver, driverBackedUp);
                    RestoreFile(targetVersion, backupVersion, versionBackedUp);
                }
            }
            catch (Exception ex)
            {
                rollbackError = ex;
            }

            try
            {
                if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(stagingDirectory, true);
                }
            }
            catch (Exception ex)
            {
                rollbackError ??= ex;
            }

            StatusLog = "Обновление ChromeDriver завершилось ошибкой.";
            if (rollbackError is not null)
            {
                throw new AggregateException(
                    "Не удалось восстановить предыдущую версию ChromeDriver.",
                    rollbackError,
                    originalError);
            }

            throw new InvalidOperationException(
                "Не удалось обновить ChromeDriver; предыдущая версия восстановлена.",
                originalError);
        }
    }

    private static DownloadInfo GetDownloadInfo()
    {
        using HttpResponseMessage response = HttpClient.GetAsync(VersionsUri).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement stable = document.RootElement
            .GetProperty("channels")
            .GetProperty("Stable");
        string version = stable.GetProperty("version").GetString()
            ?? throw new InvalidDataException("В ответе отсутствует версия ChromeDriver.");

        foreach (JsonElement item in stable.GetProperty("downloads").GetProperty("chromedriver").EnumerateArray())
        {
            if (item.GetProperty("platform").GetString() != "win64")
            {
                continue;
            }

            string value = item.GetProperty("url").GetString()
                ?? throw new InvalidDataException("В ответе отсутствует URL ChromeDriver.");
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? url) || url.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidDataException("Сервис вернул небезопасный URL ChromeDriver.");
            }

            return new DownloadInfo(url, version);
        }

        throw new InvalidDataException("Версия ChromeDriver для win64 не найдена.");
    }

    private static void DownloadArchive(Uri url, string destination)
    {
        using HttpResponseMessage response = HttpClient
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter()
            .GetResult();
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxArchiveBytes)
        {
            throw new InvalidDataException("Архив ChromeDriver превышает допустимый размер.");
        }

        using Stream input = response.Content.ReadAsStream();
        using FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > MaxArchiveBytes)
            {
                throw new InvalidDataException("Архив ChromeDriver превышает допустимый размер.");
            }
            output.Write(buffer, 0, read);
        }

        if (total == 0)
        {
            throw new InvalidDataException("Получен пустой архив ChromeDriver.");
        }
    }

    private static string ExtractAndValidateDriver(string archivePath, string destination, string expectedVersion)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        ZipArchiveEntry[] candidates = archive.Entries
            .Where(entry => string.Equals(
                System.IO.Path.GetFileName(entry.FullName),
                "chromedriver.exe",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (candidates.Length != 1 || candidates[0].Length <= 0)
        {
            throw new InvalidDataException("Архив должен содержать ровно один непустой chromedriver.exe.");
        }

        string extractedDriver = System.IO.Path.Combine(destination, "chromedriver.exe");
        using (Stream input = candidates[0].Open())
        using (FileStream output = new(extractedDriver, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            input.CopyTo(output);
        }

        ValidateInstalledDriver(extractedDriver, expectedVersion);
        return extractedDriver;
    }

    private static bool BackupFile(string source, string backup)
    {
        if (!File.Exists(source))
        {
            return false;
        }

        try
        {
            File.Move(source, backup, true);
        }
        catch (IOException)
        {
            File.Copy(source, backup, true);
            VerifyCopy(source, backup);
            File.Delete(source);
        }
        catch (UnauthorizedAccessException)
        {
            File.Copy(source, backup, true);
            VerifyCopy(source, backup);
            File.Delete(source);
        }

        if (!File.Exists(backup))
        {
            throw new IOException("Не удалось создать резервную копию ChromeDriver.");
        }
        return true;
    }

    private static void RestoreFile(string target, string backup, bool existed)
    {
        if (existed)
        {
            File.Copy(backup, target, true);
            VerifyCopy(backup, target);
        }
        else if (File.Exists(target))
        {
            File.Delete(target);
        }
    }

    private static void VerifyCopy(string source, string destination)
    {
        if (!File.Exists(destination) || new FileInfo(source).Length != new FileInfo(destination).Length)
        {
            throw new IOException("Проверка резервной копии ChromeDriver не пройдена.");
        }
    }

    private static void ValidateInstalledDriver(string driverPath, string expectedVersion)
    {
        FileInfo file = new(driverPath);
        if (!file.Exists || file.Length == 0)
        {
            throw new InvalidDataException("Файл ChromeDriver отсутствует или пуст.");
        }

        string? actualVersion = FileVersionInfo.GetVersionInfo(driverPath).FileVersion;
        if (string.IsNullOrWhiteSpace(actualVersion) ||
            !actualVersion.StartsWith(expectedVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Версия chromedriver.exe не совпадает с выбранной стабильной версией.");
        }
    }

    private sealed record DownloadInfo(Uri Url, string Version);
}
