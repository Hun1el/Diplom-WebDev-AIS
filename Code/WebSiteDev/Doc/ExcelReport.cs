using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebSiteDev
{
    public class ExcelReport
    {
        public static void ExportToExcel(DataGridView DataGridView, List<decimal> OrderCosts, DateTime DateFrom, DateTime DateTo, string SearchText, string SelectedStatus, string SelectedSort)
        {
            dynamic ExcelApp = null;
            dynamic workbook = null;
            dynamic worksheet = null;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF (*.pdf)|*.pdf|Excel 2007-2026 (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.Title = "Сохранить отчёт по заказам";
            saveFileDialog.FileName = "Отчет_заказы_" + DateFrom.ToString("dd.MM.yyyy") + "-" + DateTo.ToString("dd.MM.yyyy");

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                Type ExcelAppType = Type.GetTypeFromProgID("Excel.Application");
                ExcelApp = Activator.CreateInstance(ExcelAppType);
                ExcelApp.Visible = false;

                workbook = ExcelApp.Workbooks.Add();
                worksheet = workbook.ActiveSheet;
                worksheet.Name = "Отчет";
                worksheet.Cells.Font.Name = "Calibri";
                worksheet.Cells.Font.Size = 11;

                worksheet.PageSetup.TopMargin = 10;
                worksheet.PageSetup.BottomMargin = 10;
                worksheet.PageSetup.LeftMargin = 10;
                worksheet.PageSetup.RightMargin = 10;
                worksheet.PageSetup.HeaderMargin = 5;
                worksheet.PageSetup.FooterMargin = 5;

                int CurrentRow = 1;

                // ==================== ЗАГОЛОВОК ====================
                worksheet.Cells[CurrentRow, 1].Value = "ОТЧЁТ ПО ЗАКАЗАМ";
                worksheet.Cells[CurrentRow, 1].Font.Bold = true;
                worksheet.Cells[CurrentRow, 1].Font.Size = 16;
                worksheet.Cells[CurrentRow, 1].Font.Color = Color.White;
                worksheet.Cells[CurrentRow, 1].Interior.Color = Color.FromArgb(45, 156, 219);
                worksheet.Range[worksheet.Cells[CurrentRow, 1], worksheet.Cells[CurrentRow, 8]].Merge();
                worksheet.Range[worksheet.Cells[CurrentRow, 1], worksheet.Cells[CurrentRow, 8]].HorizontalAlignment = HAlignCenter();
                worksheet.Range[worksheet.Cells[CurrentRow, 1], worksheet.Cells[CurrentRow, 8]].VerticalAlignment = VAlignCenter();
                worksheet.Rows[CurrentRow].RowHeight = 28;
                CurrentRow = CurrentRow + 1;

                // ==================== ИНФОРМАЦИЯ ОБ ОТЧЁТЕ ====================
                string PeriodStr = DateFrom.ToString("dd.MM.yyyy") + " - " + DateTo.ToString("dd.MM.yyyy");
                worksheet.Cells[CurrentRow, 1].Value = "Период: " + PeriodStr;
                worksheet.Cells[CurrentRow, 1].WrapText = false;
                worksheet.Cells[CurrentRow, 1].Font.Bold = true;
                worksheet.Cells[CurrentRow, 1].Font.Size = 11;
                worksheet.Cells[CurrentRow, 6].Value = "Дата создания: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[CurrentRow, 6].Font.Size = 11;
                CurrentRow = CurrentRow + 1;

                // ==================== ФИЛЬТРЫ ====================
                bool HasFilters = false;
                string FiltersText = "";

                if (SearchText != "")
                {
                    FiltersText = FiltersText + "Поиск: " + SearchText + " | ";
                    HasFilters = true;
                }

                if (SelectedStatus != "Статус не выбран" && SelectedStatus != "")
                {
                    FiltersText = FiltersText + "Статус: " + SelectedStatus + " | ";
                    HasFilters = true;
                }

                if (SelectedSort != "Сортировка не выбрана" && SelectedSort != "")
                {
                    FiltersText = FiltersText + "Сортировка: " + SelectedSort;
                    HasFilters = true;
                }

                if (!HasFilters)
                {
                    FiltersText = "Все данные за выбранный период";
                }
                else if (FiltersText.EndsWith(" | "))
                {
                    FiltersText = FiltersText.Substring(0, FiltersText.Length - 3);
                }

                worksheet.Cells[CurrentRow, 1].Value = FiltersText;
                worksheet.Cells[CurrentRow, 1].Font.Size = 10;
                worksheet.Cells[CurrentRow, 1].Font.Italic = true;
                worksheet.Range[worksheet.Cells[CurrentRow, 1], worksheet.Cells[CurrentRow, 8]].WrapText = false;
                worksheet.Range[worksheet.Cells[CurrentRow, 1], worksheet.Cells[CurrentRow, 8]].Merge();
                CurrentRow = CurrentRow + 1;
                CurrentRow = CurrentRow + 1;

                // ==================== ЗАГОЛОВОК ТАБЛИЦЫ ====================
                int HeaderRow = CurrentRow;
                worksheet.Cells[HeaderRow, 1].Value = "№";
                worksheet.Cells[HeaderRow, 2].Value = "Клиент";
                worksheet.Cells[HeaderRow, 3].Value = "Сотрудник";
                worksheet.Cells[HeaderRow, 4].Value = "Дата";
                worksheet.Cells[HeaderRow, 5].Value = "Срок";
                worksheet.Cells[HeaderRow, 6].Value = "Состав заказа";
                worksheet.Cells[HeaderRow, 7].Value = "Статус";
                worksheet.Cells[HeaderRow, 8].Value = "Сумма, руб.";

                dynamic HeaderRange = worksheet.Range[worksheet.Cells[HeaderRow, 1], worksheet.Cells[HeaderRow, 8]];
                HeaderRange.Font.Bold = true;
                HeaderRange.Font.Size = 11;
                HeaderRange.Font.Color = Color.White;
                HeaderRange.Interior.Color = Color.FromArgb(45, 156, 219);
                HeaderRange.HorizontalAlignment = HAlignCenter();
                HeaderRange.VerticalAlignment = VAlignCenter();
                HeaderRange.WrapText = true;
                worksheet.Rows[HeaderRow].RowHeight = 24;

                CurrentRow = CurrentRow + 1;
                int DataStartRow = CurrentRow;

                decimal TotalSum = 0;
                int NewCount = 0;
                int InProgressCount = 0;
                int CompletedCount = 0;
                int CancelledCount = 0;

                // Данные для графика по месяцам
                Dictionary<string, decimal> MonthlySales = new Dictionary<string, decimal>();
                Dictionary<string, int> MonthlyOrderCount = new Dictionary<string, int>();
                Dictionary<string, string> MonthlyLabels = new Dictionary<string, string>();

                // Предзаполняем все месяцы диапазона нулями
                DateTime fillMonth = new DateTime(DateFrom.Year, DateFrom.Month, 1);
                DateTime endMonth = new DateTime(DateTo.Year, DateTo.Month, 1);

                while (fillMonth <= endMonth)
                {
                    string key = fillMonth.Year.ToString() + "-" + fillMonth.Month.ToString("D2");
                    string label = GetMonthName(fillMonth.Month) + " " + fillMonth.Year;

                    if (!MonthlySales.ContainsKey(key))
                    {
                        MonthlySales[key] = 0;
                        MonthlyOrderCount[key] = 0;
                        MonthlyLabels[key] = label;
                    }
                    fillMonth = fillMonth.AddMonths(1);
                }

                // ==================== ЗАПОЛНЕНИЕ ДАННЫХ ====================
                for (int i = 0; i < DataGridView.Rows.Count; i++)
                {
                    string OrderID = "";
                    object OrderIDObj = DataGridView.Rows[i].Cells["OrderID"].Value;

                    if (OrderIDObj != null)
                    {
                        OrderID = OrderIDObj.ToString();
                    }

                    string ClientName = "";
                    object ClientNameObj = DataGridView.Rows[i].Cells["ClientName"].Value;

                    if (ClientNameObj != null)
                    {
                        ClientName = ClientNameObj.ToString();
                    }

                    string UserName = "";
                    object UserNameObj = DataGridView.Rows[i].Cells["UserName"].Value;

                    if (UserNameObj != null)
                    {
                        UserName = UserNameObj.ToString();
                    }

                    string OrderDate = "";
                    object OrderDateObj = DataGridView.Rows[i].Cells["OrderDate"].Value;

                    if (OrderDateObj != null)
                    {
                        DateTime dt = Convert.ToDateTime(OrderDateObj);
                        OrderDate = dt.ToString("dd.MM.yy");

                        string MonthKey = dt.Year.ToString() + "-" + dt.Month.ToString("D2");

                        if (!MonthlySales.ContainsKey(MonthKey))
                        {
                            MonthlySales[MonthKey] = 0;
                            MonthlyOrderCount[MonthKey] = 0;
                            MonthlyLabels[MonthKey] = GetMonthName(dt.Month) + " " + dt.Year;
                        }

                        decimal CostVal = 0;
                        object CostForMonth = DataGridView.Rows[i].Cells["OrderCost"].Value;

                        if (CostForMonth != null)
                        {
                            decimal.TryParse(CostForMonth.ToString(), out CostVal);
                        }

                        MonthlySales[MonthKey] = MonthlySales[MonthKey] + CostVal;
                        MonthlyOrderCount[MonthKey] = MonthlyOrderCount[MonthKey] + 1;
                    }

                    string CompDate = "";
                    object CompDateObj = DataGridView.Rows[i].Cells["OrderCompDate"].Value;

                    if (CompDateObj != null)
                    {
                        DateTime dt = Convert.ToDateTime(CompDateObj);
                        CompDate = dt.ToString("dd.MM.yy");
                    }

                    string ProductName = "";
                    object ProductNameObj = DataGridView.Rows[i].Cells["ProductName"].Value;

                    if (ProductNameObj != null)
                    {
                        ProductName = ProductNameObj.ToString();
                    }

                    if (ProductName != "")
                    {
                        string[] products = ProductName.Split(new string[] { ", " }, StringSplitOptions.None);
                        ProductName = string.Join("\n", products);
                    }

                    string StatusName = "";
                    object StatusNameObj = DataGridView.Rows[i].Cells["StatusName"].Value;

                    if (StatusNameObj != null)
                    {
                        StatusName = StatusNameObj.ToString();
                    }

                    string OrderCostStr = "0";
                    object OrderCostObj = DataGridView.Rows[i].Cells["OrderCost"].Value;

                    if (OrderCostObj != null)
                    {
                        OrderCostStr = OrderCostObj.ToString();
                    }

                    decimal OrderCost = 0;
                    bool Parsed = decimal.TryParse(OrderCostStr, out OrderCost);

                    if (Parsed)
                    {
                        TotalSum = TotalSum + OrderCost;
                    }

                    if (StatusName == "Новый")
                    {
                        NewCount++;
                    }

                    if (StatusName == "В работе")
                    {
                        InProgressCount++;
                    }

                    if (StatusName == "Завершён")
                    {
                        CompletedCount++;
                    }

                    if (StatusName == "Отменён")
                    {
                        CancelledCount++;
                    }

                    worksheet.Cells[CurrentRow, 1].Value = OrderID;
                    worksheet.Cells[CurrentRow, 2].Value = ClientName;
                    worksheet.Cells[CurrentRow, 3].Value = UserName;
                    worksheet.Cells[CurrentRow, 4].Value = OrderDate;
                    worksheet.Cells[CurrentRow, 5].Value = CompDate;
                    worksheet.Cells[CurrentRow, 6].Value = ProductName;
                    worksheet.Cells[CurrentRow, 7].Value = StatusName;
                    worksheet.Cells[CurrentRow, 8].Value = OrderCost;

                    dynamic DataRow = worksheet.Range[worksheet.Cells[CurrentRow, 1], worksheet.Cells[CurrentRow, 8]];
                    DataRow.Font.Size = 10;
                    DataRow.HorizontalAlignment = HAlignCenter();
                    DataRow.VerticalAlignment = VAlignCenter();
                    DataRow.Borders.LineStyle = BorderStyleContinuous();

                    worksheet.Cells[CurrentRow, 2].WrapText = true;
                    worksheet.Cells[CurrentRow, 3].WrapText = true;

                    dynamic ProductCell = worksheet.Cells[CurrentRow, 6];
                    ProductCell.WrapText = true;
                    ProductCell.HorizontalAlignment = HAlignLeft();
                    ProductCell.VerticalAlignment = VAlignCenter();

                    dynamic CostCell = worksheet.Cells[CurrentRow, 8];
                    CostCell.NumberFormatLocal = "# ##0,00 ₽";

                    int productCount = 1;

                    if (ProductName != "")
                    {
                        string[] lines = ProductName.Split('\n');
                        productCount = lines.Length;
                    }

                    worksheet.Rows[CurrentRow].RowHeight = 18 + (productCount * 14);

                    dynamic StatusCell = worksheet.Cells[CurrentRow, 7];
                    StatusCell.HorizontalAlignment = HAlignCenter();
                    StatusCell.VerticalAlignment = VAlignCenter();
                    StatusCell.Font.Bold = true;
                    StatusCell.Font.Color = Color.Black;

                    if (StatusName == "Новый")
                    {
                        StatusCell.Interior.Color = Color.FromArgb(217, 225, 242);
                    }
                    else if (StatusName == "В работе")
                    {
                        StatusCell.Interior.Color = Color.FromArgb(255, 242, 204);
                    }
                    else if (StatusName == "Завершён")
                    {
                        StatusCell.Interior.Color = Color.FromArgb(198, 232, 207);
                    }
                    else if (StatusName == "Отменён")
                    {
                        StatusCell.Interior.Color = Color.FromArgb(242, 220, 219);
                    }

                    CurrentRow = CurrentRow + 1;
                }

                CurrentRow = CurrentRow + 1;

                // ==================== СТАТИСТИКА ====================
                decimal AverageSum = 0;
                if (DataGridView.Rows.Count > 0)
                {
                    AverageSum = TotalSum / DataGridView.Rows.Count;
                }

                int StatsHeaderRow = CurrentRow;
                worksheet.Cells[StatsHeaderRow, 1].Value = "УСЛОВНЫЕ ОБОЗНАЧЕНИЯ";
                worksheet.Cells[StatsHeaderRow, 1].Font.Bold = true;
                worksheet.Cells[StatsHeaderRow, 1].Font.Size = 11;
                worksheet.Cells[StatsHeaderRow, 1].Font.Color = Color.White;
                worksheet.Cells[StatsHeaderRow, 1].Interior.Color = Color.FromArgb(45, 156, 219);
                worksheet.Range[worksheet.Cells[StatsHeaderRow, 1], worksheet.Cells[StatsHeaderRow, 3]].WrapText = false;
                worksheet.Range[worksheet.Cells[StatsHeaderRow, 1], worksheet.Cells[StatsHeaderRow, 3]].Merge();
                worksheet.Range[worksheet.Cells[StatsHeaderRow, 1], worksheet.Cells[StatsHeaderRow, 3]].HorizontalAlignment = HAlignCenter();

                worksheet.Cells[StatsHeaderRow, 5].Value = "ИТОГОВАЯ ИНФОРМАЦИЯ";
                worksheet.Cells[StatsHeaderRow, 5].Font.Bold = true;
                worksheet.Cells[StatsHeaderRow, 5].Font.Size = 11;
                worksheet.Cells[StatsHeaderRow, 5].Font.Color = Color.White;
                worksheet.Cells[StatsHeaderRow, 5].Interior.Color = Color.FromArgb(45, 156, 219);
                worksheet.Range[worksheet.Cells[StatsHeaderRow, 5], worksheet.Cells[StatsHeaderRow, 8]].Merge();
                worksheet.Range[worksheet.Cells[StatsHeaderRow, 5], worksheet.Cells[StatsHeaderRow, 8]].HorizontalAlignment = HAlignCenter();

                CurrentRow = CurrentRow + 1;
                int LegendRow = CurrentRow;

                // Новый
                worksheet.Cells[LegendRow, 1].Value = "Новый";
                worksheet.Cells[LegendRow, 1].Interior.Color = Color.FromArgb(217, 225, 242);
                worksheet.Cells[LegendRow, 1].Font.Bold = true;
                worksheet.Cells[LegendRow, 1].Font.Size = 10;
                worksheet.Cells[LegendRow, 1].Font.Color = Color.Black;
                worksheet.Cells[LegendRow, 1].HorizontalAlignment = HAlignCenter();
                worksheet.Cells[LegendRow, 1].VerticalAlignment = VAlignCenter();
                worksheet.Range[worksheet.Cells[LegendRow, 2], worksheet.Cells[LegendRow, 3]].Merge();
                worksheet.Cells[LegendRow, 2].Value = "Заказ создан, ждет обработки";
                worksheet.Cells[LegendRow, 2].Font.Size = 10;
                worksheet.Rows[LegendRow].RowHeight = 20;
                worksheet.Cells[LegendRow, 5].Value = "Всего заказов:";
                worksheet.Cells[LegendRow, 5].Font.Bold = true;
                worksheet.Cells[LegendRow, 5].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].Value = DataGridView.Rows.Count;
                worksheet.Cells[LegendRow, 6].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].NumberFormatLocal = "# ##0";
                worksheet.Cells[LegendRow, 6].HorizontalAlignment = HAlignLeft();
                worksheet.Cells[LegendRow, 6].IndentLevel = 6;
                worksheet.Cells[LegendRow, 7].Value = "Общая сумма:";
                worksheet.Cells[LegendRow, 7].Font.Bold = true;
                worksheet.Cells[LegendRow, 7].Font.Size = 10;
                worksheet.Cells[LegendRow, 7].IndentLevel = 1;
                worksheet.Cells[LegendRow, 8].Value = TotalSum;
                worksheet.Cells[LegendRow, 8].NumberFormatLocal = "# ##0,00 ₽";
                worksheet.Cells[LegendRow, 8].Font.Size = 10;
                LegendRow = LegendRow + 1;

                // В работе
                worksheet.Cells[LegendRow, 1].Value = "В работе";
                worksheet.Cells[LegendRow, 1].Interior.Color = Color.FromArgb(255, 242, 204);
                worksheet.Cells[LegendRow, 1].Font.Bold = true;
                worksheet.Cells[LegendRow, 1].Font.Size = 10;
                worksheet.Cells[LegendRow, 1].Font.Color = Color.Black;
                worksheet.Cells[LegendRow, 1].HorizontalAlignment = HAlignCenter();
                worksheet.Cells[LegendRow, 1].VerticalAlignment = VAlignCenter();
                worksheet.Range[worksheet.Cells[LegendRow, 2], worksheet.Cells[LegendRow, 3]].Merge();
                worksheet.Cells[LegendRow, 2].Value = "Заказ находится в процессе выполнения";
                worksheet.Cells[LegendRow, 2].Font.Size = 10;
                worksheet.Rows[LegendRow].RowHeight = 20;
                worksheet.Cells[LegendRow, 5].Value = "Новых:";
                worksheet.Cells[LegendRow, 5].Font.Bold = true;
                worksheet.Cells[LegendRow, 5].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].Value = NewCount;
                worksheet.Cells[LegendRow, 6].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].NumberFormatLocal = "# ##0";
                worksheet.Cells[LegendRow, 6].HorizontalAlignment = HAlignLeft();
                worksheet.Cells[LegendRow, 6].IndentLevel = 6;
                worksheet.Cells[LegendRow, 7].Value = "Средняя сумма:";
                worksheet.Cells[LegendRow, 7].Font.Bold = true;
                worksheet.Cells[LegendRow, 7].Font.Size = 10;
                worksheet.Cells[LegendRow, 7].IndentLevel = 1;
                worksheet.Cells[LegendRow, 8].Value = AverageSum;
                worksheet.Cells[LegendRow, 8].NumberFormatLocal = "# ##0,00 ₽";
                worksheet.Cells[LegendRow, 8].Font.Size = 10;
                LegendRow = LegendRow + 1;

                // Завершён
                worksheet.Cells[LegendRow, 1].Value = "Завершён";
                worksheet.Cells[LegendRow, 1].Interior.Color = Color.FromArgb(198, 232, 207);
                worksheet.Cells[LegendRow, 1].Font.Bold = true;
                worksheet.Cells[LegendRow, 1].Font.Size = 10;
                worksheet.Cells[LegendRow, 1].Font.Color = Color.Black;
                worksheet.Cells[LegendRow, 1].HorizontalAlignment = HAlignCenter();
                worksheet.Cells[LegendRow, 1].VerticalAlignment = VAlignCenter();
                worksheet.Range[worksheet.Cells[LegendRow, 2], worksheet.Cells[LegendRow, 3]].Merge();
                worksheet.Cells[LegendRow, 2].Value = "Заказ успешно выполнен";
                worksheet.Cells[LegendRow, 2].Font.Size = 10;
                worksheet.Rows[LegendRow].RowHeight = 20;
                worksheet.Cells[LegendRow, 5].Value = "В работе:";
                worksheet.Cells[LegendRow, 5].Font.Bold = true;
                worksheet.Cells[LegendRow, 5].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].Value = InProgressCount;
                worksheet.Cells[LegendRow, 6].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].NumberFormatLocal = "# ##0";
                worksheet.Cells[LegendRow, 6].HorizontalAlignment = HAlignLeft();
                worksheet.Cells[LegendRow, 6].IndentLevel = 6;
                worksheet.Cells[LegendRow, 7].Value = "Макс. заказ:";
                worksheet.Cells[LegendRow, 7].Font.Bold = true;
                worksheet.Cells[LegendRow, 7].Font.Size = 10;
                worksheet.Cells[LegendRow, 7].IndentLevel = 1;
                decimal MaxCost = 0;

                for (int i = 0; i < OrderCosts.Count; i++)
                {
                    if (OrderCosts[i] > MaxCost)
                    {
                        MaxCost = OrderCosts[i];
                    }
                }
                worksheet.Cells[LegendRow, 8].Value = MaxCost;
                worksheet.Cells[LegendRow, 8].NumberFormatLocal = "# ##0,00 ₽";
                worksheet.Cells[LegendRow, 8].Font.Size = 10;
                LegendRow = LegendRow + 1;

                // Отменён
                worksheet.Cells[LegendRow, 1].Value = "Отменён";
                worksheet.Cells[LegendRow, 1].Interior.Color = Color.FromArgb(242, 220, 219);
                worksheet.Cells[LegendRow, 1].Font.Bold = true;
                worksheet.Cells[LegendRow, 1].Font.Size = 10;
                worksheet.Cells[LegendRow, 1].Font.Color = Color.Black;
                worksheet.Cells[LegendRow, 1].HorizontalAlignment = HAlignCenter();
                worksheet.Cells[LegendRow, 1].VerticalAlignment = VAlignCenter();
                worksheet.Range[worksheet.Cells[LegendRow, 2], worksheet.Cells[LegendRow, 3]].Merge();
                worksheet.Cells[LegendRow, 2].Value = "Заказ был отменён";
                worksheet.Cells[LegendRow, 2].Font.Size = 10;
                worksheet.Rows[LegendRow].RowHeight = 20;
                worksheet.Cells[LegendRow, 5].Value = "Завершено:";
                worksheet.Cells[LegendRow, 5].Font.Bold = true;
                worksheet.Cells[LegendRow, 5].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].Value = CompletedCount;
                worksheet.Cells[LegendRow, 6].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].NumberFormatLocal = "# ##0";
                worksheet.Cells[LegendRow, 6].HorizontalAlignment = HAlignLeft();
                worksheet.Cells[LegendRow, 6].IndentLevel = 6;
                worksheet.Cells[LegendRow, 7].Value = "Мин. заказ:";
                worksheet.Cells[LegendRow, 7].Font.Bold = true;
                worksheet.Cells[LegendRow, 7].Font.Size = 10;
                worksheet.Cells[LegendRow, 7].IndentLevel = 1;
                decimal MinCost = 99999999;

                for (int i = 0; i < OrderCosts.Count; i++)
                {
                    if (OrderCosts[i] > 0 && OrderCosts[i] < MinCost)
                    {
                        MinCost = OrderCosts[i];
                    }
                }

                if (MinCost == 99999999)
                {
                    MinCost = 0;
                }
                worksheet.Cells[LegendRow, 8].Value = MinCost;
                worksheet.Cells[LegendRow, 8].NumberFormatLocal = "# ##0,00 ₽";
                worksheet.Cells[LegendRow, 8].Font.Size = 10;
                LegendRow = LegendRow + 1;

                // Отменено
                worksheet.Cells[LegendRow, 5].Value = "Отменено:";
                worksheet.Cells[LegendRow, 5].Font.Bold = true;
                worksheet.Cells[LegendRow, 5].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].Value = CancelledCount;
                worksheet.Cells[LegendRow, 6].Font.Size = 10;
                worksheet.Cells[LegendRow, 6].NumberFormatLocal = "# ##0";
                worksheet.Cells[LegendRow, 6].HorizontalAlignment = HAlignLeft();
                worksheet.Cells[LegendRow, 6].IndentLevel = 6;

                // ==================== ШИРИНА КОЛОНОК ====================
                worksheet.Columns[1].ColumnWidth = 10;
                worksheet.Columns[2].ColumnWidth = 22;
                worksheet.Columns[3].ColumnWidth = 26;
                worksheet.Columns[4].ColumnWidth = 10;
                worksheet.Columns[5].ColumnWidth = 16;
                worksheet.Columns[6].ColumnWidth = 32;
                worksheet.Columns[6].WrapText = true;
                worksheet.Columns[7].ColumnWidth = 14;
                worksheet.Columns[8].ColumnWidth = 16;

                // ==================== НАСТРОЙКА ПЕЧАТИ ====================
                worksheet.PageSetup.PaperSize = PaperA4();
                worksheet.PageSetup.Orientation = OrientationLandscape();
                worksheet.PageSetup.Zoom = false;
                worksheet.PageSetup.FitToPagesWide = 1;
                worksheet.PageSetup.FitToPagesTall = false;
                worksheet.PageSetup.PrintArea = worksheet.UsedRange.Address;
                worksheet.PageSetup.CenterHorizontally = false;
                worksheet.PageSetup.CenterVertically = false;

                // ==================== ЛИСТ С ГРАФИКОМ ====================
                object missing = Missing.Value;
                dynamic ChartWorksheet = workbook.Sheets.Add(missing, workbook.Sheets[workbook.Sheets.Count], missing, missing);
                ChartWorksheet.Name = "График продаж";
                ChartWorksheet.Cells.Font.Name = "Calibri";

                // Заголовок листа
                ChartWorksheet.Cells[1, 1].Value = "ПРОДАЖИ ПО МЕСЯЦАМ";
                ChartWorksheet.Cells[1, 1].Font.Bold = true;
                ChartWorksheet.Cells[1, 1].Font.Size = 14;
                ChartWorksheet.Cells[1, 1].Font.Color = Color.White;
                ChartWorksheet.Cells[1, 1].Interior.Color = Color.FromArgb(45, 156, 219);
                ChartWorksheet.Range[ChartWorksheet.Cells[1, 1], ChartWorksheet.Cells[1, 3]].Merge();
                ChartWorksheet.Range[ChartWorksheet.Cells[1, 1], ChartWorksheet.Cells[1, 3]].HorizontalAlignment = HAlignCenter();
                ChartWorksheet.Rows[1].RowHeight = 28;

                // Заголовки таблицы данных
                ChartWorksheet.Cells[2, 1].Value = "Месяц";
                ChartWorksheet.Cells[2, 2].Value = "Сумма продаж, ₽";
                ChartWorksheet.Cells[2, 3].Value = "Кол-во заказов";
                dynamic ChartDataHeader = ChartWorksheet.Range[ChartWorksheet.Cells[2, 1], ChartWorksheet.Cells[2, 3]];
                ChartDataHeader.Font.Bold = true;
                ChartDataHeader.Font.Size = 11;
                ChartDataHeader.Font.Color = Color.White;
                ChartDataHeader.Interior.Color = Color.FromArgb(45, 156, 219);
                ChartDataHeader.HorizontalAlignment = HAlignCenter();
                ChartDataHeader.VerticalAlignment = VAlignCenter();
                ChartDataHeader.Borders.LineStyle = BorderStyleContinuous();

                // Сортировка ключей по дате и запись данных
                List<string> SortedKeys = new List<string>(MonthlySales.Keys);
                SortedKeys.Sort();

                int ChartDataRow = 3;
                foreach (string key in SortedKeys)
                {
                    ChartWorksheet.Cells[ChartDataRow, 1].Value = MonthlyLabels[key];
                    ChartWorksheet.Cells[ChartDataRow, 2].Value = (double)MonthlySales[key];
                    ChartWorksheet.Cells[ChartDataRow, 2].NumberFormatLocal = "# ##0,00 ₽";
                    ChartWorksheet.Cells[ChartDataRow, 3].Value = MonthlyOrderCount[key];
                    ChartWorksheet.Cells[ChartDataRow, 3].NumberFormatLocal = "# ##0";

                    dynamic RowRange = ChartWorksheet.Range[ChartWorksheet.Cells[ChartDataRow, 1], ChartWorksheet.Cells[ChartDataRow, 3]];
                    RowRange.Font.Size = 10;
                    RowRange.HorizontalAlignment = HAlignCenter();
                    RowRange.Borders.LineStyle = BorderStyleContinuous();

                    bool IsEvenRow = (ChartDataRow % 2 == 0);

                    if (IsEvenRow)
                    {
                        RowRange.Interior.Color = Color.FromArgb(235, 245, 253);
                    }

                    ChartDataRow = ChartDataRow + 1;
                }

                // Ширина колонок таблицы данных
                ChartWorksheet.Columns[1].ColumnWidth = 20;
                ChartWorksheet.Columns[2].ColumnWidth = 22;
                ChartWorksheet.Columns[3].ColumnWidth = 18;

                // Создание графика
                int ChartDataCount = SortedKeys.Count;
                if (ChartDataCount > 0)
                {
                    dynamic ChartObjects = ChartWorksheet.ChartObjects();
                    dynamic ChartObj = ChartObjects.Add(20, (ChartDataRow + 1) * 15, 700, 380);
                    dynamic chart = ChartObj.Chart;

                    // Источник данных колонки А (месяц) и B (сумма)
                    dynamic SourceRange = ChartWorksheet.Range[ChartWorksheet.Cells[2, 1], ChartWorksheet.Cells[2 + ChartDataCount, 2]];
                    chart.SetSourceData(SourceRange);
                    chart.ChartType = ChartTypeLineMarkers();

                    // Заголовок графика
                    chart.HasTitle = true;
                    chart.ChartTitle.Text = "Сумма продаж по месяцам (" + DateFrom.ToString("dd.MM.yyyy") + " - " + DateTo.ToString("dd.MM.yyyy") + ")";
                    chart.ChartTitle.Font.Bold = true;
                    chart.ChartTitle.Font.Size = 13;

                    // Стиль линии и маркеров
                    dynamic series = chart.SeriesCollection(1);
                    series.Format.Line.ForeColor.RGB = ColorTranslator.ToOle(Color.FromArgb(45, 156, 219));
                    series.Format.Line.Weight = 2.5f;
                    series.MarkerStyle = MarkerStyleSquare();
                    series.MarkerSize = 7;
                    series.MarkerForegroundColor = ColorTranslator.ToOle(Color.FromArgb(45, 156, 219));
                    series.MarkerBackgroundColor = ColorTranslator.ToOle(Color.White);

                    // Подписи данных
                    series.HasDataLabels = true;
                    series.DataLabels.Font.Size = 9;
                    series.DataLabels.Font.Bold = true;
                    series.DataLabels.NumberFormatLocal = "#\u00a0##0\u00a0## ₽";

                    // Ось категорий X
                    chart.Axes(AxisCategory()).HasTitle = false;
                    chart.Axes(AxisCategory()).TickLabels.Font.Size = 10;

                    // Ось значений Y
                    chart.Axes(AxisValue()).HasTitle = true;
                    chart.Axes(AxisValue()).AxisTitle.Text = "Сумма, ₽";
                    chart.Axes(AxisValue()).AxisTitle.Font.Size = 10;
                    chart.Axes(AxisValue()).TickLabels.NumberFormatLocal = "#\u00a0##0\u00a0## ₽";

                    // Легенда не нужна для одного ряда
                    chart.HasLegend = false;

                    // Настройка печати листа графика
                    ChartWorksheet.PageSetup.PaperSize = PaperA4();
                    ChartWorksheet.PageSetup.Orientation = OrientationLandscape();
                    ChartWorksheet.PageSetup.Zoom = false;
                    ChartWorksheet.PageSetup.FitToPagesWide = 1;
                    ChartWorksheet.PageSetup.FitToPagesTall = 1;
                    ChartWorksheet.PageSetup.TopMargin = 10;
                    ChartWorksheet.PageSetup.BottomMargin = 10;
                    ChartWorksheet.PageSetup.LeftMargin = 10;
                    ChartWorksheet.PageSetup.RightMargin = 10;
                    ChartWorksheet.PageSetup.CenterHorizontally = true;
                }

                // ==================== СОХРАНЕНИЕ ====================
                worksheet.Activate();

                string extension = Path.GetExtension(saveFileDialog.FileName).ToLower();

                if (extension == ".pdf")
                {
                    workbook.ExportAsFixedFormat(ExportFormatPDF(), saveFileDialog.FileName);
                    workbook.Saved = true;
                }
                else if (extension == ".xls")
                {
                    workbook.SaveAs(saveFileDialog.FileName, FormatXls());
                }
                else
                {
                    workbook.SaveAs(saveFileDialog.FileName, FormatXlsx());
                }

                workbook.Close(false);
                workbook = null;
                MessageBox.Show("Отчёт успешно сформирован!\n\nПуть сохранения:\n" + saveFileDialog.FileName, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании отчёта:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (ExcelApp != null)
                    {
                        ExcelApp.DisplayAlerts = false;
                        ExcelApp.Quit();
                    }
                }
                catch
                {

                }

                worksheet = null;
                workbook = null;

                if (ExcelApp != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(ExcelApp);
                    }
                    catch
                    {

                    }

                    ExcelApp = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        private static string GetMonthName(int month)
        {
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                                 "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            if (month >= 1 && month <= 12)
            {
                return months[month - 1];
            }

            return month.ToString();
        }

        private static int HAlignCenter()
        {
            return -4108;
        }
        private static int HAlignLeft() 
        {
            return -4131; 
        }
        private static int HAlignRight() 
        { 
            return -4152; 
        }
        private static int VAlignCenter()
        {
            return -4108;
        }
        private static int BorderStyleContinuous()
        {
            return 1;
        }
        private static int ChartTypeLineMarkers()
        {
            return 65;
        }
        private static int MarkerStyleSquare() 
        {
            return 2;
        }
        private static int AxisCategory()
        {
            return 1;
        }
        private static int AxisValue()
        {
            return 2;
        }
        private static int PaperA4() 
        {
            return 9;
        }
        private static int OrientationLandscape()
        {
            return 2;
        }
        private static int ExportFormatPDF()
        {
            return 0;
        }
        private static int FormatXlsx()
        {
            return 51;
        }
        private static int FormatXls()
        {
            return 56;
        }
    }
}