using StockSmart.Core.Models;

namespace StockSmart.Core.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetProductByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(int id);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
}