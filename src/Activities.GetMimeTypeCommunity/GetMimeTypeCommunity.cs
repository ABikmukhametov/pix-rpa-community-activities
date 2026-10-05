using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using BR.Core;
using BR.Core.Attributes;

namespace Activities.GetMimeTypeCommunity
{
    [BR.Core.Attributes.ScreenName("Получить MIME-type файла")]
    [BR.Core.Attributes.Representation("Получить MIME-type файла")]
    [BR.Core.Attributes.Description("Получить MIME для текущего файла. Не анализирует сигнатуры файла, определение по расширению.")]
    [BR.Core.Attributes.Image(typeof(GetMimeTypeCommunity), "Activities.GetMimeTypeCommunity.Icon.png")]
    [BR.Core.Attributes.Path("Custom activities.HTTP")]

    public class GetMimeTypeCommunity : BR.Core.Activity
    {
        private static readonly IReadOnlyDictionary<string, string> Types =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Текстовые форматы
            { "txt", "text/plain" },
            { "html", "text/html" },
            { "htm", "text/html" },
            { "css", "text/css" },
            { "csv", "text/csv" },

            // Изображения
            { "png", "image/png" },
            { "jpg", "image/jpeg" },
            { "jpeg", "image/jpeg" },
            { "gif", "image/gif" },
            { "webp", "image/webp" },
            { "svg", "image/svg+xml" },
            { "bmp", "image/bmp" },
            { "ico", "image/x-icon" },

            // Аудио/Видео
            { "mp3", "audio/mpeg" },
            { "wav", "audio/wav" },
            { "mp4", "video/mp4" },
            { "webm", "video/webm" },
            { "ogg", "audio/ogg" },
            { "avi", "video/x-msvideo" },
            { "mov", "video/quicktime" },

            // Архивы
            { "zip", "application/zip" },
            { "gz", "application/gzip" },
            { "tar", "application/x-tar" },
            { "7z", "application/x-7z-compressed" },
            { "rar", "application/vnd.rar" },

            // Документы
            { "pdf", "application/pdf" },
            { "doc", "application/msword" },
            { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { "xls", "application/vnd.ms-excel" },
            { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { "ppt", "application/vnd.ms-powerpoint" },
            { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

            // Прочие
            { "json", "application/json" },
            { "xml", "application/xml" },
            { "js", "text/javascript" },
            { "wasm", "application/wasm" },
            { "exe", "application/x-msdownload" },
            { "apk", "application/vnd.android.package-archive" },
            { "ttf", "font/ttf" },
            { "woff", "font/woff" },
            { "woff2", "font/woff2" }
        });

        [BR.Core.Attributes.ScreenName("Путь до файла:")]
        [BR.Core.Attributes.Description("Укажите полный путь до файла")]
        [BR.Core.Attributes.IsFilePathChooser]
        [BR.Core.Attributes.IsRequired]
        public string Full_file_path { get; set; } = string.Empty;

        [BR.Core.Attributes.ScreenName("Mime-type:")]
        [BR.Core.Attributes.Description("Mime-type файла")]
        [BR.Core.Attributes.IsOut]
        public string Mime_type { get; set; } = string.Empty;

        public override void Execute(int? optionID)
        {
            Mime_type = string.Empty;
            if (string.IsNullOrWhiteSpace(Full_file_path))
            {
                throw new ArgumentException("Путь или имя файла не указано.", nameof(Full_file_path));
            }

            string extension = System.IO.Path.GetExtension(Full_file_path).TrimStart('.');
            Mime_type = Types.TryGetValue(extension, out string? mimeType)
                ? mimeType
                : "application/octet-stream";
        }
    }
}
