using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExpenseManager.Models;
using ExpenseManager.Services;

namespace ExpenseManager.Views;

public partial class ShellView : UserControl
{
    private readonly User _user;
    private readonly Action _logout;

    public ShellView(User user, Action logout)
    {
        InitializeComponent();
        _user = user;
        _logout = logout;
        UserName.Text = user.FullName;
        UserInitials.Text = string.Concat(user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0])).ToUpperInvariant();
        Show("Dashboard");
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        var key = (string)((Button)sender).Tag;
        if (key == "Logout") { _logout(); return; }
        Show(key);
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Toggle();
        ThemeLabel.Text = ThemeService.IsDark ? "Light mode" : "Dark mode";
        ThemeIcon.Text = ThemeService.IsDark ? "\uE706" : "\uE708";
    }

    public void Navigate(string key) => Show(key);

    private void Show(string key)
    {
        Page.Content = key switch
        {
            "Dashboard" => new DashboardView(_user),
            "Transactions" => new TransactionsView(_user),
            "Reports" => new ReportsView(_user),
            "Expense" => new EntryView(_user, "Expense"),
            "Income" => new EntryView(_user, "Income"),
            _ => null
        };

        PageTitle.Text = key switch { "Expense" => "Add expense", "Income" => "Add income", _ => key };
        PageSubtitle.Text = key switch
        {
            "Dashboard" => "Your financial overview",
            "Transactions" => "Review and manage your activity",
            "Reports" => "Understand where your money goes",
            _ => "Keep your finances up to date"
        };
        foreach (var button in NavItems.Children.OfType<Button>())
        {
            var active = (string)button.Tag == key;
            button.Background = active ? (Brush)FindResource("SidebarActiveBrush") : Brushes.Transparent;
            button.Foreground = active ? Brushes.White : (Brush)FindResource("SidebarTextBrush");
        }
    }
}
