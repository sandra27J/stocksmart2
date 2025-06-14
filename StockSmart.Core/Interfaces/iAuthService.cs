public interface IAuthService
{
    Task<ServiceResponse<string>> Register(User user, string password);
    Task<ServiceResponse<string>> Login(string username, string password);
    Task<bool> UserExists(string username);
}