namespace EVWebApi.Interfaces.Repositories
{
    public interface IMfaRepository
    {
        Task SaveMfaTokenAsync(int userId, string code);
        Task<string> GetMfaTokenAsync(int userId);

        Task<UserMfaToken?> GetValidTokenAsync(int userId, string token);
        Task MarkTokenAsUsedAsync(UserMfaToken token);
    }

}

