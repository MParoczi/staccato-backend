using Application.Options;
using Domain.Exceptions;
using Domain.Services;
using DomainModels.Constants;
using DomainModels.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Application.Services;

public sealed class GoogleTokenValidator(IOptions<GoogleOptions> options) : IGoogleTokenValidator
{
    private readonly GoogleOptions _googleOptions = options.Value;

    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleOptions.ClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedException(AuthErrorCodes.GoogleAuthFailed, "Google Sign-In failed.");
        }
        catch (Exception)
        {
            throw new ServiceUnavailableException();
        }
    }
}
