using System.Windows;
using ExpenseManager.Models;
using ExpenseManager.Services;
using ExpenseManager.Views;

namespace ExpenseManager;

public partial class MainWindow : Window
{
    private readonly AuthenticationService _auth = new();
    private User? _user;
    public MainWindow() { InitializeComponent(); ShowLogin(); }
    private void ShowLogin() => Root.Content = new LoginView { OnLogin = LoginAsync, OnRegister = () => Root.Content = new RegisterView { OnBack = ShowLogin, OnCreated = ShowLogin, Auth = _auth } };
    private async void LoginAsync(string email, string password)
    {
        var result = await _auth.LoginAsync(email, password);
        if (!result.Ok) { ((LoginView)Root.Content).SetMessage(result.Message); return; }
        _user = result.User; ShowShell();
    }
    private void ShowShell() => Root.Content = new ShellView(_user!, Logout);
    private void Logout() { _user = null; ShowLogin(); }
}
