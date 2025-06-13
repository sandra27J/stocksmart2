using Moq;
using StockSmart.Core.Interfaces;
using StockSmart.Core.Models;
using StockSmart.Core.Services;
using Xunit;

namespace StockSmart.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly Mock<IInventoryAlertService> _mockAlertService;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _mockAlertService = new Mock<IInventoryAlertService>();
        _productService = new ProductService(_mockProductRepo.Object, _mockAlertService.Object);
    }

    // GetAllProductsAsync Tests
    [Fact]
    public async Task GetAllProductsAsync_ReturnsProducts_WhenExist()
    {
        // Arrange
        var testProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1" },
            new Product { Id = 2, Name = "Product 2" }
        };
        _mockProductRepo.Setup(x => x.GetAllProductsAsync()).ReturnsAsync(testProducts);

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task GetAllProductsAsync_ReturnsEmptyList_WhenNoProducts()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetAllProductsAsync()).ReturnsAsync(new List<Product>());

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetAllProductsAsync_ReturnsFailure_WhenRepositoryFails()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetAllProductsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _productService.GetAllProductsAsync());
    }

    // GetProductByIdAsync Tests
    [Fact]
    public async Task GetProductByIdAsync_ReturnsProduct_WhenExists()
    {
        // Arrange
        var testProduct = new Product { Id = 1, Name = "Test Product" };
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(testProduct);

        // Act
        var result = await _productService.GetProductByIdAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test Product", result.Data.Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsFailure_WhenNotFound()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync((Product)null);

        // Act
        var result = await _productService.GetProductByIdAsync(1);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Product not found.", result.Message);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsFailure_WhenInvalidId()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(0)).ReturnsAsync((Product)null);

        // Act
        var result = await _productService.GetProductByIdAsync(0);

        // Assert
        Assert.False(result.Success);
    }

    // AddProductAsync Tests
    [Fact]
    public async Task AddProductAsync_ReturnsProduct_WhenSuccessful()
    {
        // Arrange
        var newProduct = new Product { Name = "New Product", Quantity = 10 };
        _mockProductRepo.Setup(x => x.AddProductAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _productService.AddProductAsync(newProduct);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New Product", result.Data.Name);
    }

    [Fact]
    public async Task AddProductAsync_GeneratesAlert_WhenLowStock()
    {
        // Arrange
        var lowStockProduct = new Product { Name = "Low Stock", Quantity = 2, LowStockThreshold = 5 };
        _mockProductRepo.Setup(x => x.AddProductAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _mockAlertService.Setup(x => x.GenerateLowStockAlert(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        await _productService.AddProductAsync(lowStockProduct);

        // Assert
        _mockAlertService.Verify(x => x.GenerateLowStockAlert(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task AddProductAsync_ThrowsException_WhenRepositoryFails()
    {
        // Arrange
        var newProduct = new Product { Name = "New Product" };
        _mockProductRepo.Setup(x => x.AddProductAsync(It.IsAny<Product>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _productService.AddProductAsync(newProduct));
    }

    // UpdateProductAsync Tests
    [Fact]
    public async Task UpdateProductAsync_ReturnsProduct_WhenSuccessful()
    {
        // Arrange
        var existingProduct = new Product { Id = 1, Name = "Existing", Quantity = 10 };
        var updatedProduct = new Product { Id = 1, Name = "Updated", Quantity = 5 };
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(existingProduct);
        _mockProductRepo.Setup(x => x.UpdateProductAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _productService.UpdateProductAsync(updatedProduct);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated", result.Data.Name);
    }

    [Fact]
    public async Task UpdateProductAsync_GeneratesAlert_WhenStockDropsBelowThreshold()
    {
        // Arrange
        var existingProduct = new Product { Id = 1, Quantity = 10, LowStockThreshold = 5 };
        var updatedProduct = new Product { Id = 1, Quantity = 4, LowStockThreshold = 5 };
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(existingProduct);
        _mockProductRepo.Setup(x => x.UpdateProductAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _mockAlertService.Setup(x => x.GenerateLowStockAlert(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        await _productService.UpdateProductAsync(updatedProduct);

        // Assert
        _mockAlertService.Verify(x => x.GenerateLowStockAlert(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ReturnsFailure_WhenProductNotFound()
    {
        // Arrange
        var updatedProduct = new Product { Id = 99 };
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(99)).ReturnsAsync((Product)null);

        // Act
        var result = await _productService.UpdateProductAsync(updatedProduct);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Product not found.", result.Message);
    }

    // DeleteProductAsync Tests
    [Fact]
    public async Task DeleteProductAsync_ReturnsSuccess_WhenProductExists()
    {
        // Arrange
        var existingProduct = new Product { Id = 1 };
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(existingProduct);
        _mockProductRepo.Setup(x => x.DeleteProductAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _productService.DeleteProductAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task DeleteProductAsync_ReturnsFailure_WhenProductNotFound()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync((Product)null);

        // Act
        var result = await _productService.DeleteProductAsync(1);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Product not found.", result.Message);
    }

    [Fact]
    public async Task DeleteProductAsync_ThrowsException_WhenRepositoryFails()
    {
        // Arrange
        var existingProduct = new Product { Id = 1 };
        _mockProductRepo.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(existingProduct);
        _mockProductRepo.Setup(x => x.DeleteProductAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _productService.DeleteProductAsync(1));
    }

    // GetLowStockProductsAsync Tests
    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsProducts_WhenExist()
    {
        // Arrange
        var lowStockProducts = new List<Product>
        {
            new Product { Id = 1, Quantity = 2, LowStockThreshold = 5 }
        };
        _mockProductRepo.Setup(x => x.GetLowStockProductsAsync()).ReturnsAsync(lowStockProducts);

        // Act
        var result = await _productService.GetLowStockProductsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsEmptyList_WhenNoneExist()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetLowStockProductsAsync()).ReturnsAsync(new List<Product>());

        // Act
        var result = await _productService.GetLowStockProductsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ThrowsException_WhenRepositoryFails()
    {
        // Arrange
        _mockProductRepo.Setup(x => x.GetLowStockProductsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _productService.GetLowStockProductsAsync());
    }
}