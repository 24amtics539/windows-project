# Expense Manager

A beginner-friendly WPF desktop expense tracker built with C#, .NET 8, SQLite, and Entity Framework Core.

## Features

- Local registration/login using PBKDF2 password hashing
- Gmail-only registration validation using `@gmail.com`
- Per-user data isolation
- Add income and expenses, categories, dates, payment methods, descriptions
- Dashboard totals, recent transactions, search, filtering, edit, delete confirmation, and refreshable reports
- SQLite database created automatically on first launch
- Centralized colors, styles, reusable cards/buttons, DynamicResource brushes, and collection binding

## Run

Open `ExpenseManager.sln` or `ExpenseManager.csproj` in Visual Studio with the .NET desktop workload installed. Or use `dotnet restore`, `dotnet build`, and `dotnet run`. `ExpenseManager.db` is created automatically by `AppDbContext`.

## Structure

`Models` contains entities; `Data` contains the EF Core context; `Services` contains authentication, hashing, and persistence; `Views` contains navigable WPF screens; `Resources` contains palette/styles; `Commands` contains `RelayCommand`.

## How it works

Registration checks email uniqueness and hashes the password before saving. Login identifies the current user, and every transaction operation includes that user's `UserId`, so accounts remain isolated.

## WPF concepts demonstrated

XAML, Grid/StackPanel/DockPanel/UniformGrid, ContentControl navigation, routed events, DataGrid/ListView/ItemsControl binding, resource dictionaries, StaticResource/DynamicResource, styles, a rounded ControlTemplate, string formatting, filtering, password security, application startup, and reusable service/view separation.

## Future improvements

Dark-theme switching, monthly charts, CSV/PDF export, richer validation adorners, and EF migrations.
