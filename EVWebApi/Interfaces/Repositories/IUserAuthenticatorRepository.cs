public interface IUserAuthenticatorRepository
{
    // Add method signatures as needed, e.g.:
    Task SaveAsync(UserAuthenticator authenticator);
    Task<UserAuthenticator?> GetByUserIdAsync(int userId);
}