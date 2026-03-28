using DomainModels.Models;

namespace Domain.Services;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct = default);
}