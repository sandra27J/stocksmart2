using Moq;
using StockSmart.Core.Interfaces;
using StockSmart.Core.Models;
using StockSmart.Core.Services;
using Xunit;

namespace StockSmart.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly AuthService _authService;
    private readonly IConfiguration _config;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "SuperSecretKey1234567890SuperSecretKey1234567890"},
                {"Jwt:Issuer", "StockSmart"},
                {"Jwt:Audience", "StockSmartUsers"}
            })
            .Build();
        _authService = new AuthService(_config, _mockUserRepo.Object);
    }

    // Register Tests
    [Fact]
    public async Task Register_ReturnsSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var newUser = new User { Username = "testuser", Email = "test@example.com" };
        _mockUserRepo.Setup(x => x.UserExists("testuser")).ReturnsAsync(false);
        _mockUserRepo.Setup(x => x.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.Register(newUser, "Test@123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Register_ReturnsFailure_WhenUserExists()
    {
        // Arrange
        var newUser = new User { Username = "existinguser" };
        _mockUserRepo.Setup(x => x.UserExists("existinguser")).ReturnsAsync(true);

        // Act
        var result = await _authService.Register(newUser, "Test@123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User already exists.", result.Message);
    }

    [Fact]
    public async Task Register_CreatesPasswordHash_WhenSuccessful()
    {
        // Arrange
        var newUser = new User { Username = "testuser" };
        _mockUserRepo.Setup(x => x.UserExists("testuser")).ReturnsAsync(false);
        User capturedUser = null;
        _mockUserRepo.Setup(x => x.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        await _authService.Register(newUser, "Test@123");

        // Assert
        Assert.NotNull(capturedUser);
        Assert.NotNull(capturedUser.PasswordHash);
        Assert.NotNull(capturedUser.PasswordSalt);
    }

    // Login Tests
    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsValid()
    {
        // Arrange
        var testUser = new User 
        { 
            Username = "validuser", 
            PasswordHash = new byte[64], 
            PasswordSalt = new byte[128] 
        };
        _mockUserRepo.Setup(x => x.GetUserByUsernameAsync("validuser")).ReturnsAsync(testUser);
        _mockUserRepo.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.Login("validuser", "Test@123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Login_ReturnsFailure_WhenUserNotFound()
    {
        // Arrange
        _mockUserRepo.Setup(x => x.GetUserByUsernameAsync("invaliduser")).ReturnsAsync((User)null);

        // Act
        var result = await _authService.Login("invaliduser", "Test@123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Username or password is incorrect.", result.Message);
    }

    [Fact]
    public async Task Login_UpdatesLastLogin_WhenSuccessful()
    {
        // Arrange
        var testUser = new User 
        { 
            Username = "validuser", 
            PasswordHash = new byte[64], 
            PasswordSalt = new byte[128] 
        };
        _mockUserRepo.Setup(x => x.GetUserByUsernameAsync("validuser")).ReturnsAsync(testUser);
        User updatedUser = null;
        _mockUserRepo.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => updatedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        await _authService.Login("validuser", "Test@123");

        // Assert
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginDate);
    }

    // UserExists Tests
    [Fact]
    public async Task UserExists_ReturnsTrue_WhenUserFound()
    {
        // Arrange
        _mockUserRepo.Setup(x => x.UserExists("existinguser")).ReturnsAsync(true);

        // Act
        var result = await _authService.UserExists("existinguser");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserExists_ReturnsFalse_WhenUserNotFound()
    {
        // Arrange
        _mockUserRepo.Setup(x => x.UserExists("nonexistentuser")).ReturnsAsync(false);

        // Act
        var result = await _authService.UserExists("nonexistentuser");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserExists_PropagatesRepositoryException()
    {
        // Arrange
        _mockUserRepo.Setup(x => x.UserExists("erroruser"))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _authService.UserExists("erroruser"));
    }
}