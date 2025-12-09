using EVWebApi.Data;
using EVWebApi.DTOs;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Services;
using EVWebAPI.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly IMfaService _mfaService;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditLogService _auditlogservice;

    public AuthService(IUserRepository userRepo, IConfiguration config, IMfaService mfaService, ILogger<AuthService> logger, IAuditLogService auditlogservice) {
        _userRepo = userRepo;
        _config = config;
        _mfaService = mfaService;
        _logger = logger;
        _auditlogservice = auditlogservice;
    }

    public async Task<AuthResult> AuthenticateAsync(string? username,string? email, string password) 
    {
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Either username or email must be provided");
        
        var user = email is not null
        ? await _userRepo.GetByEmailAsync(email)
        : await _userRepo.GetByUsernameAsync(username!);

        if (user == null)
            throw new NotFoundException("User not found");

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
            throw new AuthenticationException("Invalid email or password");

        if (user.MfaEnabled)
        {
            //return "MFA_REQUIRED";

            return new AuthResult
            {
                MfaRequired = true,
                UserId = user.UserId,
                UserName = user.Username
            };
        }
        //return GenerateJwtToken(user);
        //normal login
        return new AuthResult
        {
            MfaRequired = false,
            Token = GenerateJwtToken(user),
            UserId = user.UserId,
            UserName = user.Username
        };

    }

    public async Task<string> GenerateJwtAfterMfaAsync(string email) {
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null)
            throw new NotFoundException("User not found");
        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user) {


        if (user == null)
            throw new BadRequestException("User object is null");

        var key = _config["Jwt:Key"];
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("JWT Key is missing in configuration.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new [] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
            new Claim("userId", user.UserId.ToString()),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
       
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GeneratePasswordResetJwtAsync(User user)
    {
        if (user == null)
            throw new BadRequestException("User object is null");


        var key = _config["Jwt:Key"];
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("JWT Key is missing in configuration.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


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
    public async Task PasswordResetAsync(string token, string newPassword)
    {
       
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
        {
            throw new BadRequestException("Token and new password are required.");
        }
        // repeting same block to add security as password reset endpoint is open to the public(can't do authroization)

        var jwtSecret = _config.GetValue<string>("Jwt:Key");
        var tokenHandler = new JwtSecurityTokenHandler();

        
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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
        {
            throw new NotFoundException("User not found.");
        }

        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();


        await _auditlogservice.LogAsync(user.UserId, user.Username, "Login", "Reset Password");

    }

}
