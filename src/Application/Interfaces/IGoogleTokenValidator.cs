namespace Application.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct);
}

public record GoogleUserInfo(string Subject, string Email, string Name);
