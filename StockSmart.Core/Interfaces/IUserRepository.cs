using StockSmart.Core.Models;

namespace StockSmart.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> UserExists(string username);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task SaveRefreshTokenAsync(int userId, RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenAsync(int userId);
    Task RevokeRefreshTokenAsync(int userId);
}