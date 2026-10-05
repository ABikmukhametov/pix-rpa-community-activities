using MimeKit;

namespace Activities.EmailCommunity;

[BR.Core.Attributes.ScreenName("Сохранить вложения письма в папку")]
[BR.Core.Attributes.Representation("Безопасно сохранить файловые вложения MimeMessage")]
[BR.Core.Attributes.Description(
    "Сохраняет файловые вложения в заданный каталог. Имена нормализуются, " +
    "выход за границу каталога запрещён, существующие файлы по умолчанию не перезаписываются.")]
[BR.Core.Attributes.Path("Custom activities.Email")]
[BR.Core.Attributes.Image(typeof(SaveEmailAttachmentsToFolder), "Activities.EmailCommunity.Icon.png")]
public class SaveEmailAttachmentsToFolder : BR.Core.Activity
{
    [BR.Core.Attributes.ScreenName("Письмо")]
    [BR.Core.Attributes.Description("Объект MimeMessage, содержащий вложения")]
    [BR.Core.Attributes.IsRequired]
    public required MimeMessage Message { get; set; }

    [BR.Core.Attributes.ScreenName("Каталог для вложений")]
    [BR.Core.Attributes.Description("Путь к каталогу для сохранения вложений")]
    [BR.Core.Attributes.IsRequired]
    [BR.Core.Attributes.IsFilePathChooser]
    public required string TargetFolder { get; set; }

    [BR.Core.Attributes.ScreenName("Перезаписывать существующие файлы")]
    [BR.Core.Attributes.Description("Если выключено, для существующего имени добавляется числовой суффикс")]
    public bool OverwriteExisting { get; set; }

    [BR.Core.Attributes.ScreenName("Полные пути сохранённых файлов")]
    [BR.Core.Attributes.Description("Список сохранённых вложений")]
    [BR.Core.Attributes.IsOut]
    public List<string> SavedFilePaths { get; set; } = new();

    public override void Execute(int? optionID)
    {
        SavedFilePaths = new List<string>();
        string root = System.IO.Path.GetFullPath(TargetFolder);
        Directory.CreateDirectory(root);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (MimeEntity attachment in Message.Attachments)
        {
            if (attachment is not MimePart mimePart)
            {
                continue;
            }

            string fileName = SafeFileNames.Normalize(mimePart.FileName);
            fileName = EnsureUniqueWithinMessage(fileName, usedNames);
            string filePath = SafeFileNames.GetAvailablePath(root, fileName, OverwriteExisting);
            FileMode mode = OverwriteExisting ? FileMode.Create : FileMode.CreateNew;
            using FileStream stream = new(filePath, mode, FileAccess.Write, FileShare.None);
            (mimePart.Content ?? throw new InvalidDataException("Вложение не содержит MIME-контент."))
                .DecodeTo(stream);
            SavedFilePaths.Add(filePath);
        }
    }

    private static string EnsureUniqueWithinMessage(string fileName, HashSet<string> usedNames)
    {
        if (usedNames.Add(fileName))
        {
            return fileName;
        }

        string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string extension = System.IO.Path.GetExtension(fileName);
        for (int index = 1; index < int.MaxValue; index++)
        {
            string candidate = $"{baseName} ({index}){extension}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }
        throw new IOException("Не удалось подобрать уникальное имя вложения.");
    }
}
