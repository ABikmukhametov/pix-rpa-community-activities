using BR.Core;
using System.IO.Compression;
using System.Text;

namespace Activities.LogsCommunity
{
    [BR.Core.Attributes.ScreenName("Удалить/архивировать лог-файлы")]
    [BR.Core.Attributes.Representation("Очистка логов")]
    [BR.Core.Attributes.Description("Показывает или обрабатывает TXT-файлы не новее пороговой даты. Архив проверяется до удаления исходников.")]
    [BR.Core.Attributes.Path(@"Custom activities.Logs")]
    [BR.Core.Attributes.Image(typeof(LogCleaner), "Activities.LogsCommunity.Icon.png")]
    public class LogCleaner : Activity
    {
        [BR.Core.Attributes.ScreenName("Каталог с логами:")]
        [BR.Core.Attributes.Description("Основной каталог для поиска лог-файлов")]
        [BR.Core.Attributes.IsRequired]
        [BR.Core.Attributes.IsFilePathChooser]
        public required string LogsDirectory { get; set; } = "Logs";

        [BR.Core.Attributes.ScreenName("Удалять без архива:")]
        [BR.Core.Attributes.Description("True — удалить; False — сначала создать и проверить ZIP-архив")]
        public bool NeedDelete { get; set; }

        [BR.Core.Attributes.ScreenName("Только просмотр:")]
        [BR.Core.Attributes.Description("True — сформировать отчет без изменения файлов")]
        public bool PreviewOnly { get; set; } = true;

        [BR.Core.Attributes.ScreenName("Пороговая дата:")]
        [BR.Core.Attributes.Description("Обрабатываются файлы, созданные до конца указанной даты включительно")]
        [BR.Core.Attributes.IsRequired]
        public required DateTime ThresholdDate { get; set; }

        [BR.Core.Attributes.ScreenName("Отчет:")]
        [BR.Core.Attributes.Description("Результат проверки или обработки")]
        [BR.Core.Attributes.IsOut]
        public required string Report { get; set; }

        public override void Execute(int? optionID)
        {
            string root = System.IO.Path.GetFullPath(
                string.IsNullOrWhiteSpace(LogsDirectory)
                    ? throw new ArgumentException("Каталог с логами не указан.")
                    : LogsDirectory);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"Каталог с логами не найден: {root}");

            string archiveDirectory = System.IO.Path.Combine(root, "Archives");
            DateTime cutoffExclusive = ThresholdDate.Date.AddDays(1);
            var files = Directory.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
                .Select(System.IO.Path.GetFullPath)
                .Where(path => !IsUnderDirectory(path, archiveDirectory))
                .Select(path => new FileInfo(path))
                .Where(file => file.CreationTime < cutoffExclusive)
                .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            long totalSize = files.Sum(file => file.Length);
            if (files.Count == 0)
            {
                Report = "Нет файлов для обработки.";
                return;
            }

            if (PreviewOnly)
            {
                Report = BuildReport(root, files, totalSize, "ПРЕДВАРИТЕЛЬНЫЙ ПРОСМОТР", null);
                return;
            }

            if (NeedDelete)
            {
                foreach (FileInfo file in files)
                    file.Delete();
                Report = BuildReport(root, files, totalSize, "УДАЛЕНИЕ ЗАВЕРШЕНО", null);
                return;
            }

            Directory.CreateDirectory(archiveDirectory);
            string finalArchive = GetUniqueArchivePath(
                archiveDirectory,
                $"logs_{files.Min(f => f.CreationTime):yyyyMMdd}_{files.Max(f => f.CreationTime):yyyyMMdd}.zip");
            string temporaryArchive = finalArchive + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                using (var archive = ZipFile.Open(temporaryArchive, ZipArchiveMode.Create))
                {
                    foreach (FileInfo file in files)
                    {
                        string entryName = System.IO.Path.GetRelativePath(root, file.FullName);
                        archive.CreateEntryFromFile(file.FullName, entryName, CompressionLevel.Optimal);
                    }
                }

                VerifyArchive(temporaryArchive, root, files);
                File.Move(temporaryArchive, finalArchive);
                foreach (FileInfo file in files)
                    file.Delete();
            }
            catch
            {
                if (File.Exists(temporaryArchive))
                    File.Delete(temporaryArchive);
                throw;
            }

            Report = BuildReport(root, files, totalSize, "АРХИВАЦИЯ ЗАВЕРШЕНА", finalArchive);
        }

        private static void VerifyArchive(string archivePath, string root, IReadOnlyCollection<FileInfo> files)
        {
            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count != files.Count)
                throw new InvalidDataException("Проверка архива не пройдена: число записей не совпадает.");

            var expected = files.ToDictionary(
                file => System.IO.Path.GetRelativePath(root, file.FullName).Replace('\\', '/'),
                file => file.Length,
                StringComparer.OrdinalIgnoreCase);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!expected.TryGetValue(entry.FullName.Replace('\\', '/'), out long length) || entry.Length != length)
                    throw new InvalidDataException($"Проверка архива не пройдена для записи '{entry.FullName}'.");
                using Stream stream = entry.Open();
                stream.CopyTo(Stream.Null);
            }
        }

        private static bool IsUnderDirectory(string path, string directory)
        {
            string normalizedDirectory = System.IO.Path.GetFullPath(directory)
                .TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            return path.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetUniqueArchivePath(string directory, string fileName)
        {
            string candidate = System.IO.Path.Combine(directory, fileName);
            if (!File.Exists(candidate))
                return candidate;
            return System.IO.Path.Combine(
                directory,
                $"{System.IO.Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.zip");
        }

        private static string BuildReport(
            string root,
            IReadOnlyCollection<FileInfo> files,
            long totalSize,
            string heading,
            string? archivePath)
        {
            var builder = new StringBuilder();
            builder.AppendLine(heading);
            builder.AppendLine($"Каталог: {root}");
            builder.AppendLine($"Найдено файлов: {files.Count}");
            builder.AppendLine($"Объем: {FormatSize(totalSize)}");
            if (archivePath is not null)
                builder.AppendLine($"Архив: {archivePath}");
            builder.AppendLine("Файлы:");
            foreach (FileInfo file in files)
                builder.AppendLine(System.IO.Path.GetRelativePath(root, file.FullName));
            return builder.ToString().TrimEnd();
        }

        private static string FormatSize(long bytes)
        {
            string[] units = ["Б", "КБ", "МБ", "ГБ", "ТБ"];
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return $"{value:0.##} {units[unit]}";
        }
    }
}
