using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSmart.Core.Models;
using StockSmart.Core.Services;
using StockSmart.Core.Dtos;

namespace StockSmart.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(UserRegisterDto userDto)
    {
        try
        {
            var user = new User 
            {
                Username = userDto.Username,
                Email = userDto.Email,
                Role = "User" // Default role
            };

            var response = await _authService.Register(user, userDto.Password);
            
            if (!response.Success)
                return BadRequest(response.Message);

            return Ok(response.Data); // Returns JWT token
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(UserLoginDto userDto)
    {
        try
        {
            var response = await _authService.Login(userDto.Username, userDto.Password);
            
            if (!response.Success)
                return Unauthorized(response.Message);

            // Set refresh token as HTTP-only cookie
            Response.Cookies.Append("refreshToken", 
                                 response.RefreshToken, 
                                 new CookieOptions
                                 {
                                     HttpOnly = true,
                                     Expires = DateTime.UtcNow.AddDays(7),
                                     Secure = true,
                                     SameSite = SameSiteMode.Strict
                                 });

            return Ok(new {
                Token = response.Data,
                Expires = DateTime.UtcNow.AddHours(1)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        try
        {
            // Clear refresh token cookie
            Response.Cookies.Delete("refreshToken");
            return Ok("Logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, "Internal server error");
        }
    }
}