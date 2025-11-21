using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    public abstract class BaseController : ControllerBase
    {

    protected int CurrentUserId =>
            int.Parse(User.FindFirst("userId")?.Value ?? "0");

    }
}
