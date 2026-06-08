using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;

namespace WebSiteDev.Doc
{
    public static class CheckWord
    {
        // Word COM константы
        private const int WdLineStyleSingle = 1;
        private const int WdLineWidth075pt = 6;
        private const int WdColorBlack = 0;
        private const int WdBorderTop = -1;
        private const int WdBorderLeft = -2;
        private const int WdBorderBottom = -3;
        private const int WdBorderRight = -4;
        private const int WdAlignParagraphLeft = 0;
        private const int WdAlignParagraphCenter = 1;
        private const int WdAlignParagraphRight = 2;
        private const int WdBoldOn = 1;
        private const int WdBoldOff = 0;
        private const int WdSaveFormatDocument = 0;
        private const int WdSaveFormatDocumentDefault = 16;
        private const int WdSaveFormatPDF = 17;
        private const int WdMsoTrue = -1;
        private const int WdTableBordersOff = 0;
        private const int FirstItemIndex = 1;
        private const int FirstSectionIndex = 1;

        // Размеры страницы и отступы
        private const float PageWidth = 164f;        // 58 мм
        private const float BasePageHeight = 280f;   // чуть меньше базовая высота
        private const float RowHeight = 8f;
        private const float MarginTop = 8f;
        private const float MarginBottom = 8f;
        private const float MarginLeft = 6f;
        private const float MarginRight = 6f;
        private const float BorderDistance = 4f;

        // Размеры таблицы
        private const float Column1Width = 82f;   // Услуга
        private const float Column2Width = 22f;   // Кол.
        private const float Column3Width = 38f;   // Цена
        private const float CellPadding = 1f;

        // Разделители
        private const int SeparatorLineLength = 46;
        private const char SeparatorDoubleChar = '═';
        private const char SeparatorSingleChar = '─';

        // Шрифты
        private const string FontTimesNewRoman = "Times New Roman";
        private const string FontCourierNew = "Courier New";

        // Размеры шрифтов
        private const int FontSizeCompanyName = 9;
        private const int FontSizeCheckNumber = 8;
        private const int FontSizeTable = 4;
        private const int FontSizeTotal = 5;
        private const int FontSizeFooterThanks = 6;
        private const int FontSizeSubtitle = 6;
        private const int FontSizeSmall = 5;
        private const int FontSizeLabel = 6;
        private const int FontSizeFooterDate = 5;

        // Отступы абзацев
        private const float SpaceZero = 0f;
        private const float SpaceAfterTiny = 1f;
        private const float SpaceBeforeCheck = 3f;
        private const float SpaceAfterCheck = 3f;
        private const float SpaceBeforeTable = 2f;
        private const float SpaceAfterTable = 2f;
        private const float SpaceBeforeThanks = 6f;
        private const float SpaceBeforeDate = 4f;
        private const float SpaceAfterLogo = 2f;
        private const float SpaceAfterAddress = 1f;
        private const float SpaceAfterPhone = 3f;

        // Финансовые константы
        private const string CurrencySymbol = " ₽";
        private const string NumberFormatMoney = "0.00";
        private const decimal DiscountPercentClientMin = 4m;
        private const decimal DiscountPercentClientMax = 6m;
        private const decimal DiscountPercentQtyMin = 6m;
        private const decimal DiscountPercentQtyMax = 8m;
        private const decimal DiscountPercentComboMin = 11m;
        private const decimal DiscountPercentComboMax = 13m;
        private const decimal DiscountFactorClient = 0.05m;
        private const decimal DiscountFactorQty = 0.07m;
        private const decimal DiscountFactorSurcharge = 0.15m;

        // Табуляция
        private const float RightTabStopPosition = 184f;
        private const int RightTabAlignment = 2;
        private const int TabLeaderNone = 0;
        private const int LabelPadWidth = 10;

        // Форматы даты
        private const string DateFormatShort = "dd.MM.yyyy";
        private const string DateFormatLong = "dd.MM.yyyy  HH:mm:ss";

        // Диалог сохранения
        private const int FilterIndexPDF = 1;
        private const string DefaultExtension = "pdf";
        private const string SaveDialogTitle = "Сохранить чек заказа";
        private const string FileNamePrefix = "Чек_заказа_№";
        private const string SaveFilter = "PDF (*.pdf)|*.pdf|Word 2007-2026 (*.docx)|*.docx|Word 97-2003 (*.doc)|*.doc";

        // Расширения файлов
        private const string ExtPdf = ".pdf";
        private const string ExtDoc = ".doc";

        // ProgID
        private const string WordProgID = "Word.Application";

        public static void CreateCheck(int orderID)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = SaveFilter;
            saveFileDialog.FilterIndex = FilterIndexPDF;
            saveFileDialog.Title = SaveDialogTitle;
            saveFileDialog.FileName = FileNamePrefix + orderID;
            saveFileDialog.DefaultExt = DefaultExtension;

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            dynamic app = null;
            dynamic doc = null;

            try
            {
                Type wordType = Type.GetTypeFromProgID(WordProgID);
                app = Activator.CreateInstance(wordType);
                app.Visible = false;
                doc = app.Documents.Add();

                int ProductCount = 0;
                bool HasDiscountPre = false, HasSurchargePre = false;
                bool HasClientDiscountPre = false, HasQtyDiscountPre = false;
                using (MySqlConnection conTemp = new MySqlConnection(Data.GetConnectionString()))
                {
                    conTemp.Open();
                    MySqlCommand countCmd = new MySqlCommand("SELECT COUNT(*) FROM orderproduct WHERE OrderID = " + orderID, conTemp);
                    ProductCount = Convert.ToInt32(countCmd.ExecuteScalar());

                    using (var rPre = new MySqlCommand("SELECT Discount, Surcharge FROM `Order` WHERE OrderID = " + orderID, conTemp).ExecuteReader())
                    {
                        if (rPre.Read())
                        {
                            if (rPre["Discount"] != DBNull.Value)
                            {
                                decimal d = Convert.ToDecimal(rPre["Discount"]);

                                if (d > 0)
                                {
                                    HasDiscountPre = true;
                                }
                            }
                            if (rPre["Surcharge"] != DBNull.Value)
                            {
                                decimal s = Convert.ToDecimal(rPre["Surcharge"]);

                                if (s > 0)
                                {
                                    HasSurchargePre = true;
                                }
                            }
                        }
                    }

                    if (HasDiscountPre)
                    {
                        object SumObj = new MySqlCommand(
                            "SELECT SUM(op.ProductCount * p.BasePrice) FROM orderproduct op " +
                            "LEFT JOIN Product p ON op.ProductID = p.ProductID WHERE op.OrderID = " + orderID, conTemp).ExecuteScalar();
                        if (SumObj != null && SumObj != DBNull.Value)
                        {
                            decimal SumV = Convert.ToDecimal(SumObj);
                            object DiscValObj = new MySqlCommand("SELECT Discount FROM `Order` WHERE OrderID = " + orderID, conTemp).ExecuteScalar();

                            if (DiscValObj != null && DiscValObj != DBNull.Value && SumV > 0)
                            {
                                decimal dv = Convert.ToDecimal(DiscValObj);
                                decimal pct = (dv / SumV) * 100;

                                HasClientDiscountPre = (pct >= DiscountPercentClientMin && pct <= DiscountPercentClientMax) || (pct >= DiscountPercentComboMin && pct <= DiscountPercentComboMax);
                                HasQtyDiscountPre = (pct >= DiscountPercentQtyMin && pct <= DiscountPercentComboMax);

                                if (pct >= DiscountPercentComboMin && pct <= DiscountPercentComboMax)
                                {
                                    HasClientDiscountPre = true;
                                    HasQtyDiscountPre = true;
                                }
                                else if (pct >= DiscountPercentQtyMin && pct <= DiscountPercentQtyMax)
                                {
                                    HasQtyDiscountPre = true;
                                    HasClientDiscountPre = false;
                                }
                                else if (pct >= DiscountPercentClientMin && pct <= DiscountPercentClientMax)
                                {
                                    HasClientDiscountPre = true;
                                    HasQtyDiscountPre = false;
                                }
                            }
                        }
                    }
                }

                int DiscountRows = 0;

                if (HasClientDiscountPre)
                {
                    DiscountRows = DiscountRows + 1;
                }

                if (HasQtyDiscountPre)
                {
                    DiscountRows = DiscountRows + 1;
                }
                if (!HasClientDiscountPre && !HasQtyDiscountPre && HasDiscountPre)
                {
                    DiscountRows = 1;
                }

                float PageHeight = BasePageHeight;

                if (ProductCount > 1)
                {
                    PageHeight = PageHeight + (ProductCount - 1) * RowHeight;
                }

                PageHeight = PageHeight + DiscountRows * RowHeight;

                if (HasSurchargePre)
                {
                    PageHeight = PageHeight + RowHeight;
                }

                doc.PageSetup.PageWidth = PageWidth;
                doc.PageSetup.PageHeight = PageHeight;
                doc.PageSetup.TopMargin = MarginTop;
                doc.PageSetup.BottomMargin = MarginBottom;
                doc.PageSetup.LeftMargin = MarginLeft;
                doc.PageSetup.RightMargin = MarginRight;

                foreach (int side in new int[] { WdBorderTop, WdBorderLeft, WdBorderBottom, WdBorderRight })
                {
                    doc.Sections[FirstSectionIndex].Borders[side].LineStyle = WdLineStyleSingle;
                    doc.Sections[FirstSectionIndex].Borders[side].LineWidth = WdLineWidth075pt;
                    doc.Sections[FirstSectionIndex].Borders[side].Color = WdColorBlack;
                }
                doc.Sections[FirstSectionIndex].Borders.DistanceFromTop = BorderDistance;
                doc.Sections[FirstSectionIndex].Borders.DistanceFromBottom = BorderDistance;
                doc.Sections[FirstSectionIndex].Borders.DistanceFromLeft = BorderDistance;
                doc.Sections[FirstSectionIndex].Borders.DistanceFromRight = BorderDistance;

                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    con.Open();

                    AddParagraph(doc, "WebSiteDev", FontTimesNewRoman, FontSizeCompanyName, bold: true, align: WdAlignParagraphCenter, sb: SpaceZero, sa: SpaceZero);
                    AddParagraph(doc, "Разработка веб-сайтов", FontTimesNewRoman, FontSizeSubtitle, bold: false, align: WdAlignParagraphCenter, sb: SpaceZero, sa: SpaceZero);
                    AddParagraph(doc, "г. Заволжье, ул. Рождественская, д. 1", FontTimesNewRoman, FontSizeSmall, bold: false, align: WdAlignParagraphCenter, sb: SpaceZero, sa: SpaceAfterAddress);
                    AddParagraph(doc, "Тел: +7 (911) 222-33-44", FontTimesNewRoman, FontSizeSmall, bold: false, align: WdAlignParagraphCenter, sb: SpaceZero, sa: SpaceAfterPhone);

                    AddSeparator(doc, SeparatorDoubleChar);

                    AddParagraph(doc, "Чек заказа № " + orderID, FontTimesNewRoman, FontSizeCheckNumber, bold: true, align: WdAlignParagraphCenter, sb: SpaceBeforeCheck, sa: SpaceAfterCheck);

                    AddSeparator(doc, SeparatorSingleChar);

                    string query = @"
                        SELECT o.OrderID,
                               CONCAT(c.Surname, ' ', c.FirstName, ' ', COALESCE(c.MiddleName,'')) AS ClientName,
                               CONCAT(u.Surname, ' ', u.FirstName, ' ', COALESCE(u.MiddleName,'')) AS UserName,
                               o.OrderDate, o.OrderCompDate,
                               o.OrderCost, o.Discount, o.Surcharge
                        FROM `Order` o
                        LEFT JOIN Clients c ON o.ClientID = c.ClientID
                        LEFT JOIN Users   u ON o.UserID   = u.UserID
                        WHERE o.OrderID = " + orderID;

                    MySqlCommand cmd = new MySqlCommand(query, con);

                    decimal Discount = 0, Surcharge = 0;
                    bool HasDiscount = false, HasSurcharge = false;

                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            if (r["Discount"] != DBNull.Value)
                            {
                                Discount = Convert.ToDecimal(r["Discount"]);

                                if (Discount > 0)
                                {
                                    HasDiscount = true;
                                }
                            }

                            if (r["Surcharge"] != DBNull.Value)
                            {
                                Surcharge = Convert.ToDecimal(r["Surcharge"]);

                                if (Surcharge > 0)
                                {
                                    HasSurcharge = true;
                                }
                            }

                            DateTime dt1 = Convert.ToDateTime(r["OrderDate"]);
                            DateTime dt2 = Convert.ToDateTime(r["OrderCompDate"]);

                            AddLeftRight(doc, "Дата заказа:", dt1.ToString(DateFormatShort), FontSizeLabel);
                            AddLeftRight(doc, "Срок выполнения:", dt2.ToString(DateFormatShort), FontSizeLabel);

                            AddSeparator(doc, SeparatorSingleChar);

                            AddAligned(doc, "Клиент:", r["ClientName"].ToString(), FontSizeLabel);
                            AddAligned(doc, "Сотрудник:", r["UserName"].ToString(), FontSizeLabel);
                        }
                    }

                    AddSeparator(doc, SeparatorSingleChar);

                    AddParagraph(doc, "СОСТАВ ЗАКАЗА", FontTimesNewRoman, FontSizeTable, bold: true, align: WdAlignParagraphCenter, sb: SpaceBeforeTable, sa: SpaceAfterTable);

                    string query2 = @"
                        SELECT p.ProductName, op.ProductCount, p.BasePrice AS ProductCost
                        FROM orderproduct op
                        LEFT JOIN Product p ON op.ProductID = p.ProductID
                        WHERE op.OrderID = " + orderID;

                    MySqlCommand cmd2 = new MySqlCommand(query2, con);

                    decimal SumRaw = 0;
                    object ObjSum = new MySqlCommand(
                        "SELECT SUM(op.ProductCount * p.BasePrice) FROM orderproduct op " +
                        "LEFT JOIN Product p ON op.ProductID = p.ProductID WHERE op.OrderID = " + orderID, con).ExecuteScalar();

                    if (ObjSum != null && ObjSum != DBNull.Value)
                    {
                        SumRaw = Convert.ToDecimal(ObjSum);
                    }

                    string TotalCost = "0.00";
                    object Obj2 = new MySqlCommand("SELECT OrderCost FROM `Order` WHERE OrderID = " + orderID, con).ExecuteScalar();

                    if (Obj2 != null && Obj2 != DBNull.Value)
                    {
                        TotalCost = Convert.ToDecimal(Obj2).ToString(NumberFormatMoney);
                    }

                    decimal ClientDiscount = 0, QtyDiscount = 0;
                    bool HasClientDiscount = false, HasQtyDiscount = false;

                    if (HasDiscount)
                    {
                        decimal Percent;

                        if (SumRaw > 0)
                        {
                            Percent = (Discount / SumRaw) * 100;
                        }
                        else
                        {
                            Percent = 0;
                        }

                        if (Percent >= DiscountPercentComboMin && Percent <= DiscountPercentComboMax)
                        {
                            HasClientDiscount = true;
                            HasQtyDiscount = true;
                            QtyDiscount = Math.Round(SumRaw * DiscountFactorQty, 2);
                            ClientDiscount = Discount - QtyDiscount;
                        }
                        else if (Percent >= DiscountPercentQtyMin && Percent <= DiscountPercentQtyMax)
                        {
                            HasQtyDiscount = true;
                            QtyDiscount = Discount;
                        }
                        else if (Percent >= DiscountPercentClientMin && Percent <= DiscountPercentClientMax)
                        {
                            HasClientDiscount = true;
                            ClientDiscount = Discount;
                        }
                        else
                        {
                            HasClientDiscount = true;
                            ClientDiscount = Discount;
                        }
                    }

                    using (MySqlDataReader r2 = cmd2.ExecuteReader())
                    {
                        var rows = new System.Collections.Generic.List<string[]>();

                        while (r2.Read())
                        {
                            decimal price = 0;
                            decimal.TryParse(r2["ProductCost"].ToString(), out price);

                            rows.Add(new string[]
                            {
                                r2["ProductName"].ToString(),
                                r2["ProductCount"].ToString(),
                                price.ToString(NumberFormatMoney) + CurrencySymbol
                            });
                        }

                        if (rows.Count > 0)
                        {
                            int DiscLines = 0;

                            if (HasClientDiscount)
                            {
                                DiscLines = DiscLines + 1;
                            }

                            if (HasQtyDiscount)
                            {
                                DiscLines = DiscLines + 1;
                            }

                            if (!HasClientDiscount && !HasQtyDiscount && HasDiscount)
                            {
                                DiscLines = 1;
                            }

                            int SummaryRows = 2 + DiscLines;

                            if (HasSurcharge)
                            {
                                SummaryRows = SummaryRows + 1;
                            }

                            int TotalTableRows = rows.Count + 1 + SummaryRows;

                            dynamic LastPara = doc.Paragraphs[doc.Paragraphs.Count];
                            dynamic tbl = doc.Tables.Add(LastPara.Range, TotalTableRows, 3);
                            tbl.Borders.Enable = WdTableBordersOff;

                            SetCell(tbl, 1, 1, "Услуга", FontSizeTable, bold: true);
                            SetCell(tbl, 1, 2, "Кол.", FontSizeTable, bold: true);
                            SetCell(tbl, 1, 3, "Цена", FontSizeTable, bold: true);
                            tbl.Cell(1, 3).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;

                            tbl.Columns[1].Width = Column1Width;
                            tbl.Columns[2].Width = Column2Width;
                            tbl.Columns[3].Width = Column3Width;

                            tbl.Rows[1].Borders[WdBorderBottom].LineStyle = WdLineStyleSingle;

                            for (int i = 0; i < rows.Count; i++)
                            {
                                int row = i + 2;
                                SetCell(tbl, row, 1, rows[i][0], FontSizeTable, bold: false);
                                SetCell(tbl, row, 2, rows[i][1], FontSizeTable, bold: false);
                                SetCell(tbl, row, 3, rows[i][2], FontSizeTable, bold: false);
                                tbl.Cell(row, 3).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                            }

                            int sr = rows.Count + 2;

                            tbl.Rows[sr].Borders[WdBorderTop].LineStyle = WdLineStyleSingle;

                            tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                            tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                            SetCell(tbl, sr, 1, "Сумма: " + SumRaw.ToString(NumberFormatMoney) + CurrencySymbol, FontSizeTable, bold: false);
                            tbl.Cell(sr, 1).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                            sr++;

                            if (HasQtyDiscount)
                            {
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                SetCell(tbl, sr, 1, "Скидка за товары (7%): -" + QtyDiscount.ToString(NumberFormatMoney) + CurrencySymbol, FontSizeTable, bold: false);
                                tbl.Cell(sr, 1).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                                sr++;
                            }

                            if (HasClientDiscount)
                            {
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                SetCell(tbl, sr, 1, "Скидка клиента (5%): -" + ClientDiscount.ToString(NumberFormatMoney) + CurrencySymbol, FontSizeTable, bold: false);
                                tbl.Cell(sr, 1).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                                sr++;
                            }

                            if (HasDiscount && !HasQtyDiscount && !HasClientDiscount)
                            {
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                SetCell(tbl, sr, 1, "Скидка: -" + Discount.ToString(NumberFormatMoney) + CurrencySymbol, FontSizeTable, bold: false);
                                tbl.Cell(sr, 1).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                                sr++;
                            }

                            if (HasSurcharge)
                            {
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                                SetCell(tbl, sr, 1, "Срочность (15%): +" + Surcharge.ToString(NumberFormatMoney) + CurrencySymbol, FontSizeTable, bold: false);
                                tbl.Cell(sr, 1).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                                sr++;
                            }

                            tbl.Rows[sr].Borders[WdBorderTop].LineStyle = WdLineStyleSingle;
                            tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                            tbl.Cell(sr, 1).Merge(tbl.Cell(sr, 2));
                            SetCell(tbl, sr, 1, "ИТОГО: " + TotalCost + CurrencySymbol, FontSizeTotal, bold: true);
                            tbl.Cell(sr, 1).Range.ParagraphFormat.Alignment = WdAlignParagraphRight;
                        }
                    }

                    AddSeparator(doc, SeparatorDoubleChar);

                    AddParagraph(doc, "Спасибо за заказ!", FontTimesNewRoman, FontSizeFooterThanks, bold: true, align: WdAlignParagraphCenter, sb: SpaceBeforeThanks, sa: SpaceZero);
                    AddParagraph(doc, "www.websitedev.ru", FontTimesNewRoman, FontSizeSmall, bold: false, align: WdAlignParagraphCenter, sb: SpaceZero, sa: SpaceZero);
                    AddParagraph(doc, DateTime.Now.ToString(DateFormatLong), FontTimesNewRoman, FontSizeFooterDate, bold: false, align: WdAlignParagraphCenter, sb: SpaceBeforeDate, sa: SpaceZero);
                }

                string ext = Path.GetExtension(saveFileDialog.FileName).ToLower();

                if (ext == ExtPdf)
                {
                    doc.SaveAs(saveFileDialog.FileName, WdSaveFormatPDF);
                }
                else if (ext == ExtDoc)
                {
                    doc.SaveAs(saveFileDialog.FileName, WdSaveFormatDocument);
                }
                else
                {
                    doc.SaveAs(saveFileDialog.FileName, WdSaveFormatDocumentDefault);
                }

                doc.Close(false);
                app.Quit(false);

                MessageBox.Show("Чек успешно сформирован!\n\n" + saveFileDialog.FileName, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании чека:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (doc != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(doc);
                    }
                }
                catch
                {

                }

                try
                {
                    if (app != null)
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(app); app = null;
                    }
                }
                catch
                {

                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static void AddParagraph(dynamic doc, string text, string font, int size,
            bool bold, int align, float sb, float sa)
        {
            dynamic p = doc.Paragraphs.Add();
            p.Range.Text = text;
            p.Range.Font.Name = font;
            p.Range.Font.Size = size;

            if (bold)
            {
                p.Range.Font.Bold = WdBoldOn;
            }
            else
            {
                p.Range.Font.Bold = WdBoldOff;
            }

            p.Alignment = align;
            p.SpaceBefore = sb;
            p.SpaceAfter = sa;
            p.Range.InsertParagraphAfter();
        }

        private static void AddLeftRight(dynamic doc, string label, string value, int size, bool boldValue = false)
        {
            dynamic p = doc.Paragraphs.Add();
            p.Range.Font.Name = FontCourierNew;
            p.Range.Font.Size = size;
            p.Range.Font.Bold = WdBoldOff;
            p.Alignment = WdAlignParagraphLeft;
            p.SpaceBefore = SpaceZero;
            p.SpaceAfter = SpaceAfterTiny;

            p.TabStops.Add(RightTabStopPosition, RightTabAlignment, TabLeaderNone);

            p.Range.Text = label + "\t" + value;

            if (boldValue)
            {
                dynamic rng = p.Range.Duplicate;
                rng.Start = p.Range.Start + label.Length + 1;
                rng.End = p.Range.End;
                rng.Font.Bold = WdBoldOn;
            }

            p.Range.InsertParagraphAfter();
        }

        private static void AddSeparator(dynamic doc, char separatorChar)
        {
            string line = new string(separatorChar, SeparatorLineLength);
            dynamic p = doc.Paragraphs.Add();
            p.Range.Text = line;
            p.Range.Font.Name = FontCourierNew;
            p.Range.Font.Size = FontSizeSmall;
            p.Range.Font.Bold = WdBoldOff;
            p.Alignment = WdAlignParagraphCenter;
            p.SpaceBefore = SpaceAfterTiny;
            p.SpaceAfter = SpaceAfterTiny;
            p.Range.InsertParagraphAfter();
        }

        private static void SetCell(dynamic tbl, int row, int col, string text, int size, bool bold)
        {
            tbl.Cell(row, col).Range.Text = text;
            tbl.Cell(row, col).Range.Font.Name = FontCourierNew;
            tbl.Cell(row, col).Range.Font.Size = size;

            if (bold)
            {
                tbl.Cell(row, col).Range.Font.Bold = WdBoldOn;
            }
            else
            {
                tbl.Cell(row, col).Range.Font.Bold = WdBoldOff;
            }

            tbl.Cell(row, col).TopPadding = CellPadding;
            tbl.Cell(row, col).BottomPadding = CellPadding;
            tbl.Cell(row, col).LeftPadding = CellPadding;
            tbl.Cell(row, col).RightPadding = CellPadding;
        }

        private static string GetDiscountReason(MySqlConnection con, int orderID, decimal discount)
        {
            object obj = new MySqlCommand(
                "SELECT SUM(op.ProductCount * p.BasePrice) FROM orderproduct op " +
                "LEFT JOIN Product p ON op.ProductID = p.ProductID WHERE op.OrderID = " + orderID, con).ExecuteScalar();

            if (obj == null || obj == DBNull.Value)
            {
                return "Скидка";
            }

            decimal Total = Convert.ToDecimal(obj);

            if (Total <= 0)
            {
                return "Скидка";
            }

            decimal Percent = (discount / Total) * 100;

            if (Percent >= DiscountPercentComboMin && Percent <= DiscountPercentComboMax)
            {
                return "Скидка клиента + товары";
            }

            if (Percent >= DiscountPercentQtyMin && Percent <= DiscountPercentQtyMax)
            {
                return "Скидка за товары (7%)";
            }
            if (Percent >= DiscountPercentClientMin && Percent <= DiscountPercentClientMax)
            {
                return "Скидка клиента (5%)";
            }

            return "Скидка";
        }

        private static void AddAligned(dynamic doc, string label, string value, int size)
        {
            // Отделяем метку от значения ровно одним пробелом
            string text = label + " " + value;

            dynamic p = doc.Paragraphs.Add();
            p.Range.Font.Name = FontCourierNew;
            p.Range.Font.Size = size;
            p.Range.Font.Bold = WdBoldOff;
            p.Alignment = WdAlignParagraphLeft;
            p.SpaceBefore = SpaceZero;
            p.SpaceAfter = SpaceAfterTiny;
            p.Range.Text = text;
            p.Range.InsertParagraphAfter();
        }
    }
}