using System.Windows;
using System.Windows.Controls;

namespace ExpenseManager.Views;
public partial class LoginView : UserControl
{
    public Action<string,string>? OnLogin { get; set; } public Action? OnRegister { get; set; }
    public LoginView() { InitializeComponent(); }
    public void SetMessage(string text) => Message.Text = text;
    private void Login_Click(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(EmailBox.Text) || PasswordBox.Password.Length == 0) { Message.Text = "Please enter your email and password."; return; } OnLogin?.Invoke(EmailBox.Text, PasswordBox.Password); }
    private void Register_Click(object s, RoutedEventArgs e) => OnRegister?.Invoke();
}
