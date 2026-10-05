#pragma warning disable CS8600, CS8602, CS8604

using BR.Core;
using BR.Core.Attributes;
using System.IO;
using System.Text;

namespace Activities.ExcelCommunity
{
    [BR.Core.Attributes.ScreenName("Установить высоту строк, ширину столбцов")]
    [BR.Core.Attributes.Representation("Настроить высоту строк и ширину столбцов")]
    [BR.Core.Attributes.Description("Настроить высоту строк и ширину столбцов")]
    [BR.Core.Attributes.Path(@"Custom activities.Excel")]
    [BR.Core.Attributes.Image(typeof(SetHeightWidth), "Activities.ExcelCommunity.Icon.png")]
    public class SetHeightWidth : Activity
    {
        [BR.Core.Attributes.ScreenName("Путь до Excel-файла:")]
        [BR.Core.Attributes.Description("Путь до Excel-файла")]
        [BR.Core.Attributes.IsFilePathChooser(true)]
        [BR.Core.Attributes.IsRequired]
        public required string ExcelPath { get; set; }

        [BR.Core.Attributes.ScreenName("Имя листа:")]
        [BR.Core.Attributes.Description("Укажите наименование листа, для которого будет выполнена обработка")]
        [BR.Core.Attributes.IsRequired]
        public required string SheetName { get; set; }

        [BR.Core.Attributes.ScreenName("Наименования столбцов:")]
        [BR.Core.Attributes.Description("Укажите список наименований столбцов, для которых будет выполнена обработка\n" +
                                        "Ищем заголовки в первой строке. Не найденные заголовки игнорируются без ошибки.")]
        [BR.Core.Attributes.IsRequired]
        public required List<string> ExcelColumnNames { get; set; }

        [BR.Core.Attributes.ScreenName("Ширина столбца:")]
        [BR.Core.Attributes.Description("Данная ширина столбца будет указана для каждого столбца из предыдущего списка")]
        [BR.Core.Attributes.IsRequired]
        public required double СolumnWidth { get; set; } = 30.0;

        [BR.Core.Attributes.ScreenName("Высота строки:")]
        [BR.Core.Attributes.Description("Данная высота строк будет установлена для всего используемого диапазона листа")]
        [BR.Core.Attributes.IsRequired]
        public required double RowHeight { get; set; } = 15.0;

        public override void Execute(int? optionID)
        {
            var ExcelApp = new Microsoft.Office.Interop.Excel.Application();
            ExcelApp.Visible = false;

            string ExcelPathFull = System.IO.Path.GetFullPath(ExcelPath);

            try
            {
                if (!System.IO.File.Exists(ExcelPathFull))
                {
                    throw new FileNotFoundException($"Файл не найден: {ExcelPathFull}");
                }

                var ExcelWorkbook  = ExcelApp.Workbooks.Open(ExcelPathFull);
                var ExcelWorksheet = ExcelWorkbook.Sheets
                                                .Cast<Microsoft.Office.Interop.Excel.Worksheet>()
                                                .FirstOrDefault(s => s.Name.Equals(SheetName, StringComparison.OrdinalIgnoreCase));

                if (ExcelWorksheet == null)
                {
                    throw new Exception($"Лист не найден: {SheetName}");
                }

                var UsedRange = ExcelWorksheet.UsedRange;
                // Ищем заголовки в первой строке
                Microsoft.Office.Interop.Excel.Range headerRange = (Microsoft.Office.Interop.Excel.Range)UsedRange.Rows[1];

                UsedRange.Rows.RowHeight = RowHeight;

                foreach (string column in ExcelColumnNames)
                {
                    Microsoft.Office.Interop.Excel.Range targetHeader = null;
                        //Поиск нужного столбца по заголовку
                        foreach (Microsoft.Office.Interop.Excel.Range cell in headerRange.Cells)
                        {
                            if (cell.Value2?.ToString().Trim().Equals(column, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                targetHeader = cell;
                                break;
                            }
                        }

                        if (targetHeader != null)
                        {
                            // Получаем номер столбца
                            int columnNumber = targetHeader.Column;

                            // Устанавливаем ширину
                            Microsoft.Office.Interop.Excel.Range columnRange = (Microsoft.Office.Interop.Excel.Range)ExcelWorksheet.Columns[columnNumber];
                            columnRange.ColumnWidth = СolumnWidth;

                            // Освобождаем ресурсы
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(columnRange);
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(targetHeader);
                        }
                }
                ExcelWorkbook.Save();
                ExcelWorkbook.Close();

                System.Runtime.InteropServices.Marshal.ReleaseComObject(UsedRange);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(ExcelWorksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(ExcelWorkbook);
            }
            finally
            {
                ExcelApp.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(ExcelApp);
            }
        }
    }
}
