using BR.Core;
using System.Text;

namespace Activities.LogsCommunity
{
    public enum AvailableEncodings
    {
        UTF8,
        Windows1251,
        UTF8BOM,
        UTF16LE,
        UTF16BE,
        UTF32,
        ASCII,
        Latin1,
        KOI8R,
        CP866
    }

    public static class EncodingExtensions
    {
        public static Encoding ToEncoding(this AvailableEncodings encoding)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return encoding switch
            {
                AvailableEncodings.UTF8 => new UTF8Encoding(false),
                AvailableEncodings.UTF8BOM => new UTF8Encoding(true),
                AvailableEncodings.UTF16LE => Encoding.Unicode,
                AvailableEncodings.UTF16BE => Encoding.BigEndianUnicode,
                AvailableEncodings.UTF32 => Encoding.UTF32,
                AvailableEncodings.Windows1251 => Encoding.GetEncoding(1251),
                AvailableEncodings.ASCII => Encoding.ASCII,
                AvailableEncodings.Latin1 => Encoding.Latin1,
                AvailableEncodings.KOI8R => Encoding.GetEncoding(20866),
                AvailableEncodings.CP866 => Encoding.GetEncoding(866),
                _ => throw new NotSupportedException("Кодировка не поддерживается.")
            };
        }
    }

    [BR.Core.Attributes.ScreenName("Создать лог-файл")]
    [BR.Core.Attributes.Representation("Создать новый лог-файл")]
    [BR.Core.Attributes.Description("Создает лог-файл в указанном каталоге.")]
    [BR.Core.Attributes.Path(@"Custom activities.Logs")]
    [BR.Core.Attributes.Image(typeof(CreateLogFile), "Activities.LogsCommunity.Icon.png")]
    public class CreateLogFile : Activity
    {
        [BR.Core.Attributes.ScreenName("Каталог для логов:")]
        [BR.Core.Attributes.Description("Целевой каталог для лог-файла")]
        [BR.Core.Attributes.IsRequired]
        [BR.Core.Attributes.IsFilePathChooser]
        public required string LogDirectory { get; set; } = "Logs";

        [BR.Core.Attributes.ScreenName("Шаблон имени файла:")]
        [BR.Core.Attributes.Description("Шаблон даты и времени; недопустимые символы заменяются на '_'")]
        public string LogFileNameTemplate { get; set; } = "yyyy-MM-dd_HH-mm-ss";

        [BR.Core.Attributes.ScreenName("Кодировка файла:")]
        [BR.Core.Attributes.Description("Используемая кодировка")]
        [BR.Core.Attributes.IsRequired]
        public required AvailableEncodings EncodingForWrite { get; set; }

        [BR.Core.Attributes.ScreenName("Путь до лог-файла:")]
        [BR.Core.Attributes.Description("Путь до созданного лог-файла")]
        [BR.Core.Attributes.IsOut]
        public required string LogFilePath { get; set; }

        public override void Execute(int? optionID)
        {
            string resolvedLogDir = System.IO.Path.GetFullPath(
                string.IsNullOrWhiteSpace(LogDirectory)
                    ? throw new ArgumentException("Каталог для логов не указан.")
                    : LogDirectory);
            Directory.CreateDirectory(resolvedLogDir);

            string fileNamePart;
            try
            {
                fileNamePart = DateTimeOffset.Now.ToString(LogFileNameTemplate);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Некорректный шаблон имени лог-файла.", nameof(LogFileNameTemplate), ex);
            }

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
                fileNamePart = fileNamePart.Replace(invalidChar, '_');

            if (string.IsNullOrWhiteSpace(fileNamePart))
                throw new ArgumentException("Шаблон имени дал пустое имя файла.", nameof(LogFileNameTemplate));

            LogFilePath = System.IO.Path.Combine(resolvedLogDir, fileNamePart + ".txt");
            using var writer = new StreamWriter(LogFilePath, false, EncodingForWrite.ToEncoding());
        }
    }
}