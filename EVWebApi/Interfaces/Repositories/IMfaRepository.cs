namespace EVWebApi.Interfaces.Repositories
{
    public interface IMfaRepository
    {
        Task SaveMfaTokenAsync(UserMfaToken token);
        Task<string> GetMfaTokenAsync(int userId);

        Task<UserMfaToken?> GetValidTokenAsync(int userId, string token);
    }

}

