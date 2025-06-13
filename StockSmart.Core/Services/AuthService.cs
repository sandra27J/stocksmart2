using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StockSmart.Core.Interfaces;
using StockSmart.Core.Models;

namespace StockSmart.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, 
                      IConfiguration config,
                      ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> Register(User user, string password)
    {
        try
        {
            if (await _userRepository.UserExists(user.Username))
                return new AuthResponse(false, "Username already exists");

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.DateCreated = DateTime.UtcNow;

            await _userRepository.AddUserAsync(user);

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            await SaveRefreshToken(user.Id, refreshToken);

            return new AuthResponse(true, token, refreshToken.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Register");
            return new AuthResponse(false, "Registration failed");
        }
    }

    public async Task<AuthResponse> Login(string username, string password)
    {
        try
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null || !VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return new AuthResponse(false, "Invalid credentials");

            user.LastLoginDate = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            await SaveRefreshToken(user.Id, refreshToken);

            return new AuthResponse(true, token, refreshToken.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Login");
            return new AuthResponse(false, "Login failed");
        }
    }

    private async Task SaveRefreshToken(int userId, RefreshToken refreshToken)
    {
        await _userRepository.SaveRefreshTokenAsync(userId, refreshToken);
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = creds,
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow
        };
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        if (storedHash.Length != 64 || storedSalt.Length != 128)
        {
            _logger.LogWarning("Invalid password hash or salt length");
            return false;
        }

        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != storedHash[i])
            {
                _logger.LogWarning("Password hash mismatch");
                return false;
            }
        }
        
        return true;
    }

    public async Task<AuthResponse> RefreshToken(string token, string refreshToken)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return new AuthResponse(false, "Invalid user");

            var storedRefreshToken = await _userRepository.GetRefreshTokenAsync(userId);
            if (storedRefreshToken == null || storedRefreshToken.Token != refreshToken || 
                storedRefreshToken.Expires <= DateTime.UtcNow)
                return new AuthResponse(false, "Invalid refresh token");

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            await _userRepository.RevokeRefreshTokenAsync(userId);
            await SaveRefreshToken(userId, newRefreshToken);

            return new AuthResponse(true, newToken, newRefreshToken.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResponse(false, "Token refresh failed");
        }
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
        return principal;
    }
}