using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MetGlobal.Services
{
    public static class ExportService
    {
        private static readonly BaseColor ColorHeaderBg = new BaseColor(230, 230, 230);
        private static readonly BaseColor ColorGroupBg = new BaseColor(240, 248, 255);
        private static readonly BaseColor ColorTotalBg = new BaseColor(50, 50, 50);
        private static readonly BaseColor ColorTotalText = BaseColor.WHITE;

        public static iTextSharp.text.Image RenderControlToImage(FrameworkElement control)
        {
            if (control == null) return null;

            control.Measure(new Size(control.ActualWidth, control.ActualHeight));
            control.Arrange(new Rect(new Size(control.ActualWidth, control.ActualHeight)));
            control.UpdateLayout();

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)control.ActualWidth,
                (int)control.ActualHeight,
                96d, 96d,
                PixelFormats.Pbgra32);

            rtb.Render(control);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return iTextSharp.text.Image.GetInstance(ms.ToArray());
            }
        }

        public static void ExportToPdf<T>(
            IEnumerable<T> data,
            string title,
            iTextSharp.text.Image chartImage = null,
            Func<T, string> groupKeySelector = null,
            string groupTitlePrefix = "Группа: ",
            string[] summableProperties = null)
        {
            if (data == null || !data.Any())
            {
                DialogService.ShowError("Нет данных для экспорта.");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF-файл (*.pdf)|*.pdf",
                FileName = $"{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                Document doc = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(saveFileDialog.FileName, FileMode.Create));

                doc.Open();

                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "ARIAL.TTF");
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                Font fontNormal = new Font(baseFont, 9, Font.NORMAL);
                Font fontBold = new Font(baseFont, 9, Font.BOLD);
                Font fontTitle = new Font(baseFont, 16, Font.BOLD);
                Font fontGroup = new Font(baseFont, 10, Font.ITALIC);
                Font fontTotal = new Font(baseFont, 10, Font.BOLD, ColorTotalText);

                Paragraph pTitle = new Paragraph(title, fontTitle) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 10 };
                doc.Add(pTitle);
                doc.Add(new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", fontNormal) { Alignment = Element.ALIGN_RIGHT, SpacingAfter = 20 });

                if (chartImage != null)
                {
                    float maxWidth = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
                    float maxHeight = 250f;

                    if (chartImage.Width > maxWidth) chartImage.ScaleToFit(maxWidth, maxHeight);

                    chartImage.Alignment = Element.ALIGN_CENTER;
                    chartImage.SpacingAfter = 20f;
                    doc.Add(chartImage);
                }

                var properties = typeof(T).GetProperties();
                var displayProps = groupKeySelector != null
                    ? properties.Where(p => !p.Name.Contains("Formatted") && !p.Name.Equals("SupplierName") && !p.Name.Equals("RawSortableDate")).ToArray()
                    : properties.Where(p => !p.Name.Equals("RawSortableDate")).ToArray();

                PdfPTable table = new PdfPTable(displayProps.Length) { WidthPercentage = 100 };

                foreach (var prop in displayProps)
                {
                    // Использование TranslationHelper для заголовков
                    string headerText = TranslationHelper.GetHeader(prop.Name);
                    PdfPCell cell = new PdfPCell(new Phrase(headerText, fontBold))
                    {
                        BackgroundColor = ColorHeaderBg,
                        Padding = 6,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    table.AddCell(cell);
                }

                Dictionary<string, decimal> grandTotals = new Dictionary<string, decimal>();
                if (summableProperties != null)
                {
                    foreach (var prop in summableProperties) grandTotals[prop] = 0;
                }

                if (groupKeySelector != null)
                {
                    var groups = data.GroupBy(groupKeySelector);

                    foreach (var group in groups)
                    {
                        PdfPCell groupCell = new PdfPCell(new Phrase($"{groupTitlePrefix} {group.Key}", fontGroup))
                        {
                            Colspan = displayProps.Length,
                            BackgroundColor = ColorGroupBg,
                            PaddingTop = 8,
                            PaddingBottom = 8,
                            PaddingLeft = 10,
                            BorderWidthTop = 1f
                        };
                        table.AddCell(groupCell);

                        Dictionary<string, decimal> groupTotals = new Dictionary<string, decimal>();
                        if (summableProperties != null)
                        {
                            foreach (var prop in summableProperties) groupTotals[prop] = 0;
                        }

                        foreach (var item in group)
                        {
                            AddRowToTable(table, item, displayProps, fontNormal, summableProperties, groupTotals, grandTotals);
                        }

                        if (summableProperties != null && summableProperties.Any())
                        {
                            // РУСИФИЦИРОВАНО: Итого по группе
                            AddTotalRow(table, displayProps, groupTotals, fontBold, "Итого по группе:", BaseColor.WHITE, BaseColor.BLACK);
                        }
                    }
                }
                else
                {
                    foreach (var item in data)
                    {
                        AddRowToTable(table, item, displayProps, fontNormal, summableProperties, null, grandTotals);
                    }
                }

                if (summableProperties != null && summableProperties.Any())
                {
                    // РУСИФИЦИРОВАНО: ОБЩИЙ ИТОГ
                    AddTotalRow(table, displayProps, grandTotals, fontTotal, "ОБЩИЙ ИТОГ:", ColorTotalBg, ColorTotalText);
                }

                doc.Add(table);
                doc.Close();

                DialogService.ShowInfo("Экспорт в PDF успешно завершен!");
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Ошибка экспорта: {ex.Message}");
            }
        }

        private static void AddRowToTable<T>(
            PdfPTable table,
            T item,
            System.Reflection.PropertyInfo[] props,
            Font font,
            string[] sumProps,
            Dictionary<string, decimal> groupStats,
            Dictionary<string, decimal> grandStats)
        {
            foreach (var prop in props)
            {
                object val = prop.GetValue(item);
                string textVal = val?.ToString() ?? "";

                if (val is decimal d) textVal = d.ToString("N2");
                if (val is double db) textVal = db.ToString("N2");
                if (val is DateTime dt) textVal = dt.ToString("dd.MM.yyyy");

                // Форматирование процентов, если свойство называется Percentage
                if (prop.Name == "Percentage" && val is double pct)
                {
                    textVal = pct.ToString("P1");
                }

                if (sumProps != null && sumProps.Contains(prop.Name) && val != null)
                {
                    decimal numVal = Convert.ToDecimal(val);
                    if (groupStats != null) groupStats[prop.Name] += numVal;
                    if (grandStats != null) grandStats[prop.Name] += numVal;
                }

                int align = (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(double))
                    ? Element.ALIGN_RIGHT
                    : Element.ALIGN_LEFT;

                PdfPCell cell = new PdfPCell(new Phrase(textVal, font))
                {
                    Padding = 4,
                    HorizontalAlignment = align,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                table.AddCell(cell);
            }
        }

        private static void AddTotalRow(
            PdfPTable table,
            System.Reflection.PropertyInfo[] props,
            Dictionary<string, decimal> totals,
            Font font,
            string label,
            BaseColor bgColor,
            BaseColor fgColor)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, font))
            {
                Colspan = 1,
                BackgroundColor = bgColor,
                Padding = 5,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            labelCell.Phrase.Font.Color = fgColor;
            table.AddCell(labelCell);

            for (int i = 1; i < props.Length; i++)
            {
                string propName = props[i].Name;
                string valText = totals.ContainsKey(propName) ? totals[propName].ToString("N2") : "";

                PdfPCell cell = new PdfPCell(new Phrase(valText, font))
                {
                    BackgroundColor = bgColor,
                    Padding = 5,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                };
                cell.Phrase.Font.Color = fgColor;
                table.AddCell(cell);
            }
        }

        public static void ExportToExcel<T>(IEnumerable<T> data, string worksheetName)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = $"{worksheetName}_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(worksheetName.Length > 30 ? worksheetName.Substring(0, 30) : worksheetName);

                    var properties = typeof(T).GetProperties();
                    int col = 1;

                    foreach (var prop in properties)
                    {
                        worksheet.Cell(1, col).Value = TranslationHelper.GetHeader(prop.Name);
                        worksheet.Cell(1, col).Style.Font.Bold = true;
                        worksheet.Cell(1, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                        col++;
                    }

                    worksheet.Cell(2, 1).InsertData(data);
                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveFileDialog.FileName);
                }
                DialogService.ShowInfo("Экспорт в Excel успешно завершен!");
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Ошибка экспорта в Excel: " + ex.Message);
            }
        }
    }
}