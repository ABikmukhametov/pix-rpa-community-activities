using BR.Core;
using NLog;

namespace Activities.LogsCommunity
{
    public enum LogLevels
    {
        INFO,
        TRACE,
        DEBUG,
        WARN,
        ERROR
    }

    [BR.Core.Attributes.ScreenName("Записать в лог-файл")]
    [BR.Core.Attributes.Representation("Добавить запись в лог-файл")]
    [BR.Core.Attributes.Description("Добавляет запись в лог-файл и стандартный лог PIX.")]
    [BR.Core.Attributes.Path(@"Custom activities.Logs")]
    [BR.Core.Attributes.Image(typeof(WriteLog), "Activities.LogsCommunity.Icon.png")]
    public class WriteLog : Activity
    {
        [BR.Core.Attributes.ScreenName("Лог-файл:")]
        [BR.Core.Attributes.Description("Полный путь до лог-файла")]
        [BR.Core.Attributes.IsRequired]
        [BR.Core.Attributes.IsFilePathChooser]
        public required string LogFilePath { get; set; }

        [BR.Core.Attributes.ScreenName("Формат даты при логировании:")]
        [BR.Core.Attributes.Description("Формат вывода даты")]
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss zzz";

        [BR.Core.Attributes.ScreenName("Уровень логирования:")]
        [BR.Core.Attributes.Description("Уровень записи")]
        [BR.Core.Attributes.IsRequired]
        public required LogLevels LogLevel { get; set; }

        [BR.Core.Attributes.ScreenName("Кодировка файла:")]
        [BR.Core.Attributes.Description("Используемая кодировка")]
        [BR.Core.Attributes.IsRequired]
        public required AvailableEncodings EncodingForWrite { get; set; }

        [BR.Core.Attributes.ScreenName("Сообщение:")]
        [BR.Core.Attributes.Description("Записываемое сообщение")]
        [BR.Core.Attributes.IsRequired]
        public required string MessageToWrite { get; set; }

        public override void Execute(int? optionID)
        {
            string fullPath = System.IO.Path.GetFullPath(
                string.IsNullOrWhiteSpace(LogFilePath)
                    ? throw new ArgumentException("Путь к лог-файлу не указан.")
                    : LogFilePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Лог-файл не найден.", fullPath);

            string timestamp;
            try
            {
                timestamp = DateTimeOffset.Now.ToString(DateTimeFormat);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Некорректный формат даты.", nameof(DateTimeFormat), ex);
            }

            using (var writer = new StreamWriter(fullPath, true, EncodingForWrite.ToEncoding()))
                writer.WriteLine($"{timestamp}---{LogLevel}---{MessageToWrite}");

            var logger = LogManager.GetCurrentClassLogger();
            switch (LogLevel)
            {
                case LogLevels.INFO: logger.Info(MessageToWrite); break;
                case LogLevels.TRACE: logger.Trace(MessageToWrite); break;
                case LogLevels.DEBUG: logger.Debug(MessageToWrite); break;
                case LogLevels.WARN: logger.Warn(MessageToWrite); break;
                case LogLevels.ERROR: logger.Error(MessageToWrite); break;
                default: throw new ArgumentOutOfRangeException(nameof(LogLevel));
            }
        }
    }
}