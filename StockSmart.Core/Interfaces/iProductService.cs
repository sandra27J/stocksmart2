public interface IProductService
{
    Task<ServiceResponse<List<Product>>> GetAllProductsAsync();
    Task<ServiceResponse<Product>> GetProductByIdAsync(int id);
    Task<ServiceResponse<Product>> AddProductAsync(Product product);
    Task<ServiceResponse<Product>> UpdateProductAsync(Product product);
    Task<ServiceResponse<bool>> DeleteProductAsync(int id);
    Task<ServiceResponse<List<Product>>> GetLowStockProductsAsync();
}