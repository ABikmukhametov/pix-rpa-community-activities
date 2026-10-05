#pragma warning disable CS8600, CS8602, CS8604

using BR.Core;
using BR.Core.Attributes;
using Microsoft.Office.Interop.Excel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Activities.ExcelCommunity
{
    [BR.Core.Attributes.ScreenName("Закрасить лист по формуле")]
    [BR.Core.Attributes.Representation("Закраска диапазона по формуле")]
    [BR.Core.Attributes.Description("Закраска указанного диапазона листа по формуле")]
    [BR.Core.Attributes.Path(@"Custom activities.Excel")]
    [BR.Core.Attributes.Image(typeof(ColorTheRangeByFormula), "Activities.ExcelCommunity.Icon.png")]
    public class ColorTheRangeByFormula : Activity
    {
        [BR.Core.Attributes.ScreenName("Путь до Excel-файла:")]
        [BR.Core.Attributes.Description("Путь до Excel-файла")]
        [BR.Core.Attributes.IsFilePathChooser(true)]
        [BR.Core.Attributes.IsRequired]
        public required string ExcelPath { get; set; }

        [BR.Core.Attributes.ScreenName("Имя листа:")]
        [BR.Core.Attributes.Description("Укажите наименование листа, для которого будет выполнена обработка")]
        [BR.Core.Attributes.IsRequired]
        public required string SheetName { get; set; } = "Лист1";

        [BR.Core.Attributes.ScreenName("Диапазон:")]
        [BR.Core.Attributes.Description("Форматируемый диапазон в формате Excel (пример: A2:D10).\n\n" +
                                        "◈ Если указан: формула применяется ТОЛЬКО к указанным ячейкам\n" +
                                        "◈ Если пусто: автоматически выбираются все данные на листе:\n" +
                                        "  - Начиная со строки 2 (предполагается заголовок)\n" +
                                        "  - До последней заполненной ячейки\n\n" +
                                        "ВАЖНО: При указании диапазона проверяйте поведение в Excel, а после - переносите в активность.")]
        public string? UserRange { get; set; } = "А2:H222";

        [BR.Core.Attributes.ScreenName("Удалить форматирование с листа:")]
        [BR.Core.Attributes.Description("Флаг определяет будут ли удалены текущие настройки форматирования с листа")]
        [BR.Core.Attributes.IsRequired]
        public bool ClearExistingFormats { get; set; } = false;

        [BR.Core.Attributes.ScreenName("Формула:")]
        [BR.Core.Attributes.Description("Формула, которая будет применена к листу.\n\n" +
                                        "ВАЖНО: Разница в синтаксисе формул между русской и английской версиями Excel связана с региональными настройками системы.")]
        [BR.Core.Attributes.IsRequired]
        public required string Formula { get; set; } = @"=И($A2=\""Да\"";$K2=\""Казань\"")";

        [BR.Core.Attributes.ScreenName("Цвет (HEX):")]
        [BR.Core.Attributes.Description("HEX-код цвета в формате #RRGGBB")]
        [BR.Core.Attributes.IsRequired]
        public required string HexColor { get; set; } = "#7ac1d6";


        public override void Execute(int? optionID)
        {
            Application excelApp = null;
            Workbook workbook = null;
            Worksheet worksheet = null;
            Microsoft.Office.Interop.Excel.Range targetRange = null;

            try
            {
                string ExcelPathFull = System.IO.Path.GetFullPath(ExcelPath);

                // Инициализация Excel
                excelApp = new Application { Visible = false };
                ConfigureExcelPerformance(excelApp);

                // Открытие файла
                workbook = OpenWorkbook(excelApp, ExcelPathFull);
                worksheet = GetWorksheet(workbook, SheetName);

                // Поиск данных
                var dataRange = PrepareDataRange(worksheet);

                // Применение форматирования
                ApplyConditions(dataRange, worksheet);

                SaveAndClose(workbook, excelApp);
            }
            finally
            {
                Cleanup(excelApp, workbook, worksheet, targetRange);
            }
        }

        private Microsoft.Office.Interop.Excel.Range PrepareDataRange(Worksheet worksheet)
        {
            Microsoft.Office.Interop.Excel.Range targetRange;

            if (!string.IsNullOrWhiteSpace(UserRange))
            {
                try
                {
                    targetRange = worksheet.Range[UserRange];
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Некорректный диапазон: {UserRange}. Пример правильного формата: A1:D10", ex);
                }
            }
            else
            {
                var usedRange = worksheet.UsedRange;
                var lastCell = usedRange.SpecialCells(XlCellType.xlCellTypeLastCell);
                targetRange = worksheet.Range[
                    worksheet.Cells[2, 1],
                    worksheet.Cells[lastCell.Row, lastCell.Column]
                ];
            }

            if (targetRange == null || targetRange.Cells.Count == 0)
                throw new InvalidOperationException("Указанный диапазон не содержит данных");

            return targetRange;
        }

        private void ConfigureExcelPerformance(Application excelApp)
        {
            excelApp.ScreenUpdating = false;
            excelApp.EnableEvents = false;
            excelApp.DisplayAlerts = false;
        }

        private Workbook OpenWorkbook(Application excelApp, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл Excel не найден", path);

            return excelApp.Workbooks.Open(System.IO.Path.GetFullPath(path));
        }

        private Worksheet GetWorksheet(Workbook workbook, string sheetName)
        {
            var worksheet = workbook.Sheets[sheetName] as Worksheet;
            return worksheet ?? throw new ArgumentException($"Лист '{sheetName}' не найден");
        }

        private void ApplyConditions(Microsoft.Office.Interop.Excel.Range dataRange, Worksheet worksheet)
        {
            if (ClearExistingFormats)
                worksheet.UsedRange.FormatConditions.Delete();

            Microsoft.Office.Interop.Excel.FormatCondition Condition = (Microsoft.Office.Interop.Excel.FormatCondition)dataRange.FormatConditions.Add(Microsoft.Office.Interop.Excel.XlFormatConditionType.xlExpression,
                                             Formula1: Formula.Replace("\n", "")
                                            );
            // Конвертация HEX в RGB
            Condition.Interior.Color = HexToRgb(HexColor);
        }

        private void SaveAndClose(Workbook workbook, Application excelApp)
        {
            workbook.Save();
            excelApp.Quit();
        }

        private void Cleanup(Application excelApp, Workbook workbook, Worksheet worksheet, Microsoft.Office.Interop.Excel.Range targetRange)
        {
            try
            {
                // Освобождаем все COM-объекты в обратном порядке создания
                if (targetRange != null)
                {
                    Marshal.FinalReleaseComObject(targetRange);
                    targetRange = null;
                }

                if (worksheet != null)
                {
                    Marshal.FinalReleaseComObject(worksheet);
                    worksheet = null;
                }

                if (workbook != null)
                {
                    workbook.Close(SaveChanges: false);
                    Marshal.FinalReleaseComObject(workbook);
                    workbook = null;
                }

                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.FinalReleaseComObject(excelApp);
                    excelApp = null;
                }

                // Принудительная очистка памяти
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { /* Игнорируем ошибки при очистке */ }
        }
        private System.Drawing.Color HexToRgb(string hexColor)
        {
            try
            {
                // Удаляем решетку если есть
                hexColor = hexColor.Trim().Replace("#", "");

                // Проверяем формат
                if (!Regex.IsMatch(hexColor, @"^[0-9a-fA-F]{6}$"))
                    throw new ArgumentException("Некорректный формат HEX цвета");

                // Конвертируем компоненты
                int red = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                int green = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                int blue = Convert.ToInt32(hexColor.Substring(4, 2), 16);

                // Excel использует BGR (не RGB!) в формате OLE
                return System.Drawing.Color.FromArgb(red, green, blue);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Ошибка конвертации цвета: {hexColor}. " +
                    "Используйте формат #RRGGBB (пример: #FF5733)", ex);
            }
        }

    }
}
