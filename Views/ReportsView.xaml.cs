using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExpenseManager.Models;
using ExpenseManager.Services;
using Microsoft.Win32;

namespace ExpenseManager.Views;

public partial class ReportsView : UserControl
{
    private readonly User _user;
    private List<Transaction> _data = new();

    public ReportsView(User user)
    {
        InitializeComponent();
        _user = user;
        _ = Load();
    }

    private async Task Load()
    {
        _data = await new TransactionService().GetAsync(_user.UserId);
        var income = _data.Where(x => x.Type == "Income").Sum(x => x.Amount);
        var expense = _data.Where(x => x.Type == "Expense").Sum(x => x.Amount);
        TotalIncome.Text = $"\u20B9{income:N2}";
        TotalExpense.Text = $"\u20B9{expense:N2}";
        ReportCount.Text = $"{_data.Count} transaction{(_data.Count == 1 ? "" : "s")} included";

        var categories = _data.Where(x => x.Type == "Expense").GroupBy(x => x.Category).Select(g => new ReportRow(g.Key, g.Sum(x => x.Amount), 0)).OrderByDescending(x => x.Value).Take(8).ToList();
        var maxCategory = categories.FirstOrDefault()?.Value ?? 0;
        CategoryReport.ItemsSource = categories.Select(x => x with { Percent = maxCategory == 0 ? 0 : (double)(x.Value / maxCategory * 100) }).ToList();
        EmptyReport.Visibility = categories.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (categories.Count > 0) { TopCategory.Text = categories[0].Label; TopCategoryValue.Text = $"\u20B9{categories[0].Value:N2} spent"; } else { TopCategory.Text = "No data"; TopCategoryValue.Text = "Add an expense to begin"; }

        var months = Enumerable.Range(0, 6).Select(i => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5 + i)).Select(month => new ReportRow(month.ToString("MMM"), _data.Where(x => x.Type == "Expense" && x.Date.Year == month.Year && x.Date.Month == month.Month).Sum(x => x.Amount), 0)).ToList();
        var maxMonth = months.Max(x => x.Value);
        MonthlyReport.ItemsSource = months.Select(x => x with { Percent = maxMonth == 0 ? 0 : (double)(x.Value / maxMonth * 100) }).ToList();
        ReportSummary.Text = _data.Count == 0 ? "Your report will appear here after you add your first transaction." : $"You have recorded {_data.Count} transactions. Total spending is \u20B9{expense:N2}, while total income is \u20B9{income:N2}. Your current net balance is \u20B9{income - expense:N2}.";
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_data.Count == 0) { MessageBox.Show("There is no report data to export yet.", "Export report", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var dialog = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = $"expense-report-{DateTime.Today:yyyy-MM-dd}.pdf" };
        if (dialog.ShowDialog() != true) return;
        var income = _data.Where(x => x.Type == "Income").Sum(x => x.Amount);
        var expense = _data.Where(x => x.Type == "Expense").Sum(x => x.Amount);
        var lines = new List<string> { $"Total income: INR {income:N2}", $"Total expenses: INR {expense:N2}", $"Net balance: INR {income - expense:N2}", "", "Expense categories:" };
        lines.AddRange(_data.Where(x => x.Type == "Expense").GroupBy(x => x.Category).OrderByDescending(x => x.Sum(t => t.Amount)).Select(group => $"{group.Key}: INR {group.Sum(x => x.Amount):N2}"));
        PdfExportService.Export(dialog.FileName, "Expense Manager - Financial Report", lines);
        MessageBox.Show("Your report was exported as a PDF successfully.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_data.Count == 0) { MessageBox.Show("There is no report data to export yet.", "Export report", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var dialog = new SaveFileDialog { Filter = "Excel Workbooks (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv", FileName = $"expense-report-{DateTime.Today:yyyy-MM-dd}.xlsx" };
        if (dialog.ShowDialog() != true) return;
        var income = _data.Where(x => x.Type == "Income").Sum(x => x.Amount);
        var expense = _data.Where(x => x.Type == "Expense").Sum(x => x.Amount);
        var categories = _data.Where(x => x.Type == "Expense").GroupBy(x => x.Category).OrderByDescending(x => x.Sum(t => t.Amount)).Select(g => new KeyValuePair<string, decimal>(g.Key, g.Sum(x => x.Amount))).ToList();
        ExcelExportService.ExportReport(dialog.FileName, "Expense Manager - Financial Report", income, expense, categories, ReportSummary.Text);
        MessageBox.Show("Your report was exported as an Excel spreadsheet successfully.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private sealed record ReportRow(string Label, decimal Value, double Percent) { public string Display => $"\u20B9{Value:N0}"; }
}
