using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSmart.Core.Models;

public class InventoryAlert
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product Product { get; set; }

    [Required]
    [StringLength(200)]
    public string Message { get; set; } = "Stock level is below threshold";

    [Required]
    public AlertLevel Level { get; set; } = AlertLevel.Warning;

    [Required]
    public DateTime AlertDate { get; set; } = DateTime.UtcNow;

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedDate { get; set; }

    [StringLength(100)]
    public string ResolvedBy { get; set; }
}

public enum AlertLevel
{
    Info = 1,
    Warning = 2,
    Critical = 3
}