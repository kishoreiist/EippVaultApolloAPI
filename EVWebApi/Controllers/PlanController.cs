using EVWebApi.Data;
using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PlanController:BaseController
    {
        private readonly IStorageQuotaService _planService;

        public PlanController(IStorageQuotaService planService)
        {
            _planService = planService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPlanUsage()
        {
            var result = await _planService.GetPlanUsageAsync();
            return Ok(new { data = result });
        }
    }
    
}
