using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSmart.Core.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; }

    [Required]
    public byte[] PasswordHash { get; set; }

    [Required]
    public byte[] PasswordSalt { get; set; }

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "User";

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginDate { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation properties
    public ICollection<InventoryAlert> ResolvedAlerts { get; set; }
}