using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs.Security;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using EVWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.FileManager.Base;

namespace EVWebApi.Controllers
{
    //[Authorize(Roles = "admin,super_admin")]
    [ApiController]
    [Route("api/security")]
    public class SecurityAdminController : BaseController
    {
        private readonly ISecurityAdminService _securityService;
        private readonly IAuditLogService _auditlogservice;
        public SecurityAdminController(ISecurityAdminService securityService, IAuditLogService auditlogservice)
        {
            _securityService = securityService;
            _auditlogservice = auditlogservice;
        }
        //get blacklisted ip
        [HttpGet("ips_status")]
        public async Task<IActionResult> GetIpsStatus([FromQuery] BlacklistQueryParameters query)
        {
            try
            {
                var ips = await _securityService.GetBlacklistedIpsAsync(query);
                string filterDetails = query.ToFilterLog();
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Security", "BlackList IPs Retrieved", null, filters: filterDetails);
                return Ok(ips);
            }
        
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Blacklisted Ip Fetch failed",
                    Error = ex.Message
                });
            }

        }
        //get lock & user
        [HttpGet("locked_users")]
        public async Task<IActionResult> GetLockedUsers([FromQuery] LockedUserQueryParameters query)
        {
            try{

            var users= await _securityService.GetLockedUsersAsync(query);
            string filterDetails = query.ToFilterLog();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Security", "Locked users Retrieved", null, filters: filterDetails);

            return Ok(users);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Locked users Fetch failed",
                    Error = ex.Message
                });
            }

        }

        //UNLOCK  USER
        [HttpPut("unlock_users/{userId}")]
        public async Task<IActionResult> UnlockUser(int userId)
        {
            try
            {
                var user = await _securityService.UnlockUserAsync(userId, CurrentUserId);
                if (user)
                {
                    await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Security", "UnLocked Users", userId.ToString());
                    return Ok("User unlocked successfully.");
                }
                else { return NoContent(); }


            }
            catch (Exception ex)
            { 
                return StatusCode(500, new
                {
                    Message = "Unlocking user failed",
                    Error = ex.Message
                });
            }
        }

        //remove blacklist
        [HttpPut("remove_blacklist/{ip}")]
        public async Task<IActionResult> RemoveBlacklistedIp(string ip)
        {
            try
            {
                await _securityService.RemoveBlackListIpAsync(ip);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Security", "Removed IP from blacklist", ip);
                return Ok(new { Message = "IP removed from blacklist." });

            }
            catch (Exception ex)    
            {  
                return StatusCode(500, new
                {
                    Message = "Removing IP from blacklist failed",
                    Error = ex.Message
                });
            }
        }




    }
}
