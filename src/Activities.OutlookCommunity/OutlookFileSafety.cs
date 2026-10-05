namespace Activities.OutlookCommunity;

internal static class OutlookFileSafety
{
    internal static string ValidateFileName(string? fileName)
    {
        string name = System.IO.Path.GetFileName(fileName) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || name != fileName)
            throw new InvalidDataException("Вложение Outlook содержит небезопасное имя.");
        if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException($"Имя вложения '{name}' содержит недопустимые символы.");

        string stem = System.IO.Path.GetFileNameWithoutExtension(name).TrimEnd('.', ' ');
        string[] reserved = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];
        if (reserved.Contains(stem, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException($"Имя вложения '{name}' зарезервировано Windows.");
        return name;
    }

    internal static string UniquePath(string directory, string fileName)
    {
        string root = System.IO.Path.GetFullPath(directory);
        string candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, ValidateFileName(fileName)));
        EnsureUnderRoot(candidate, root);
        if (!File.Exists(candidate))
            return candidate;

        string stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string extension = System.IO.Path.GetExtension(fileName);
        for (int index = 1; ; index++)
        {
            candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, $"{stem}_{index}{extension}"));
            EnsureUnderRoot(candidate, root);
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    private static void EnsureUnderRoot(string path, string root)
    {
        string prefix = root.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Путь выходит за пределы временного каталога.");
    }
}
