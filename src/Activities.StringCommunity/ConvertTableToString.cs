using BR.Core;
using BR.Core.Attributes;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace Activities.StringCommunity
{
    [BR.Core.Attributes.ScreenName("Форматирование таблицы с переносами")]
    [BR.Core.Attributes.Representation("Преобразование DataTable в текст с поддержкой многострочных ячеек")]
    [BR.Core.Attributes.Description("Форматирует таблицу с сохранением структуры даже при наличии многострочного текста в ячейках")]
    [BR.Core.Attributes.Path(@"Custom activities.Строки")]
    [BR.Core.Attributes.OptionList(new[] { "String", "HTML" })]
    [BR.Core.Attributes.Image(typeof(ConvertTableToString), "Activities.StringCommunity.Icon.png")]
    public class ConvertTableToString : BR.Core.Activity
    {
        [BR.Core.Attributes.ScreenName("Исходная таблица")]
        [BR.Core.Attributes.Description("DataTable для преобразования в форматированную строку")]
        [BR.Core.Attributes.IsRequired]
        public required DataTable InputTable { get; set; }

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Стиль заголовков")]
        [BR.Core.Attributes.Description("CSS-стиль для заголовков")]
        public string? HtmlHeaderStyle { get; set; } = "background: #111318; color: #ffffff;";

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Стиль четных строк")]
        [BR.Core.Attributes.Description("CSS-стиль для четных строк")]
        public string HtmlEvenRowStyle { get; set; } = "background: #9fb5df;";

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Стиль нечетных строк")]
        [BR.Core.Attributes.Description("CSS-стиль для нечетных строк")]
        public string HtmlOddRowStyle { get; set; } = "background: #dcdfe5;";

        [BR.Core.Attributes.Options(new[] { 0 })]
        [BR.Core.Attributes.ScreenName("Результат форматирования")]
        [BR.Core.Attributes.Description("Отформатированная строка с данными таблицы")]
        [BR.Core.Attributes.IsOut]
        public string FormattedResult { get; set; } = string.Empty;

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Заголовок таблицы")]
        [BR.Core.Attributes.Description("Текст для элемента <caption>")]
        public string? HtmlTableCaption { get; set; } = "Результат обработки RPA";

        [BR.Core.Attributes.Options(new[] { 1 })]
        [BR.Core.Attributes.ScreenName("Результат форматирования")]
        [BR.Core.Attributes.Description("Отформатированная HTML-строка с данными таблицы")]
        [BR.Core.Attributes.IsOut]
        public string FormattedHTMLResult { get; set; } = string.Empty;

        public override void Execute(int? optionID)
        {
            FormattedResult = string.Empty;
            FormattedHTMLResult = string.Empty;
            if (InputTable == null || InputTable.Columns.Count == 0)
            {
                throw new ArgumentException("Некорректная входная таблица");
            }

            if (optionID == 0)
            {
                FormattedResult = FormatDataTableWithAlignment(InputTable, " | ");
                return;
            }
            if (optionID == 1)
            {
                FormattedHTMLResult = FormatAsHtmlTable(InputTable);
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(optionID), "Неизвестный режим форматирования.");
        }

        private string FormatDataTableWithAlignment(DataTable table, string separator)
        {
            var columnWidths = CalculateColumnWidths(table);
            var sb = new StringBuilder();

            // Заголовки
            sb.AppendLine(FormatRow(table.Columns.Cast<DataColumn>().Select(c => c.ColumnName), columnWidths, separator));

            // Разделитель
            sb.AppendLine(CreateSeparator(columnWidths, separator));

            // Данные
            foreach (DataRow row in table.Rows)
            {
                AppendDataRow(sb, row, columnWidths, separator);
            }

            return sb.ToString();
        }

        private void AppendDataRow(StringBuilder sb, DataRow row, int[] widths, string separator)
        {
            // Разбиваем каждую ячейку на строки
            var cellLines = row.ItemArray
                .Select((item, index) => new {
                    Lines = SplitCellContent(ConvertItemToString(item, "")),
                    Width = widths[index]
                })
                .ToList();

            // Определяем максимальное количество строк в ячейках этой строки
            var maxLines = cellLines.Max(c => c.Lines.Count);

            // Формируем каждую линию многострочной ячейки
            for (int lineNumber = 0; lineNumber < maxLines; lineNumber++)
            {
                var lineParts = cellLines.Select((cell, index) =>
                    GetCellLine(cell.Lines, lineNumber, widths[index]));

                sb.AppendLine(string.Join(separator, lineParts));
            }
        }

        private List<string> SplitCellContent(string content)
        {
            return Regex.Split(content, @"\r\n|\r|\n").ToList();
        }

        private string GetCellLine(List<string> lines, int lineNumber, int width)
        {
            // Если строка существует - возвращаем её с выравниванием
            if (lineNumber < lines.Count)
                return lines[lineNumber].PadRight(width);

            // Если строк меньше - возвращаем пустое место
            return new string(' ', width);
        }

        private int[] CalculateColumnWidths(DataTable table)
        {
            return table.Columns.Cast<DataColumn>()
                .Select(col =>
                {
                    // Анализируем данные
                    var maxDataLength = 0;
                    var maxValueExample = string.Empty;

                    foreach (DataRow row in table.Rows)
                    {
                        var cellValue = ConvertItemToString(row[col], "");
                        var cellLength = cellValue.Length;

                        // Для многострочных значений находим самую длинную строку
                        var lines = SplitCellContent(cellValue);
                        if (lines.Any())
                        {
                            cellLength = lines.Max(l => l.Length);
                        }

                        if (cellLength > maxDataLength)
                        {
                            maxDataLength = cellLength;
                            maxValueExample = cellValue.Trim(); // Обрезаем для удобства просмотра
                        }
                    }

                    return Math.Max(col.ColumnName.Length, maxDataLength);
                })
                .ToArray();
        }

        private static string ConvertItemToString(object? item, string nullReplacement)
        {
            if (item == null || item == DBNull.Value)
                return nullReplacement; // Будет возвращена пустая строка

            return item.ToString() ?? nullReplacement;
        }

        private string FormatRow(IEnumerable<string> values, int[] widths, string separator)
        {
            return string.Join(separator, values
                                                .Select((value, index) => value.PadRight(widths[index])));
        }

        private string CreateSeparator(int[] widths, string separator)
        {
            var separatorChar = separator.Contains('-') ? '-' : '-';
            return string.Join(separator.Replace(' ', '-'),
                widths.Select(w => new string(separatorChar, w)));
        }

        private string FormatAsHtmlTable(DataTable table)
        {
            const string HtmlTableClass = "pretty-table";
            string headerStyle = ValidateCss(HtmlHeaderStyle);
            string evenStyle = ValidateCss(HtmlEvenRowStyle);
            string oddStyle = ValidateCss(HtmlOddRowStyle);

            var sb = new StringBuilder();
            sb.Append($"<table class='{HtmlTableClass}'>");

            // Добавляем заголовок таблицы
            if (!string.IsNullOrEmpty(HtmlTableCaption))
            {
                sb.Append($"<caption>{WebUtility.HtmlEncode(HtmlTableCaption)}</caption>");
            }

            sb.Append("<thead><tr>");
            foreach (DataColumn col in table.Columns)
            {
                var style = $" style='{headerStyle}'";
                sb.Append($"<th{style}>{WebUtility.HtmlEncode(col.ColumnName)}</th>");
            }
            sb.Append("</tr></thead>");

            // Тело таблицы
            sb.Append("<tbody>");
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var rowStyle = i % 2 == 0 ? evenStyle : oddStyle;

                sb.Append($"<tr style='{rowStyle}'>");
                foreach (var item in table.Rows[i].ItemArray)
                {
                    sb.Append("<td>");
                    sb.Append(WebUtility.HtmlEncode(ConvertItemToString(item, string.Empty))
                        .Replace("\r\n", "<br>")
                        .Replace("\r", "<br>")
                        .Replace("\n", "<br>"));
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");

            return sb.ToString();
        }

        private static string ValidateCss(string? css)
        {
            string value = css?.Trim() ?? string.Empty;
            if (value.IndexOfAny(new[] { '<', '>', '\'', '"', '\r', '\n' }) >= 0)
            {
                throw new ArgumentException("CSS-стиль содержит недопустимые символы.");
            }
            return value;
        }
    }
}
