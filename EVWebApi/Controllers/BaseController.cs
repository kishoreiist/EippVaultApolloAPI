using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVWebApi.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected int CurrentUserId =>
            int.TryParse(User.FindFirst("userId")?.Value, out var id) ? id : throw new UnauthorizedAccessException("User Id is missing");
        protected string CurrentUsername =>
             User.FindFirst("username")?.Value ?? string.Empty;

        protected string CurrentUserFullname =>
            User.FindFirst("name")?.Value ?? string.Empty;

        protected string CurrentUserType =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
