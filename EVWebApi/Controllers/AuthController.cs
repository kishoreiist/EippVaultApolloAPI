using EVWebApi.Services;
using EVWebAPI.Models;
using EVWebAPI.Services;
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

        private readonly ILogger<AuthController> _logger;
        public AuthController(IAuthService authService, IMfaService mfaService, ILogger<AuthController> logger)
        {
            ArgumentNullException.ThrowIfNull(authService);
            ArgumentNullException.ThrowIfNull(mfaService);
            ArgumentNullException.ThrowIfNull(logger);

            _authService = authService;
            _mfaService = mfaService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _authService.AuthenticateAsync(request.Email, request.Password);
                if (result == "MFA_REQUIRED")
                {
                    _logger.LogInformation("MFA required for {Email}", request.Email);
                    // 202 Accepted: MFA flow was started (token generation & sending)
                    return Accepted(new { status = "MFA_REQUIRED" });
                }

                if (result is null)
                {
                    _logger.LogWarning("Unauthorized login attempt for {Email}", request.Email);
                    return Unauthorized();
                }

                _logger.LogInformation("User {Email} authenticated successfully", request.Email);
                return Ok(new { token = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request?.Email);
                return Problem(detail: "An unexpected error occurred while processing the login request.",
                               statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("mfa/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var success = await _mfaService.VerifyTokenAsync(request.Email, request.Token);
                if (!success)
                {
                    _logger.LogWarning("Failed MFA verification for {Email}", request.Email);
                    return Unauthorized();
                }

                var jwt = await _authService.GenerateJwtAfterMfaAsync(request.Email);
                _logger.LogInformation("MFA verified and JWT issued for {Email}", request.Email);
                return Ok(new { token = jwt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MFA verification for {Email}", request?.Email);
                return Problem(detail: "An unexpected error occurred while verifying MFA.",
                               statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
