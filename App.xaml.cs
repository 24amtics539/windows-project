using System.Windows;
using ExpenseManager.Data;

namespace ExpenseManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e) { using var db = new AppDbContext(); db.Database.EnsureCreated(); base.OnStartup(e); }
}
