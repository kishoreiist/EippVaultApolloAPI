using EVWebApi.Data;
using EVWebApi.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EVWebApi.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> HasPermissionAsync(int roleId, string permissionKey)
        {
            var rolePermissions = await _context.Roles
                .AsNoTracking()
                .Where(r => r.RoleId == roleId)
                .Select(r => r.Permissions)
                .FirstOrDefaultAsync();

            if (rolePermissions != null &&
                rolePermissions.TryGetValue(permissionKey, out bool isAllowed))
            {
                return isAllowed;
            }
            return false;
        }
    }
}
