

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using EVWebApi.Interfaces.Services;

namespace EVWebApi.Filters
{
    public class RequiresPermissionAttribute : TypeFilterAttribute
    {
        public RequiresPermissionAttribute(string permissionKey)
            : base(typeof(RequiresPermissionFilter))
        {
            Arguments = new object[] { permissionKey };
        }
    }

    public class RequiresPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionKey;
        private readonly IPermissionService _permissionService;

        public RequiresPermissionFilter(string permissionKey, IPermissionService permissionService)
        {
            _permissionKey = permissionKey;
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            var roleIdClaim = user.FindFirstValue(ClaimTypes.Role);

            if (roleIdClaim == null || !int.TryParse(roleIdClaim, out int roleId))
            {
                context.Result = new ForbidResult();
                return;
            }
            var hasPermission = await _permissionService.HasPermissionAsync(roleId, _permissionKey);

            if (!hasPermission)
            {
                
                context.Result = new ForbidResult(); 
            }
        }
    }
}