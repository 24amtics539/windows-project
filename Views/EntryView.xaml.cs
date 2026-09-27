using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExpenseManager.Models;
using ExpenseManager.Services;

namespace ExpenseManager.Views;

public partial class EntryView : UserControl
{
    private readonly User _user;
    private readonly string _type;

    public EntryView(User user, string type)
    {
        InitializeComponent();
        _user = user;
        _type = type;
        Heading.Text = type == "Expense" ? "Add a new expense" : "Add new income";
        TypeText.Text = type == "Expense" ? "Expense" : "Income";
        TypeBadge.Background = (Brush)FindResource(type == "Expense" ? "DangerLightBrush" : "SuccessLightBrush");
        TypeText.Foreground = (Brush)FindResource(type == "Expense" ? "DangerBrush" : "SuccessBrush");
        CategoryBox.ItemsSource = type == "Expense" ? new[] { "Food", "Travel", "Shopping", "Bills", "Education", "Health", "Entertainment", "Rent", "Other" } : new[] { "Salary", "Freelance", "Business", "Bonus", "Investment", "Other" };
        PaymentBox.ItemsSource = new[] { "Cash", "UPI", "Credit Card", "Debit Card", "Bank Transfer" };
        CategoryBox.SelectedIndex = 0;
        PaymentBox.SelectedIndex = 0;
        DateBox.SelectedDate = DateTime.Today;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || !decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount <= 0)
        {
            Message.Foreground = (Brush)FindResource("DangerBrush");
            Message.Text = "Enter a name and an amount greater than zero.";
            return;
        }

        await new TransactionService().SaveAsync(new Transaction { UserId = _user.UserId, Type = _type, Name = NameBox.Text.Trim(), Amount = amount, Category = CategoryBox.SelectedItem?.ToString() ?? "Other", PaymentMethod = PaymentBox.SelectedItem?.ToString() ?? "Cash", Date = DateBox.SelectedDate ?? DateTime.Today, Description = DescriptionBox.Text.Trim() });
        Message.Foreground = (Brush)FindResource("SuccessBrush");
        Message.Text = $"{_type} saved successfully.";
        NameBox.Clear(); AmountBox.Clear(); DescriptionBox.Clear(); DateBox.SelectedDate = DateTime.Today;
        NameBox.Focus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        NameBox.Clear(); AmountBox.Clear(); DescriptionBox.Clear(); DateBox.SelectedDate = DateTime.Today; CategoryBox.SelectedIndex = 0; PaymentBox.SelectedIndex = 0; Message.Text = ""; NameBox.Focus();
    }
}
