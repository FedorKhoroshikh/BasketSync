using Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class GoogleTokenValidator(IConfiguration config) : IGoogleTokenValidator
{
    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [config["Google:ClientId"]!]
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

        return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name);
    }
}
