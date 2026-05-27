namespace EVWebApi.Interfaces.Services
{
    public interface ICloudFareTurnstileService
    {
        Task<bool> ValidateAsync(string token);
    }
}
