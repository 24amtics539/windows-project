using System.ComponentModel.DataAnnotations;

namespace ExpenseManager.Models;

public class Transaction
{
    [Key] public int TransactionId { get; set; }
    public int UserId { get; set; }
    [Required] public string Type { get; set; } = "Expense";
    [Required] public string Name { get; set; } = "";
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
