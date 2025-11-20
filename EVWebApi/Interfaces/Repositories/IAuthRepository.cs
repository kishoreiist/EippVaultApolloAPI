using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<UserMfaToken?> GetMfaTokenAsync(int userId);
        Task AddMfaTokenAsync(UserMfaToken token);
        Task SaveChangesAsync();
    }

}
