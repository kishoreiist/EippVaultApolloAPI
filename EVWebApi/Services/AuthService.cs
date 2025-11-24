using EVWebApi.Data;
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
    private readonly ILogger<AuthController> _logger;

    public AuthService(IUserRepository userRepo, IConfiguration config, IMfaService mfaService) {
        _userRepo = userRepo;
        _config = config;
        _mfaService = mfaService;
    }

    public async Task<string> AuthenticateAsync(string email, string password) {
        var user = await _userRepo.GetByEmailAsync(email);
        if(user == null) return null;      

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password verification failed for {Email}", email);
            return null;
        }

        if (user.MfaEnabled)
        {
            //await _mfaService.GenerateAndSendTokenAsync(user);
            return "MFA_REQUIRED";
        }
        return GenerateJwtToken(user);
        
    }

    public async Task<string> GenerateJwtAfterMfaAsync(string email) {
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null) return null;
        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user) {


        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var key = _config["Jwt:Key"];
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("JWT Key is missing in configuration.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
            new Claim("userId", user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
}
