using EVWebApi.Data;
using EVWebApi.DTOs;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;
using QRCoder;

public class MfaService : IMfaService
{
    private readonly IMfaRepository _mfaRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserAuthenticatorRepository _authenticatorRepo;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<MfaSettings> _mfaOptions;
    private readonly ILogger<MfaService> _logger;


    public MfaService(IMfaRepository mfaRepo, IUserRepository userRepo, IUserAuthenticatorRepository authenticatorRepo,
        IEmailSender emailSender,
        IOptions<MfaSettings> mfaOptions,
        ILogger<MfaService> logger)
    {
        _mfaRepo = mfaRepo;
        _userRepo = userRepo;
        _authenticatorRepo = authenticatorRepo;
        _emailSender = emailSender;
        _mfaOptions = mfaOptions;
        _logger = logger;

    }

    public async Task GenerateAndSendTokenAsync(User user, CancellationToken ct = default) {
        var token = new Random().Next(100000, 999999).ToString();
        var mfaToken = new UserMfaToken
        {

            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Used = false
        };
        await _mfaRepo.SaveMfaTokenAsync(mfaToken);
        // TODO: Send token via Email/SMS

        // Build email body from template
        var minutes = 5;
        var subject = _mfaOptions.Value.EmailSubject;
        var body = _mfaOptions.Value.EmailBodyTemplate
            .Replace("{CODE}", token)
            .Replace("{MINUTES}", minutes.ToString());

        await _emailSender.SendAsync(user.Email, subject, body, null, ct);
        _logger.LogInformation("MFA email sent to {Email}", user.Email);

    }

    public async Task<bool> VerifyTokenAsync(string email, string token) {
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null) return false;
        var mfaToken = await _mfaRepo.GetValidTokenAsync(user.UserId, token);
        if (mfaToken == null) return false;
        mfaToken.Used = true;
        await _mfaRepo.SaveMfaTokenAsync(mfaToken);
        return true;
    }


    // New: Generate QR Code for Google Authenticator
    public async Task<string> GenerateQrCodeAsync(int userId, string email)
    {
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        await _authenticatorRepo.SaveAsync(new UserAuthenticator
        {
            UserId = userId,
            SecretKey = base32Secret,
            CreatedAt = DateTime.UtcNow,
            Enabled = false
        });

        string issuer = "MyApp";

        string otpauthUrl = $"otpauth://totp/{issuer}:{email}?secret={base32Secret}&amp;issuer={issuer}";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new Base64QRCode(qrCodeData);
        return qrCode.GetGraphic(20); // Base64 image string
    }

    // New: Verify TOTP Code
    public async Task<bool> VerifyTotpAsync(int userId, string code)
    {
        var authenticator = await _authenticatorRepo.GetByUserIdAsync(userId);
        if (authenticator == null) return false;

        var secretKey = Base32Encoding.ToBytes(authenticator.SecretKey);
        var totp = new Totp(secretKey);
        return totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(previous: 1, future: 1));
    }

    public Task<bool> VerifyEmailOtpAsync(string email, string token)
    {
        throw new NotImplementedException();
    }

    public async Task GenerateAndSendTokenAsync(User user)
    {
        var token = new Random().Next(100000, 999999).ToString();
        var mfaToken = new UserMfaToken
        {

            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Used = false
        };
        await _mfaRepo.SaveMfaTokenAsync(mfaToken);
        // TODO: Send token via Email/SMS

        // Build email body from template
        var minutes = 5;
        var subject = _mfaOptions.Value.EmailSubject;
        var body = _mfaOptions.Value.EmailBodyTemplate
            .Replace("{CODE}", token)
            .Replace("{MINUTES}", minutes.ToString());

        await _emailSender.SendAsync(user.Email, subject, body, null);
        _logger.LogInformation("MFA email sent to {Email}", user.Email);
    }
}
