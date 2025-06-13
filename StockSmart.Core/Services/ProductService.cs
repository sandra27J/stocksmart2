using StockSmart.Core.Interfaces;
using StockSmart.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StockSmart.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryAlertService _alertService;

    public ProductService(IProductRepository productRepository, IInventoryAlertService alertService)
    {
        _productRepository = productRepository;
        _alertService = alertService;
    }

    public async Task<ServiceResponse<List<Product>>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllProductsAsync();
        return new ServiceResponse<List<Product>> { Data = products };
    }

    public async Task<ServiceResponse<Product>> GetProductByIdAsync(int id)
    {
        var product = await _productRepository.GetProductByIdAsync(id);
        
        if (product == null)
        {
            return new ServiceResponse<Product> 
            { 
                Success = false, 
                Message = "Product not found." 
            };
        }

        return new ServiceResponse<Product> { Data = product };
    }

    public async Task<ServiceResponse<Product>> AddProductAsync(Product product)
    {
        await _productRepository.AddProductAsync(product);
        
        // Check for low stock on new product
        if (product.Quantity <= product.LowStockThreshold)
        {
            await _alertService.GenerateLowStockAlert(product);
        }

        return new ServiceResponse<Product> { Data = product };
    }

    public async Task<ServiceResponse<Product>> UpdateProductAsync(Product product)
    {
        var existingProduct = await _productRepository.GetProductByIdAsync(product.Id);
        if (existingProduct == null)
        {
            return new ServiceResponse<Product> 
            { 
                Success = false, 
                Message = "Product not found." 
            };
        }

        // Check if stock level crossed threshold
        if (existingProduct.Quantity > existingProduct.LowStockThreshold && 
            product.Quantity <= product.LowStockThreshold)
        {
            await _alertService.GenerateLowStockAlert(product);
        }

        await _productRepository.UpdateProductAsync(product);
        return new ServiceResponse<Product> { Data = product };
    }

    public async Task<ServiceResponse<bool>> DeleteProductAsync(int id)
    {
        var product = await _productRepository.GetProductByIdAsync(id);
        if (product == null)
        {
            return new ServiceResponse<bool> 
            { 
                Success = false, 
                Message = "Product not found." 
            };
        }

        await _productRepository.DeleteProductAsync(id);
        return new ServiceResponse<bool> { Data = true };
    }

    public async Task<ServiceResponse<List<Product>>> GetLowStockProductsAsync()
    {
        var products = await _productRepository.GetLowStockProductsAsync();
        return new ServiceResponse<List<Product>> { Data = products };
    }
}