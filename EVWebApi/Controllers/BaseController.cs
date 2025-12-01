using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        //protected int CurrentUserId = 1;---->for testing need to remove bfr production
    protected int CurrentUserId =>
            int.Parse(User.FindFirst("userId")?.Value ?? "0");
        protected string CurrentUsername =>
             User.FindFirst("username")?.Value ?? string.Empty;

    }
}
