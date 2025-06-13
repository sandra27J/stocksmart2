namespace StockSmart.Core.Models;

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Data { get; set; } // JWT Token
    public string? RefreshToken { get; set; }

    public AuthResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public AuthResponse(bool success, string token, string refreshToken)
    {
        Success = success;
        Data = token;
        RefreshToken = refreshToken;
    }
}