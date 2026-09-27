using ExpenseManager.Data;
using ExpenseManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Services;

public class AuthenticationService
{
    public async Task<(bool Ok, string Message, User? User)> RegisterAsync(string name, string email, string password, string phone)
    {
        await using var db = new AppDbContext();
        if (await db.Users.AnyAsync(x => x.Email == email.Trim().ToLower())) return (false, "That email is already registered.", null);
        var user = new User { FullName = name.Trim(), Email = email.Trim().ToLower(), PasswordHash = PasswordService.Hash(password), PhoneNumber = phone.Trim() };
        db.Users.Add(user); await db.SaveChangesAsync(); return (true, "Account created successfully!", user);
    }
    public async Task<(bool Ok, string Message, User? User)> LoginAsync(string email, string password)
    {
        await using var db = new AppDbContext();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email.Trim().ToLower());
        return user != null && PasswordService.Verify(password, user.PasswordHash) ? (true, "", user) : (false, "Email or password is incorrect.", null);
    }
}
