namespace Activities.EmailCommunity;

internal static class SafeFileNames
{
    private static readonly HashSet<string> ReservedNames = new(
        new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        },
        StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? suppliedName)
    {
        string fileName = System.IO.Path.GetFileName(suppliedName?.Trim()) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName is "." or ".." ||
            fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 ||
            ReservedNames.Contains(System.IO.Path.GetFileNameWithoutExtension(fileName)))
        {
            throw new InvalidDataException("Вложение содержит недопустимое имя файла.");
        }
        return fileName;
    }

    public static string ResolvePath(string rootDirectory, string fileName)
    {
        string root = System.IO.Path.GetFullPath(rootDirectory);
        string candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, fileName));
        string boundary = root.EndsWith(System.IO.Path.DirectorySeparatorChar)
            ? root
            : root + System.IO.Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(boundary, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Имя вложения выходит за границу целевого каталога.");
        }
        return candidate;
    }

    public static string GetAvailablePath(string rootDirectory, string fileName, bool overwrite)
    {
        string candidate = ResolvePath(rootDirectory, fileName);
        if (overwrite || !File.Exists(candidate))
        {
            return candidate;
        }

        string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string extension = System.IO.Path.GetExtension(fileName);
        for (int index = 1; index < int.MaxValue; index++)
        {
            candidate = ResolvePath(rootDirectory, $"{baseName} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Не удалось подобрать уникальное имя вложения.");
    }
}
