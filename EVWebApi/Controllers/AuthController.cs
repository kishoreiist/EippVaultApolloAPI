using EVWebApi.DTOs;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Services;
using EVWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EVWebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMfaService _mfaService;
        private readonly IUserRepository _userRepo;
        private readonly IUserService _userService;

        private readonly ILogger<AuthController> _logger;
        public AuthController(IAuthService authService, IMfaService mfaService, IUserRepository userRepo, ILogger<AuthController> logger, IUserService userService)
        {
            ArgumentNullException.ThrowIfNull(authService);
            ArgumentNullException.ThrowIfNull(mfaService);
            ArgumentNullException.ThrowIfNull(userRepo);
            ArgumentNullException.ThrowIfNull(logger);

            _authService = authService;
            _mfaService = mfaService;
            _userRepo = userRepo;
            _logger = logger;
            _userService = userService;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new BadRequestException("Invalid request");
            if (!ModelState.IsValid) 
                throw new ValidationException("Invalid login payload");

          
                var result = await _authService.AuthenticateAsync(request.Email, request.Password);
                if (result == "MFA_REQUIRED")
                {
                    _logger.LogInformation("MFA required for {Email}", request.Email);
                    // 202 Accepted: MFA flow was started (token generation & sending)

                    return Accepted(new
                    {
                        status = "MFA_REQUIRED"
                    });
                }

                if (result is null)
                {
                    throw new AuthenticationException("Invalid email or password");
                }

                _logger.LogInformation("User {Email} authenticated successfully", request.Email);
                return Ok(new { token = result });
           
        }


        [HttpPost("enable-mfa")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Method))
                throw new BadRequestException("Invalid MFA setup request.");

            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
                throw new NotFoundException("User not found");

            if (request.Method.Equals("GOOGLE", StringComparison.OrdinalIgnoreCase))
            {
                var issuer = string.IsNullOrWhiteSpace(request.Issuer) ? "MyApp" : request.Issuer;


                    if (user.MfaEnabled && user.MfaMethod == MfaMethod.authenticator)
                    {
                        return Ok(new
                        {
                            status = "already_enabled",
                            message = "MFA is already enabled for this account.",
                            qrCodeDataUrl= "already_enabled"
                        });
                    }


                    // Generate QR as base64 image (no prefix)
                    var base64Png = await _mfaService.GenerateQrCodeAsync(user.UserId, user.Email);

                // Persist method choice (optional but recommended)
                user.MfaMethod = MfaMethod.authenticator;
                user.MfaEnabled = true; // or set true after first successful verification
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();

                // Optional: return a data URL to simplify frontend
                var dataUrl = $"data:image/png;base64,{base64Png}";

                return Ok(new
                {
                    qrCodeBase64 = base64Png,     // raw base64 (PNG)
                    qrCodeDataUrl = dataUrl,      // convenient for <img src={...}>
                    issuer
                });
            }
            else if (request.Method.Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                await _mfaService.GenerateAndSendTokenAsync(user);

                user.MfaMethod = MfaMethod.email;
                user.MfaEnabled = true; // or enable after first successful verification
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();

                return Ok(new { message = "Email MFA enabled. A verification code has been sent to your email." });
            }
            else
            {
                throw new BadRequestException("Invalid MFA method.");
            }
         
        }

        [HttpPost("mfa/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new BadRequestException("Invalid MFA verification request");

            if (!ModelState.IsValid)
                throw new ValidationException("Invalid payload");

            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
                throw new AuthenticationException("Invalid MFA user");

            bool success = false;

                if (request.Method.Equals("GOOGLE", StringComparison.OrdinalIgnoreCase))
                {
                    // Use the user's email and the code as token for verification
                    success = await _mfaService.VerifyTotpAsync(user.UserId, request.Code);                   

                }
                else if (request.Method.Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    success = await _mfaService.VerifyTokenAsync(request.Email, request.Code);
                }
                else
                {
                    return BadRequest(new { message = "Invalid MFA method" });
                }
                if (!success)
                {
                    _logger.LogWarning("Failed MFA verification for {Email} using {Method}", request.Email, request.Method);
                    return Unauthorized(new { message = "Invalid MFA code" });
                }

            var jwt = await _authService.GenerateJwtAfterMfaAsync(request.Email);
            var userDto = await _userService.GetByIdAsync(user.UserId);
            _logger.LogInformation("MFA verified and JWT issued for {Email} using {Method}", request.Email, request.Method);

            return Ok(new { token = jwt, user = userDto });
           
        }        

    }
}
