namespace StockSmart.Core.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int LowStockThreshold { get; set; } = 5;
}