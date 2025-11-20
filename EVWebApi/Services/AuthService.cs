using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
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

    public AuthService(IUserRepository userRepo, IConfiguration config, IMfaService mfaService) {
        _userRepo = userRepo;
        _config = config;
        _mfaService = mfaService;
    }

    public async Task<string> AuthenticateAsync(string email, string password) {
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        if (user.MfaEnabled) {
            await _mfaService.GenerateAndSendTokenAsync(user);
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
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim("userId", user.UserId.ToString()),
            new Claim("role", user.Role.RoleName)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
