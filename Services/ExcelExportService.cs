using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExpenseManager.Models;

namespace ExpenseManager.Services;

public static class ExcelExportService
{
    private const string FontFamily = "Segoe UI";

    public static void Export(string filePath, string title, IEnumerable<string> lines)
    {
        EnsureDirectory(filePath);
        var lineList = lines.ToList();

        if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ExportCsv(filePath, title, lineList);
            return;
        }

        using var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = doc.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = CreateStylesheet();
        stylesPart.Stylesheet.Save();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var worksheet = new Worksheet();

        var columns = new Columns(
            new Column { Min = 1, Max = 1, Width = 16, CustomWidth = true },
            new Column { Min = 2, Max = 2, Width = 14, CustomWidth = true },
            new Column { Min = 3, Max = 3, Width = 28, CustomWidth = true },
            new Column { Min = 4, Max = 4, Width = 18, CustomWidth = true },
            new Column { Min = 5, Max = 5, Width = 18, CustomWidth = true },
            new Column { Min = 6, Max = 6, Width = 18, CustomWidth = true }
        );
        worksheet.AppendChild(columns);

        var sheetData = new SheetData();
        uint rowIndex = 1;

        // Title Row
        var titleRow = new Row { RowIndex = rowIndex++, Height = 28, CustomHeight = true };
        titleRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", title, 1));
        sheetData.AppendChild(titleRow);

        // Subtitle Row
        var subRow = new Row { RowIndex = rowIndex++, Height = 20, CustomHeight = true };
        subRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", $"Exported on {DateTime.Now:dd MMM yyyy, hh:mm tt}", 2));
        sheetData.AppendChild(subRow);

        // Empty row
        sheetData.AppendChild(new Row { RowIndex = rowIndex++ });

        var pipeLines = lineList.Where(l => l.Contains('|')).ToList();
        if (pipeLines.Count > 0)
        {
            bool isFirst = true;
            foreach (var pLine in pipeLines)
            {
                var parts = pLine.Split('|').Select(p => p.Trim()).ToList();
                var row = new Row { RowIndex = rowIndex++, Height = isFirst ? 24 : 20, CustomHeight = true };
                for (int c = 0; c < parts.Count; c++)
                {
                    string colLetter = GetColumnLetter(c + 1);
                    string cellRef = $"{colLetter}{rowIndex - 1}";
                    uint style = isFirst ? 3u : ((rowIndex % 2 == 1) ? 5u : 4u);

                    if (!isFirst && decimal.TryParse(parts[c].Replace("INR", "").Replace("\u20B9", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    {
                        row.AppendChild(CreateNumberCell(cellRef, (double)num, (rowIndex % 2 == 1) ? 7u : 6u));
                    }
                    else
                    {
                        row.AppendChild(CreateInlineCell(cellRef, parts[c], style));
                    }
                }
                sheetData.AppendChild(row);
                isFirst = false;
            }
        }
        else
        {
            foreach (var line in lineList)
            {
                var row = new Row { RowIndex = rowIndex++ };
                if (!string.IsNullOrWhiteSpace(line))
                {
                    row.AppendChild(CreateInlineCell($"A{rowIndex - 1}", line, line.EndsWith(':') ? 4u : 0u));
                }
                sheetData.AppendChild(row);
            }
        }

        worksheet.AppendChild(sheetData);
        worksheetPart.Worksheet = worksheet;

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.AppendChild(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Report"
        });

        workbookPart.Workbook.Save();
    }

    public static void ExportTransactions(string filePath, string title, IEnumerable<Transaction> transactions)
    {
        EnsureDirectory(filePath);
        var txList = transactions.OrderByDescending(x => x.Date).ToList();

        if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ExportTransactionsCsv(filePath, txList);
            return;
        }

        using var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = doc.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = CreateStylesheet();
        stylesPart.Stylesheet.Save();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var worksheet = new Worksheet();

        // 7 columns
        var columns = new Columns(
            new Column { Min = 1, Max = 1, Width = 15, CustomWidth = true },  // Date
            new Column { Min = 2, Max = 2, Width = 14, CustomWidth = true },  // Type
            new Column { Min = 3, Max = 3, Width = 28, CustomWidth = true },  // Name
            new Column { Min = 4, Max = 4, Width = 18, CustomWidth = true },  // Category
            new Column { Min = 5, Max = 5, Width = 18, CustomWidth = true },  // Payment
            new Column { Min = 6, Max = 6, Width = 18, CustomWidth = true },  // Amount
            new Column { Min = 7, Max = 7, Width = 32, CustomWidth = true }   // Description
        );
        worksheet.AppendChild(columns);

        var sheetData = new SheetData();
        uint rowIndex = 1;

        // Title
        var titleRow = new Row { RowIndex = rowIndex++, Height = 28, CustomHeight = true };
        titleRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", title, 1));
        sheetData.AppendChild(titleRow);

        // Subtitle
        var subRow = new Row { RowIndex = rowIndex++, Height = 20, CustomHeight = true };
        subRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", $"Exported on {DateTime.Now:dd MMM yyyy, hh:mm tt} \u2022 {txList.Count} transaction{(txList.Count == 1 ? "" : "s")}", 2));
        sheetData.AppendChild(subRow);

        // Summary Statistics Box
        sheetData.AppendChild(new Row { RowIndex = rowIndex++ }); // Blank

        var totalIncome = txList.Where(x => x.Type == "Income").Sum(x => x.Amount);
        var totalExpense = txList.Where(x => x.Type == "Expense").Sum(x => x.Amount);
        var netBalance = totalIncome - totalExpense;

        var sumHeaderRow = new Row { RowIndex = rowIndex++, Height = 22, CustomHeight = true };
        sumHeaderRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "TOTAL INCOME", 11));
        sumHeaderRow.AppendChild(CreateInlineCell($"B{rowIndex - 1}", "TOTAL EXPENSES", 11));
        sumHeaderRow.AppendChild(CreateInlineCell($"C{rowIndex - 1}", "NET BALANCE", 11));
        sheetData.AppendChild(sumHeaderRow);

        var sumValRow = new Row { RowIndex = rowIndex++, Height = 24, CustomHeight = true };
        sumValRow.AppendChild(CreateNumberCell($"A{rowIndex - 1}", (double)totalIncome, 13)); // Income bold green
        sumValRow.AppendChild(CreateNumberCell($"B{rowIndex - 1}", (double)totalExpense, 14)); // Expense bold red
        sumValRow.AppendChild(CreateNumberCell($"C{rowIndex - 1}", (double)netBalance, netBalance >= 0 ? 13u : 14u));
        sheetData.AppendChild(sumValRow);

        sheetData.AppendChild(new Row { RowIndex = rowIndex++ }); // Blank

        // Header Row
        var headerRow = new Row { RowIndex = rowIndex++, Height = 26, CustomHeight = true };
        string[] headers = { "Date", "Type", "Transaction Name", "Category", "Payment Method", "Amount (\u20B9)", "Description" };
        for (int i = 0; i < headers.Length; i++)
        {
            string cellRef = $"{GetColumnLetter(i + 1)}{rowIndex - 1}";
            headerRow.AppendChild(CreateInlineCell(cellRef, headers[i], 3));
        }
        sheetData.AppendChild(headerRow);

        // Data Rows
        for (int i = 0; i < txList.Count; i++)
        {
            var item = txList[i];
            bool isAlt = i % 2 == 1;
            bool isIncome = item.Type == "Income";
            uint textStyle = isAlt ? 5u : 4u;
            uint typeStyle = isIncome ? 8u : 9u;
            uint numStyle = isAlt ? 7u : 6u;

            var row = new Row { RowIndex = rowIndex++, Height = 22, CustomHeight = true };
            uint currentR = rowIndex - 1;

            row.AppendChild(CreateInlineCell($"A{currentR}", item.Date.ToString("dd MMM yyyy"), textStyle));
            row.AppendChild(CreateInlineCell($"B{currentR}", item.Type, typeStyle));
            row.AppendChild(CreateInlineCell($"C{currentR}", item.Name, textStyle));
            row.AppendChild(CreateInlineCell($"D{currentR}", item.Category, textStyle));
            row.AppendChild(CreateInlineCell($"E{currentR}", item.PaymentMethod, textStyle));
            row.AppendChild(CreateNumberCell($"F{currentR}", (double)item.Amount, numStyle));
            row.AppendChild(CreateInlineCell($"G{currentR}", item.Description, textStyle));

            sheetData.AppendChild(row);
        }

        // Summary Total Row at the bottom
        if (txList.Count > 0)
        {
            var totRow = new Row { RowIndex = rowIndex++, Height = 24, CustomHeight = true };
            uint currentR = rowIndex - 1;
            totRow.AppendChild(CreateInlineCell($"A{currentR}", "Total Spending", 11));
            totRow.AppendChild(CreateInlineCell($"B{currentR}", "", 11));
            totRow.AppendChild(CreateInlineCell($"C{currentR}", "", 11));
            totRow.AppendChild(CreateInlineCell($"D{currentR}", "", 11));
            totRow.AppendChild(CreateInlineCell($"E{currentR}", "", 11));
            totRow.AppendChild(CreateNumberCell($"F{currentR}", (double)totalExpense, 14));
            totRow.AppendChild(CreateInlineCell($"G{currentR}", "", 11));
            sheetData.AppendChild(totRow);
        }

        worksheet.AppendChild(sheetData);
        worksheetPart.Worksheet = worksheet;

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.AppendChild(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Transactions"
        });

        workbookPart.Workbook.Save();
    }

    public static void ExportReport(string filePath, string title, decimal totalIncome, decimal totalExpense, IEnumerable<KeyValuePair<string, decimal>> categories, string? summary = null)
    {
        EnsureDirectory(filePath);
        var catList = categories.ToList();
        var netBalance = totalIncome - totalExpense;

        if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ExportReportCsv(filePath, totalIncome, totalExpense, catList, summary);
            return;
        }

        using var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = doc.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = CreateStylesheet();
        stylesPart.Stylesheet.Save();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var worksheet = new Worksheet();

        var columns = new Columns(
            new Column { Min = 1, Max = 1, Width = 28, CustomWidth = true },
            new Column { Min = 2, Max = 2, Width = 20, CustomWidth = true },
            new Column { Min = 3, Max = 3, Width = 18, CustomWidth = true }
        );
        worksheet.AppendChild(columns);

        var sheetData = new SheetData();
        uint rowIndex = 1;

        // Title
        var titleRow = new Row { RowIndex = rowIndex++, Height = 28, CustomHeight = true };
        titleRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", title, 1));
        sheetData.AppendChild(titleRow);

        // Subtitle
        var subRow = new Row { RowIndex = rowIndex++, Height = 20, CustomHeight = true };
        subRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", $"Financial overview as of {DateTime.Now:dd MMM yyyy, hh:mm tt}", 2));
        sheetData.AppendChild(subRow);

        sheetData.AppendChild(new Row { RowIndex = rowIndex++ }); // Blank

        // Financial Summary Table
        var sumHead = new Row { RowIndex = rowIndex++, Height = 24, CustomHeight = true };
        sumHead.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "FINANCIAL SUMMARY", 3));
        sumHead.AppendChild(CreateInlineCell($"B{rowIndex - 1}", "AMOUNT (\u20B9)", 3));
        sheetData.AppendChild(sumHead);

        var incRow = new Row { RowIndex = rowIndex++, Height = 22, CustomHeight = true };
        incRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "Total Income", 4));
        incRow.AppendChild(CreateNumberCell($"B{rowIndex - 1}", (double)totalIncome, 13));
        sheetData.AppendChild(incRow);

        var expRow = new Row { RowIndex = rowIndex++, Height = 22, CustomHeight = true };
        expRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "Total Expenses", 4));
        expRow.AppendChild(CreateNumberCell($"B{rowIndex - 1}", (double)totalExpense, 14));
        sheetData.AppendChild(expRow);

        var balRow = new Row { RowIndex = rowIndex++, Height = 24, CustomHeight = true };
        balRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "Net Balance", 11));
        balRow.AppendChild(CreateNumberCell($"B{rowIndex - 1}", (double)netBalance, netBalance >= 0 ? 13u : 14u));
        sheetData.AppendChild(balRow);

        sheetData.AppendChild(new Row { RowIndex = rowIndex++ }); // Blank

        // Category Breakdown Table
        var catHead = new Row { RowIndex = rowIndex++, Height = 24, CustomHeight = true };
        catHead.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "Category", 3));
        catHead.AppendChild(CreateInlineCell($"B{rowIndex - 1}", "Amount Spent (\u20B9)", 3));
        catHead.AppendChild(CreateInlineCell($"C{rowIndex - 1}", "Share of Total (%)", 3));
        sheetData.AppendChild(catHead);

        for (int i = 0; i < catList.Count; i++)
        {
            var item = catList[i];
            bool isAlt = i % 2 == 1;
            var pct = totalExpense > 0 ? (double)(item.Value / totalExpense) : 0;
            uint textStyle = isAlt ? 5u : 4u;
            uint numStyle = isAlt ? 7u : 6u;

            var row = new Row { RowIndex = rowIndex++, Height = 22, CustomHeight = true };
            uint curR = rowIndex - 1;
            row.AppendChild(CreateInlineCell($"A{curR}", item.Key, textStyle));
            row.AppendChild(CreateNumberCell($"B{curR}", (double)item.Value, numStyle));
            row.AppendChild(CreateNumberCell($"C{curR}", pct, 10)); // Formatted percentage
            sheetData.AppendChild(row);
        }

        // Summary note
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sheetData.AppendChild(new Row { RowIndex = rowIndex++ }); // Blank
            var noteHead = new Row { RowIndex = rowIndex++, Height = 22, CustomHeight = true };
            noteHead.AppendChild(CreateInlineCell($"A{rowIndex - 1}", "Report Summary / Notes:", 4));
            sheetData.AppendChild(noteHead);

            var noteRow = new Row { RowIndex = rowIndex++, Height = 24, CustomHeight = true };
            noteRow.AppendChild(CreateInlineCell($"A{rowIndex - 1}", summary, 2));
            sheetData.AppendChild(noteRow);
        }

        worksheet.AppendChild(sheetData);
        worksheetPart.Worksheet = worksheet;

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.AppendChild(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Financial Report"
        });

        workbookPart.Workbook.Save();
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static Cell CreateInlineCell(string cellRef, string text, uint styleIndex)
    {
        return new Cell
        {
            CellReference = cellRef,
            DataType = CellValues.InlineString,
            StyleIndex = styleIndex,
            InlineString = new InlineString(new Text(text ?? ""))
        };
    }

    private static Cell CreateNumberCell(string cellRef, double number, uint styleIndex)
    {
        return new Cell
        {
            CellReference = cellRef,
            DataType = CellValues.Number,
            StyleIndex = styleIndex,
            CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture))
        };
    }

    private static string GetColumnLetter(int colNumber)
    {
        string letter = "";
        while (colNumber > 0)
        {
            int mod = (colNumber - 1) % 26;
            letter = (char)('A' + mod) + letter;
            colNumber = (colNumber - 1) / 26;
        }
        return letter;
    }

    private static Stylesheet CreateStylesheet()
    {
        return new Stylesheet(
            new Fonts(
                new Font(new FontSize { Val = 11 }, new Color { Rgb = "FF1E293B" }, new FontName { Val = FontFamily }), // 0: Normal
                new Font(new Bold(), new FontSize { Val = 16 }, new Color { Rgb = "FF1E293B" }, new FontName { Val = FontFamily }), // 1: Title
                new Font(new FontSize { Val = 10 }, new Color { Rgb = "FF64748B" }, new FontName { Val = FontFamily }), // 2: Subtitle
                new Font(new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FFFFFFFF" }, new FontName { Val = FontFamily }), // 3: Header
                new Font(new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FF1E293B" }, new FontName { Val = FontFamily }), // 4: Bold Normal
                new Font(new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FFDC2626" }, new FontName { Val = FontFamily }), // 5: Expense Badge
                new Font(new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FF16A34A" }, new FontName { Val = FontFamily })  // 6: Income Badge
            ),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }), // 0: None
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }), // 1: Gray125
                new Fill(new PatternFill(new ForegroundColor { Rgb = "FF4F46E5" }) { PatternType = PatternValues.Solid }), // 2: Indigo Header
                new Fill(new PatternFill(new ForegroundColor { Rgb = "FFF8FAFC" }) { PatternType = PatternValues.Solid }), // 3: Alt Row
                new Fill(new PatternFill(new ForegroundColor { Rgb = "FFF1F5F9" }) { PatternType = PatternValues.Solid })  // 4: Summary Card
            ),
            new Borders(
                new Border(), // 0: None
                new Border(   // 1: Thin border (#E2E8F0)
                    new LeftBorder(new Color { Rgb = "FFE2E8F0" }) { Style = BorderStyleValues.Thin },
                    new RightBorder(new Color { Rgb = "FFE2E8F0" }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color { Rgb = "FFE2E8F0" }) { Style = BorderStyleValues.Thin },
                    new BottomBorder(new Color { Rgb = "FFE2E8F0" }) { Style = BorderStyleValues.Thin }
                )
            ),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(
                new CellFormat { FontId = 0, FillId = 0, BorderId = 0 }, // 0: Default
                new CellFormat { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true }, // 1: Title
                new CellFormat { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true }, // 2: Subtitle
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }) // 3: Header
                {
                    FontId = 3,
                    FillId = 2,
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyFill = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center }) // 4: Regular data
                {
                    FontId = 0,
                    FillId = 0,
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center }) // 5: Alternate data
                {
                    FontId = 0,
                    FillId = 3,
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }) // 6: Currency
                {
                    FontId = 0,
                    FillId = 0,
                    BorderId = 1,
                    NumberFormatId = 4,
                    ApplyNumberFormat = true,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }) // 7: Alt Currency
                {
                    FontId = 0,
                    FillId = 3,
                    BorderId = 1,
                    NumberFormatId = 4,
                    ApplyNumberFormat = true,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }) // 8: Income Badge
                {
                    FontId = 6,
                    FillId = 0,
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }) // 9: Expense Badge
                {
                    FontId = 5,
                    FillId = 0,
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }) // 10: Percentage
                {
                    FontId = 0,
                    FillId = 0,
                    BorderId = 1,
                    NumberFormatId = 10,
                    ApplyNumberFormat = true,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Vertical = VerticalAlignmentValues.Center }) // 11: Summary Label
                {
                    FontId = 4,
                    FillId = 4,
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyFill = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }) // 12: Summary Value
                {
                    FontId = 4,
                    FillId = 4,
                    BorderId = 1,
                    NumberFormatId = 4,
                    ApplyNumberFormat = true,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }) // 13: Summary Green Value
                {
                    FontId = 6,
                    FillId = 4,
                    BorderId = 1,
                    NumberFormatId = 4,
                    ApplyNumberFormat = true,
                    ApplyFont = true,
                    ApplyFill = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat(new Alignment { Horizontal = HorizontalAlignmentValues.Right, Vertical = VerticalAlignmentValues.Center }) // 14: Summary Red Value
                {
                    FontId = 5,
                    FillId = 4,
                    BorderId = 1,
                    NumberFormatId = 4,
                    ApplyNumberFormat = true,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                }
            )
        );
    }

    private static void ExportTransactionsCsv(string filePath, IEnumerable<Transaction> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Type,Transaction Name,Category,Payment Method,Amount,Description");
        foreach (var t in transactions)
        {
            sb.AppendLine($"{EscapeCsv(t.Date.ToString("yyyy-MM-dd"))},{EscapeCsv(t.Type)},{EscapeCsv(t.Name)},{EscapeCsv(t.Category)},{EscapeCsv(t.PaymentMethod)},{t.Amount.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(t.Description)}");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportReportCsv(string filePath, decimal income, decimal expense, IEnumerable<KeyValuePair<string, decimal>> categories, string? summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Financial Summary");
        sb.AppendLine($"Total Income,{income.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Total Expenses,{expense.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net Balance,{(income - expense).ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.AppendLine("Category Breakdown");
        sb.AppendLine("Category,Amount Spent,Share (%)");
        foreach (var c in categories)
        {
            var pct = expense > 0 ? (c.Value / expense * 100m) : 0m;
            sb.AppendLine($"{EscapeCsv(c.Key)},{c.Value.ToString(CultureInfo.InvariantCulture)},{pct:N1}%");
        }
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.AppendLine();
            sb.AppendLine($"Summary,\"{EscapeCsv(summary)}\"");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportCsv(string filePath, string title, IEnumerable<string> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine(EscapeCsv(title));
        sb.AppendLine();
        foreach (var line in lines)
        {
            if (line.Contains('|'))
            {
                var parts = line.Split('|').Select(p => EscapeCsv(p.Trim()));
                sb.AppendLine(string.Join(",", parts));
            }
            else
            {
                sb.AppendLine(EscapeCsv(line));
            }
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
