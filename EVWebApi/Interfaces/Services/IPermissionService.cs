namespace EVWebApi.Interfaces.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int roleId, string permissionKey);
    }
}
