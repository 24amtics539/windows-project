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

public partial class TransactionsView : UserControl
{
    private readonly User _user;
    private readonly TransactionService _service = new();
    private List<Transaction> _all = new();

    public TransactionsView(User user)
    {
        InitializeComponent();
        _user = user;
        FilterBox.SelectedIndex = 0;
        CategoryFilter.ItemsSource = new[] { "All categories", "Food", "Travel", "Shopping", "Bills", "Education", "Health", "Entertainment", "Rent", "Salary", "Freelance", "Business", "Bonus", "Investment", "Other" };
        PaymentFilter.ItemsSource = new[] { "All payment methods", "Cash", "UPI", "Credit Card", "Debit Card", "Bank Transfer" };
        CategoryFilter.SelectedIndex = 0;
        PaymentFilter.SelectedIndex = 0;
        _ = Load();
    }

    private async Task Load()
    {
        try { _all = await _service.GetAsync(_user.UserId); ApplyFilter(); }
        catch { MessageBox.Show("Transactions could not be loaded. Please try again.", "Expense Manager", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (SearchHint != null && SearchBox != null) SearchHint.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        ApplyFilter();
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e) => SearchHint.Visibility = Visibility.Collapsed;
    private void SearchBox_LostFocus(object sender, RoutedEventArgs e) => SearchHint.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

    private void ApplyFilter()
    {
        var query = SearchBox?.Text?.Trim() ?? "";
        var type = (FilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var category = CategoryFilter?.SelectedItem?.ToString();
        var payment = PaymentFilter?.SelectedItem?.ToString();
        var filtered = _all.Where(x =>
            (string.IsNullOrWhiteSpace(query) || $"{x.Name} {x.Category} {x.Description} {x.PaymentMethod}".Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(type) || type == "All types" || x.Type == type) &&
            (string.IsNullOrEmpty(category) || category == "All categories" || x.Category == category) &&
            (string.IsNullOrEmpty(payment) || payment == "All payment methods" || x.PaymentMethod == payment) &&
            (!FromDate.SelectedDate.HasValue || x.Date.Date >= FromDate.SelectedDate.Value.Date) &&
            (!ToDate.SelectedDate.HasValue || x.Date.Date <= ToDate.SelectedDate.Value.Date)).ToList();
        Table.ItemsSource = filtered;
        ResultCount.Text = $"{filtered.Count} transaction{(filtered.Count == 1 ? "" : "s")} found";
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        FilterBox.SelectedIndex = 0;
        CategoryFilter.SelectedIndex = 0;
        PaymentFilter.SelectedIndex = 0;
        FromDate.SelectedDate = null;
        ToDate.SelectedDate = null;
        ApplyFilter();
    }

    private void NewTransaction_Click(object sender, RoutedEventArgs e) => FindShell()?.Navigate("Expense");

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Transaction transaction) return;
        if (MessageBox.Show($"Delete {transaction.Name}? This action cannot be undone.", "Delete transaction", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _service.DeleteAsync(transaction.TransactionId, _user.UserId);
            await Load();
        }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        var transaction = (sender as Button)?.Tag as Transaction ?? Table.SelectedItem as Transaction;
        if (transaction is null) return;

        var name = new TextBox { Text = transaction.Name };
        var amount = new TextBox { Text = transaction.Amount.ToString(CultureInfo.InvariantCulture) };
        var category = new ComboBox { ItemsSource = new[] { "Food", "Travel", "Shopping", "Bills", "Education", "Health", "Entertainment", "Rent", "Salary", "Freelance", "Business", "Bonus", "Investment", "Other" }, SelectedItem = transaction.Category };
        var payment = new ComboBox { ItemsSource = new[] { "Cash", "UPI", "Credit Card", "Debit Card", "Bank Transfer" }, SelectedItem = transaction.PaymentMethod };
        var date = new DatePicker { SelectedDate = transaction.Date };
        var description = new TextBox { Text = transaction.Description, Height = 70, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalContentAlignment = VerticalAlignment.Top };
        var message = new TextBlock { Foreground = (Brush)FindResource("DangerBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
        var save = new Button { Content = "Save changes", Style = (Style)FindResource("PrimaryButton"), Margin = new Thickness(0, 16, 0, 0) };

        var form = new StackPanel { Margin = new Thickness(24) };
        form.Children.Add(new TextBlock { Text = "Edit transaction", FontSize = 22, FontWeight = FontWeights.SemiBold });
        AddField(form, "Name", name); AddField(form, "Amount", amount); AddField(form, "Category", category); AddField(form, "Payment method", payment); AddField(form, "Date", date); AddField(form, "Description", description);
        form.Children.Add(message); form.Children.Add(save);
        var dialog = new Window { Title = "Edit transaction", Content = form, Width = 390, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this), Background = (Brush)FindResource("BackgroundBrush") };
        save.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text) || !decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value <= 0) { message.Text = "Enter a name and a valid amount."; return; }
            transaction.Name = name.Text.Trim(); transaction.Amount = value; transaction.Category = category.SelectedItem?.ToString() ?? "Other"; transaction.PaymentMethod = payment.SelectedItem?.ToString() ?? "Cash"; transaction.Date = date.SelectedDate ?? DateTime.Today; transaction.Description = description.Text.Trim();
            await _service.SaveAsync(transaction); dialog.Close(); await Load();
        };
        dialog.ShowDialog();
    }

    private static void AddField(Panel panel, string label, Control control)
    {
        panel.Children.Add(new TextBlock { Text = label.ToUpperInvariant(), FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.FindResource("MutedBrush"), Margin = new Thickness(0, 12, 0, 5) });
        panel.Children.Add(control);
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var rows = (Table.ItemsSource as IEnumerable<Transaction>)?.ToList() ?? new List<Transaction>();
        if (rows.Count == 0) { MessageBox.Show("There are no transactions to export with the current filters.", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var dialog = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = $"transactions-{DateTime.Today:yyyy-MM-dd}.pdf" };
        if (dialog.ShowDialog() != true) return;
        var lines = rows.Select(item => $"{item.Date:dd MMM yyyy} | {item.Type} | {item.Name} | {item.Category} | INR {item.Amount:N2} | {item.PaymentMethod}");
        PdfExportService.Export(dialog.FileName, "Expense Manager - Transactions", lines);
        MessageBox.Show("Your transactions were exported as a PDF successfully.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var rows = (Table.ItemsSource as IEnumerable<Transaction>)?.ToList() ?? new List<Transaction>();
        if (rows.Count == 0) { MessageBox.Show("There are no transactions to export with the current filters.", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var dialog = new SaveFileDialog { Filter = "Excel Workbooks (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv", FileName = $"transactions-{DateTime.Today:yyyy-MM-dd}.xlsx" };
        if (dialog.ShowDialog() != true) return;
        ExcelExportService.ExportTransactions(dialog.FileName, "Expense Manager - Transactions", rows);
        MessageBox.Show("Your transactions were exported as an Excel spreadsheet successfully.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private ShellView? FindShell()
    {
        DependencyObject? current = this;
        while (current != null) { if (current is ShellView shell) return shell; current = VisualTreeHelper.GetParent(current); }
        return null;
    }
}
