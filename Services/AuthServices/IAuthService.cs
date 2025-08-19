using ProyectoFarmaVita.Models;

namespace ProyectoFarmaVita.Services.AuthServices
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<bool> LogoutAsync();
        Task<Persona?> GetCurrentUserAsync();
        Task<bool> IsUserInRoleAsync(string role);
        Task<List<string>> GetUserRolesAsync();
        bool IsAuthenticated { get; }
        Persona? CurrentUser { get; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Persona? User { get; set; }
        public string? Token { get; set; }
    }
}