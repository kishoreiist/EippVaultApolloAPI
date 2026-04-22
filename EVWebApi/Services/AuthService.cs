using DocumentFormat.OpenXml.Spreadsheet;
using EVWebApi.Data;
using EVWebApi.DTOs;
using EVWebApi.Enums;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Models.Security;
using EVWebApi.Services;
using EVWebAPI.Controllers;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static SkiaSharp.HarfBuzz.SKShaper;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly IMfaService _mfaService;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditLogService _auditlogservice;
    private readonly IEmailSender _emailSender;
    private readonly string _frontendRoot;
    private readonly string _displayName;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecurityFailureService _securityserv;
    private readonly ISessionService _sessionService;
    private readonly IUserSessionRepository _sessionRepo;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    public AuthService(IUserRepository userRepo, IConfiguration config, IMfaService mfaService, ILogger<AuthService> logger,
        IAuditLogService auditlogservice, IHttpContextAccessor httpContextAccessor, IEmailSender emailSender, 
        ISecurityFailureService securityserv, AppDbContext context, ISessionService sessionService, IUserSessionRepository sessionRepo, IMemoryCache cache)
    {
        _userRepo = userRepo;
        _config = config;
        _mfaService = mfaService;
        _logger = logger;
        _auditlogservice = auditlogservice;
        _httpContextAccessor = httpContextAccessor;

        _emailSender = emailSender;
        _frontendRoot = config["Frontend:BaseUrl"];
        _displayName = config["Email:DisplayName"];
        _securityserv = securityserv;
        _context = context;
        _sessionService = sessionService;
        _sessionRepo = sessionRepo;
        _cache = cache;
        // _notificationService = notificationService;
    }
    public async Task<AuthResult> AuthenticateAsync(LoginRequestDTO dto)
    {
        string userInput = dto.User.Trim();
        string? username = null;
        string? email = null;
        var normalizedEmail = EmailValidationHelper.Normalize(userInput);
        if (EmailValidationHelper.IsValidEmail(normalizedEmail))
            email = normalizedEmail;
        else
            username = userInput;

        return await AuthenticateAsync(username, email, dto.Password);
    }

    public async Task<AuthResult> AuthenticateAsync(string? username, string? email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Either username or email must be provided");

        var user = email is not null
        ? await _userRepo.GetByEmailAsync(email)
        : await _userRepo.GetByUsernameAsync(username!);

        if (user == null)
        {
            await Task.Delay(Random.Shared.Next(400, 900));
            throw new NotFoundException("Invalid credentials");//giving generic error message
        }

        _httpContextAccessor.HttpContext!.Items["UserId"] = user.UserId;

        if (user.Status == UserStatus.New)//to resrtict login for NEW user before passwrd reset
            throw new AccountNotActivatedException("Account not activated");

        if (user.Status == UserStatus.Disabled || user.Status == UserStatus.Deleted)//to resrtict login 
            throw new AccountDeletedException("Non-Existing/Deleted/Disabled Account");



        if (user.Status == UserStatus.Locked)//to resrtict login for users locked by admin(inactivated by user)
        {
            bool isLocked = await _securityserv.IsUserLockedAsync(user.UserId);
            if (!isLocked)
            {
                // Transition from Locked -> New as lock expied, to allow password reset and reactivation by user
                user.Status = UserStatus.New;
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();

                //  trigger the email 
                await PasswordResetSendEmailAsync(user, PasswordEmailType.AccountLocked);

                throw new LockedException("Your temporary lock has expired. A reactivation link has been sent to your email.");
            }
            throw new AccountDisabledException("Account is locked by admin");
        }


        bool validPassword;

        try
        {
            validPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password hash verification failed for {Email}", email);
            throw new AuthenticationException("Password verification failed");
        }
        if (!validPassword)
        {
            await Task.Delay(Random.Shared.Next(400, 900));
            throw new AuthenticationException("Invalid email or password");
        }

        if (user.PasswordChangedAt.HasValue && user.PasswordChangedAt.Value.AddDays(90) < DateTime.UtcNow)
        {
            //to allow login only with password
            user.EmailVerified = true;
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
            await PasswordResetSendEmailAsync(user, PasswordEmailType.PasswordExpired);

            throw new PasswordExpiredException($"Password expired. Reset link sent to email- {user.Email}");
        }

        user.EmailVerified = true;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
        if (user.MfaEnabled)
        {
           
            return new AuthResult
            {
                MfaRequired = true,
                UserId = user.UserId,
                UserName = user.Username,
                Email=user.Email,
                EmailVerified = user.EmailVerified,
            };
        }

        //normal login for first time user
        return new AuthResult
        {
            MfaRequired = false,//as not activated
            //Token = GenerateJwtToken(user),
            UserId = user.UserId,
            UserName = user.Username,
            EmailVerified = user.EmailVerified,
            Email = user.Email,
        };

    }
    //-------------------------JWT TOKEN GENERATION-----------------

    //public string GenerateRefreshToken()
    //{
    //    var randomBytes = new byte[64];
    //    using var rng = RandomNumberGenerator.Create();
    //    rng.GetBytes(randomBytes);
    //    return Convert.ToBase64String(randomBytes);
    //}

    //public string HashToken(string token)
    //{
    //    using var sha = SHA256.Create();
    //    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
    //    return Convert.ToBase64String(bytes);
    //}

    public async Task<VerifyMfaResponseDto> GenerateJwtAfterMfaAsync(string email) {
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null)
            throw new NotFoundException("Invalid Credentials");

        var userType = await _userRepo.GetUserType(user.UserId);

        //creting access & refresh token and session
        var refreshToken = RefreshTokenHelper.GenerateRefreshToken();
        var refreshTokenHash = RefreshTokenHelper.HashToken(refreshToken);

        var session = await _sessionService.CreateLoginSessionAsync(user, refreshTokenHash);
        var accessToken = GenerateJwtToken(user, userType, session);
        return new VerifyMfaResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            SessionId = session.SessionId,
            ExpiresAt=session.ExpiresAt
        };
    }

    private string GenerateJwtToken(User user, string userType,UserSession session) {


        if (user == null)
            throw new BadRequestException("User object is null");

        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];


        // Using RSA asymmetric keys for signing JWTs---RsaSha256
        var rsa = RSA.Create();
        var privateKeyPath = _config["Jwt:PrivateKeyPath"];
        rsa.ImportFromPem(File.ReadAllText(privateKeyPath));

        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var claims = new [] {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("userId", user.UserId.ToString()),
            new Claim("username", user.Username),
            new Claim(ClaimTypes.Role, userType),
            new Claim("session_id", session.SessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, session.JwtId.ToString()),
        };

        //making access token expiry never exceeds session expiry
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);

        if (accessExpiry > session.ExpiresAt)
        {
            accessExpiry = session.ExpiresAt;
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: accessExpiry,
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshResultDTO> RefreshAsync(string refreshToken)
    {
        var hashedToken = RefreshTokenHelper.HashToken(refreshToken);

        var session = await _sessionRepo.GetByRefreshTokenHashAsync(hashedToken);

        if (session == null || session.ExpiresAt < DateTime.UtcNow )
            return new RefreshResultDTO { Success = false };

        if (session.IsRevoked)
            return new RefreshResultDTO { Success = false };

        // Idle timeout check (20 minutes)
        //if (session.LastActivityAt.AddMinutes(20) < DateTime.UtcNow)
        //{
        //    //session.IsRevoked = true;
        //    session.RevokedAt = DateTime.UtcNow;
        //    await _sessionRepo.UpdateAsync(session);

        //    return new RefreshResultDTO { Success = false };
        //}

        var user = await _userRepo.GetByIdAsync(session.UserId);
        if (user == null)
            return new RefreshResultDTO { Success = false };

        var userType = await _userRepo.GetUserType(user.UserId);

        // ROTATE refresh token
        var newRefreshToken = RefreshTokenHelper.GenerateRefreshToken();
        var newRefreshHash = RefreshTokenHelper.HashToken(newRefreshToken);
        _cache.Set(newRefreshHash, user.UserId, TimeSpan.FromMinutes(5));
        session.RefreshTokenHash = newRefreshHash;
        session.JwtId = Guid.NewGuid();
        session.LastActivityAt = DateTime.UtcNow;

        await _sessionRepo.UpdateAsync(session);

        var newAccessToken = GenerateJwtToken(user, userType, session);

        return new RefreshResultDTO
        {
            Success = true,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt=session.ExpiresAt
        };
    }

    //-------------------------------------------------------//
    public string GeneratePasswordResetJwtAsync(User user)
    {
        if (user == null)
            throw new BadRequestException("User object is null");


        //var key = _config["Jwt:Key"];
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var privateKeyPath = _config["Jwt:PrivateKeyPath"];


        //if (string.IsNullOrEmpty(key))
        //    throw new InvalidOperationException("JWT Key is missing in configuration.");

        //var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        //var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(privateKeyPath));

        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var claims = new[] {
        new Claim("userId", user.UserId.ToString()), 
        new Claim("purpose", "password_reset"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())};

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public async Task PasswordResetAsync(string token, string password)
    {

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(password))
        {
            throw new BadRequestException("Token and new password are required.");
        }
        // repeting same block to add security as password reset endpoint is open to the public(can't do authroization)

        //var jwtSecret = _config.GetValue<string>("Jwt:Key");
        var tokenHandler = new JwtSecurityTokenHandler();

        var publicKeyPath = _config["Jwt:PublicKeyPath"];

        // Load PUBLIC key for validation
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(publicKeyPath));


        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = true,
            ValidIssuer = _config.GetValue<string>("Jwt:Issuer"),
            ValidateAudience = true,
            ValidAudience = _config.GetValue<string>("Jwt:Audience"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        ClaimsPrincipal principal;
        int userId;

        try
        {

            principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            var purposeClaim = principal.FindFirst("purpose");
            var userIdClaim = principal.FindFirst("userId");

            if (purposeClaim == null || purposeClaim.Value != "password_reset")
            {
                throw new AuthorizationException("Token is not valid for password reset.");
            }

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
            {
                throw new AuthorizationException("Token missing user identity.");
            }
        }
        catch (SecurityTokenExpiredException)
        {
            throw new AuthorizationException("Password reset link has expired.");
        }
        catch (Exception)
        {
            throw new AuthorizationException("Invalid password reset token.");
        }


        var user = await _userRepo.GetByIdAsync(userId);

        if (user == null)
            throw new NotFoundException("Invalid Creentials");

        if (user.Status != UserStatus.Active && user.Status != UserStatus.New)
            throw new AuthorizationException("Unauthorized action");

        var validatedPassword = PasswordValidationHelper.Validate(password, user.Username, user.FirstName, user.LastName, user.Email);
        if (!validatedPassword.IsValid)
            throw new BadRequestException(validatedPassword.Error);
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Prevent reuse (last 5)
            var lastPasswords = await _userRepo.GetLast5PasswordsAsync(user.UserId);

            foreach (var old in lastPasswords)
            {
                if (BCrypt.Net.BCrypt.Verify(password, old.PasswordHash))
                    throw new PasswordReuseException("You cannot reuse your last 5 passwords.");
            }

            //Save current password to history
            await _userRepo.AddPasswordHistoryAsync(new UserPasswordHistory
            {
                UserId = user.UserId,
                PasswordHash = user.PasswordHash,
                CreatedAt = DateTime.UtcNow
            });

            //Update new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            user.Status = UserStatus.Active;
            user.PasswordChangedAt = DateTime.UtcNow;

            _userRepo.Update(user);

            // Keep only last 5
            await _userRepo.DeleteOlderPasswordsAsync(user.UserId, 5);

            await _userRepo.SaveChangesAsync();

            await transaction.CommitAsync();

            await _auditlogservice.LogAsync(
                user.UserId,
                user.Username,
                "Login",
                "Reset Password");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        //if (user.Status == UserStatus.Active || user.Status==UserStatus.New)
        //{
        //    var validatedPassword = PasswordValidationHelper.Validate(password, user.Username, user.FirstName, user.LastName, user.Email);

        //    if (!validatedPassword.IsValid)
        //    {
        //        throw new BadRequestException(validatedPassword.Error);
        //    }
        //    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        //    user.Status = UserStatus.Active; // Ensure account is active after password reset

        //    _userRepo.Update(user);
        //    await _userRepo.SaveChangesAsync();


        //    await _auditlogservice.LogAsync(user.UserId, user.Username, "Login", "Reset Password");
        //}
        //else
        //{
        //     throw new AuthorizationException("Unauthorized action");
        //}

    }
    public async Task<bool> PasswordResetSendEmailAsync(User user, PasswordEmailType type)
    {
        var token = GeneratePasswordResetJwtAsync(user);

        var resetUrl = $"{_frontendRoot}reset_password?email={user.Email}&token={Uri.EscapeDataString(token)}";
        string htmlbody;
        string subject;

        switch (type)
        {
            case PasswordEmailType.PasswordExpired:
                subject = $"{_displayName} - Password Expired";
                htmlbody = $"""
                <p>Dear User,</p>
                <p>Your password has expired (90 days policy).</p>
                <p>Please reset your password using the button below.</p>
                {BuildResetButton(resetUrl)}
                <p>This link expires in 30 minutes.</p>
                <br/><br/>Regards,<br/>{_displayName} Team
                """;
                break;

            case PasswordEmailType.AccountLocked:
                subject = $"{_displayName} - Security Alert: Account Locked";
                htmlbody = $"""
                <p>Dear User,</p>
                <p>Multiple failed login attempts were detected.</p>
                <p>Your account has been temporarily locked.</p>
                <p><strong>To reactivate your account, please reset your password using the below button.</strong></p>
                {BuildResetButton(resetUrl)}
                <p>This link expires in 30 minutes.</p>
                <br/><br/>Regards,<br/>{_displayName} Team
                """;
                break;
            default:
                subject = $"{_displayName} - Reset Password";
                htmlbody = $"""
                <p>Dear User,</p>
                <p>Please reset your password.</p>
                {BuildResetButton(resetUrl)}
                <p>This link expires in 30 minutes.</p>
                <br/><br/>Regards,<br/>{_displayName} Team
                """;
                break;
        }
        var sent = await _emailSender.SendAsync(
             ReplyTo: null,
             UserName: null,
           toEmail: user.Email,
            subject: subject,
           htmlBody: htmlbody
        );

        if (sent)
        {
            return true;
        }
        else
        { return false; }

    }
    private string BuildResetButton(string url)
    {
        return $"""
        <table width='100%' cellpadding='0' cellspacing='0' style='margin:32px 0;'>
          <tr>
            <td align='left'>
              <a href='{url}' target='_blank'
                 style='background:#2563eb;color:#ffffff;
                        text-decoration:none;padding:14px 32px;
                        border-radius:6px;font-size:16px;
                        display:inline-block;'>
                Reset Password
              </a>
            </td>
          </tr>
        </table>
        """;
    }

}
