using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using ExpenseManager.Services;

namespace ExpenseManager.Views;

public partial class RegisterView : UserControl
{
    public Action? OnBack { get; set; }
    public Action? OnCreated { get; set; }
    public AuthenticationService? Auth { get; set; }
    public RegisterView() { InitializeComponent(); }
    private async void Create_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || PasswordBox.Password.Length < 6 || PasswordBox.Password != ConfirmBox.Password) { Message.Text = "Please complete the form. Passwords must match and be 6+ characters."; return; }
        try { _ = new MailAddress(EmailBox.Text); } catch { Message.Text = "Please enter a valid Gmail address."; return; }
        if (!EmailBox.Text.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)) { Message.Text = "Please use a Gmail address ending with @gmail.com."; return; }
        var result = await Auth!.RegisterAsync(NameBox.Text, EmailBox.Text, PasswordBox.Password, PhoneBox.Text);
        if (!result.Ok) { Message.Text = result.Message; return; }
        Message.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush"); Message.Text = result.Message; await Task.Delay(900); OnCreated?.Invoke();
    }
    private void Back_Click(object s, RoutedEventArgs e) => OnBack?.Invoke();
}
