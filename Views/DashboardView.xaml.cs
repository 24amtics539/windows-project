using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExpenseManager.Models;
using ExpenseManager.Services;

namespace ExpenseManager.Views;

public partial class DashboardView : UserControl
{
    private readonly User _user;

    public DashboardView(User user)
    {
        InitializeComponent();
        _user = user;
        Welcome.Text = $"Welcome, {user.FullName.Split(' ')[0]}";
        Period.Text = $"Your financial overview for {DateTime.Today:MMMM yyyy}";
        _ = Load();
    }

    private async Task Load()
    {
        var data = await new TransactionService().GetAsync(_user.UserId);
        var income = data.Where(x => x.Type == "Income").Sum(x => x.Amount);
        var expense = data.Where(x => x.Type == "Expense").Sum(x => x.Amount);
        var savings = income == 0 ? 0 : Math.Max(0, (income - expense) / income * 100);

        Balance.Text = $"\u20B9{income - expense:N2}";
        Income.Text = $"\u20B9{income:N2}";
        Expense.Text = $"\u20B9{expense:N2}";
        SavingsRate.Text = $"{savings:N0}%";
        Count.Text = $"{data.Count} transaction{(data.Count == 1 ? "" : "s")} recorded";
        RecentCount.Text = $"Showing {Math.Min(5, data.Count)} of {data.Count}";
        Recent.ItemsSource = data.Take(5);

        BuildCategories(data, expense);
        BuildMonths(data);
    }

    private void BuildCategories(IEnumerable<Transaction> data, decimal totalExpense)
    {
        var rows = data.Where(x => x.Type == "Expense")
            .GroupBy(x => x.Category)
            .Select(g => new ChartRow(g.Key, g.Sum(x => x.Amount), totalExpense == 0 ? 0 : (double)(g.Sum(x => x.Amount) / totalExpense * 100)))
            .OrderByDescending(x => x.Value).Take(5).ToList();
        CategoryChart.ItemsSource = rows;
        NoCategories.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BuildMonths(IEnumerable<Transaction> data)
    {
        var months = Enumerable.Range(0, 6).Select(i => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5 + i)).ToList();
        var values = months.Select(month =>
        {
            var expense = data.Where(x => x.Type == "Expense" && x.Date.Year == month.Year && x.Date.Month == month.Month).Sum(x => x.Amount);
            return new { Month = month, Value = expense };
        }).ToList();
        var max = values.Max(x => x.Value);
        MonthlyChart.ItemsSource = values.Select(x => new ChartRow(x.Month.ToString("MMM"), x.Value, max == 0 ? 0 : (double)(x.Value / max * 100))).ToList();
    }

    private void QuickAdd_Click(object sender, RoutedEventArgs e)
    {
        var type = (string)((Button)sender).Tag;
        DependencyObject? current = this;
        while (current != null)
        {
            if (current is ShellView shell) { shell.Navigate(type); return; }
            current = VisualTreeHelper.GetParent(current);
        }
    }

    private sealed record ChartRow(string Label, decimal Value, double Percent)
    {
        public string Display => $"\u20B9{Value:N0}";
    }
}
