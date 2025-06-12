using Microsoft.AspNetCore.Mvc;
using Moq;
using StockSmart.API.Controllers;
using StockSmart.Core.Interfaces;
using StockSmart.Core.Models;
using Xunit;

namespace StockSmart.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _mockRepo;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockRepo = new Mock<IProductRepository>();
        _controller = new ProductsController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOkResult_WithProducts()
    {
        // Arrange
        var testProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1" },
            new Product { Id = 2, Name = "Product 2" }
        };
        
        _mockRepo.Setup(repo => repo.GetAllProductsAsync())
                .ReturnsAsync(testProducts);

        // Act
        var result = await _controller.GetAllProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProducts = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
        Assert.Equal(2, returnedProducts.Count());
    }

    [Fact]
    public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        _mockRepo.Setup(repo => repo.GetProductByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Product?)null); 

        // Act
        var result = await _controller.GetProduct(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetProduct_ReturnsProduct_WhenExists()
    {
        // Arrange
        var testProduct = new Product { Id = 1, Name = "Test Product" };
        _mockRepo.Setup(repo => repo.GetProductByIdAsync(1))
                .ReturnsAsync(testProduct);

        // Act
        var result = await _controller.GetProduct(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(testProduct, okResult.Value);
    }
}