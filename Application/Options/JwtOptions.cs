using System.ComponentModel.DataAnnotations;

namespace Application.Options;

public sealed class JwtOptions : IValidatableObject
{
    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string SecretKey { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AccessTokenExpiryMinutes { get; init; }

    [Range(1, int.MaxValue)]
    public int RefreshTokenExpiryDays { get; init; }

    [Range(1, int.MaxValue)]
    public int RememberMeExpiryDays { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RememberMeExpiryDays < RefreshTokenExpiryDays)
            yield return new ValidationResult(
                $"RememberMeExpiryDays ({RememberMeExpiryDays}) must be >= RefreshTokenExpiryDays ({RefreshTokenExpiryDays}).",
                [nameof(RememberMeExpiryDays)]);
    }
}