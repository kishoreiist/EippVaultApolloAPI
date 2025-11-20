using EVWebApi.Data;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;
public class MfaService:IMfaService
{
    private readonly IMfaRepository _mfaRepo;
    private readonly IUserRepository _userRepo;
    public MfaService(IMfaRepository mfaRepo, IUserRepository userRepo)
    {
        _mfaRepo = mfaRepo;
        _userRepo = userRepo;
    }


    public async Task GenerateAndSendTokenAsync(User user) {
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
}
