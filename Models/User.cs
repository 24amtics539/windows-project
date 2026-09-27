using System.ComponentModel.DataAnnotations;

namespace ExpenseManager.Models;

public class User
{
    [Key] public int UserId { get; set; }
    [Required] public string FullName { get; set; } = "";
    [Required] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Transaction> Transactions { get; set; } = new();
}
