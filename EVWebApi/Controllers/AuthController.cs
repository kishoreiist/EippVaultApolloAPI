using EVWebApi.DTOs;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
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
        private readonly string _displayName;
        private readonly IEmailSender _emailSender;
        private readonly string _frontendRoot;

        private readonly ILogger<AuthController> _logger;
        private readonly IAuditLogService _auditlogservice;
        public AuthController(IAuthService authService, IMfaService mfaService, IUserRepository userRepo, ILogger<AuthController> logger, 
            IUserService userService, IAuditLogService auditlogservice, IEmailSender emailSender, IConfiguration config)
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
            _auditlogservice = auditlogservice;
            _emailSender = emailSender;
            _frontendRoot = config["Frontend:BaseUrl"];
            _displayName = config["Email:DisplayName"];
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new BadRequestException("Invalid request");
            if (!ModelState.IsValid) 
                throw new ValidationException("Invalid login payload");

          
            var result = await _authService.AuthenticateAsync(request);
            var Reqfilters = request.ToFilterLog("Login Detail :  ");
            if (result.MfaRequired)
            {
                _logger.LogInformation("MFA required for {UserName}", result.UserName);
                // 202 Accepted: MFA flow was started (token generation & sending)
                await _auditlogservice.LogAsync(result.UserId, result.UserName, "Login", "Login Successful", null, null,null, filters: Reqfilters);
                return Accepted(new
                    {
                        status = "MFA_REQUIRED",
                        email=result.Email
                    });
            }

            if (result is null)
            {
                throw new AuthenticationException("Invalid email or password");
            }

                
            await _auditlogservice.LogAsync(result.UserId, result.UserName, "Login", "Login Successful", null, null, null,filters: Reqfilters);

            _logger.LogInformation("User {UserName} authenticated successfully", result.UserName);
            
            return Ok(new { token = result.Token, email = result.Email });
           
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


                var Reqfilters = request.ToFilterLog(" MFA Enabled Details -  ");
                await _auditlogservice.LogAsync(user.UserId, user.Username, "Login", "MFA Enabled", null, null, null, filters: Reqfilters);


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

                //user.MfaMethod = MfaMethod.email;
                user.MfaEnabled = true; // or enable after first successful verification
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();

                var Reqfilters = request.ToFilterLog("Details - ");
                await _auditlogservice.LogAsync(user.UserId, user.Username, "Login", "MFA Enabled", null, null, null,filters: Reqfilters);

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

            var filters = request.ToFilterLog(" MFA Details -  ");
            await _auditlogservice.LogAsync(user.UserId, user.Username, "Login", "MFA Verified", null, null, null, filters: filters);

            return Ok(new { token = jwt, user = userDto });
           
        }

        [HttpPost("forgot_details")]
        public async Task<IActionResult> ForgotAccountDetails([FromBody] ForgotAccountDetailsDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                throw new BadRequestException("Invalid request");

            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
                //return Ok(new { message = "If the email exists, a reset link will be sent." });
                throw new NotFoundException("Invalid Email Id.");
            var action = request.Action.ToLower().Trim();
            bool usernameSent = false;
            bool passwordSent = false;

            //forgot username

            if (action == "username")
            {
                await _emailSender.SendAsync(
                            toEmail: user.Email,
                            subject: $"{_displayName} – Username Recovery",
                            htmlBody: $@"
                                    Dear User,<br/><br/>
                                    You requested assistance with retrieving your account User Name.<br/><br/>
                                    Your User Name is: <strong>{user.Username}</strong><br/><br/>
                                    <i>If you did not request this information, you can safely ignore this email.</i><br/><br/>
                                    Regards,<br/>
                                    {_displayName} Team");
            
                await _auditlogservice.LogAsync( user.UserId,user.Username,"Login","Forgot Username",null, null, null,filters: request.ToFilterLog("Details - "));
                usernameSent = true;

            }
            //forgot password

            if (action == "password")
            {

                var token =_authService.GeneratePasswordResetJwtAsync(user);
                
               // var resetUrl = $"{_frontendRoot}reset_password?token={Uri.EscapeDataString(token)}";
                var resetUrl = $"{_frontendRoot}/reset_password?email={user.Email}&token={Uri.EscapeDataString(token)}";

                await _emailSender.SendAsync(
                   toEmail: user.Email,
                    subject: $"{_displayName} - Password Reset",
                   htmlBody: $@"
                    <p>Dear User,</p>
                    <p>
                    We received a request to reset the password for your
                    <strong>{_displayName}</strong> account.
                    </p>

                    <p>
                    Click the button below to reset your password.
                    </p>

                    <!-- Button -->
                    <table width='100%' cellpadding='0' cellspacing='0' style='margin:32px 0;'>
                      <tr>
                        <td align='left'>
                          <a href='{resetUrl}' target='_blank'
                             style='background:#2563eb;color:#ffffff;
                                    text-decoration:none;padding:14px 32px;
                                    border-radius:6px;font-size:16px;
                                    display:inline-block;'>
                            Reset Password
                          </a>
                        </td>
                      </tr>
                    </table>

                    <p style='font-size:13px;color:#6b7280;'>
                      This link will expire in <strong>30 minutes</strong>.
                    </p>
                    
                    <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0;' />

                    <p style='font-size:13px;color:#6b7280;'>
                      If you did not request a password reset, you can safely ignore
                      this email. Your password will not be changed.
                    </p>
                    <br/><br/>
                    Regards,<br/>
                    {_displayName} Team"

                );

                await _auditlogservice.LogAsync(user.UserId, user.Username, "Login", "Forgot Password", null, null, null, filters: request.ToFilterLog("Details - "));
                passwordSent = true;
            }
            if (!usernameSent && !passwordSent)
                throw new BadRequestException("Invalid action. Use 'username', 'password', or 'both'.");

            return Ok(new
            {
                message = "Requested account details have been sent to your email."
            });
        }

        [HttpPost("reset_password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw new BadRequestException("Invalid request. Ensure passwords match.");
            }



            await _authService.PasswordResetAsync(request.Token, request.Password);
            return Ok(new { message = "Password reset successful." });
        }


    }
}
