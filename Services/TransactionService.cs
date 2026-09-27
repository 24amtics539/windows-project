using ExpenseManager.Data;
using ExpenseManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Services;

public class TransactionService
{
    public async Task<List<Transaction>> GetAsync(int userId) { await using var db = new AppDbContext(); return await db.Transactions.Where(x => x.UserId == userId).OrderByDescending(x => x.Date).ThenByDescending(x => x.TransactionId).AsNoTracking().ToListAsync(); }
    public async Task SaveAsync(Transaction item) { await using var db = new AppDbContext(); if (item.TransactionId == 0) db.Transactions.Add(item); else db.Transactions.Update(item); await db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id, int userId) { await using var db = new AppDbContext(); var item = await db.Transactions.FirstOrDefaultAsync(x => x.TransactionId == id && x.UserId == userId); if (item != null) { db.Remove(item); await db.SaveChangesAsync(); } }
}
