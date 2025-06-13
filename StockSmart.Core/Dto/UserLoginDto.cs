using System.ComponentModel.DataAnnotations;

namespace StockSmart.Core.Dtos;

public class UserLoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}