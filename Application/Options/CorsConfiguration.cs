using System.ComponentModel.DataAnnotations;

namespace Application.Options;

/// <summary>
///     CORS allowed origins loaded from appsettings.json → "Cors" section.
///     Named <c>CorsConfiguration</c> (not <c>CorsOptions</c>) to avoid an ambiguous-reference
///     conflict with <c>Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions</c> in startup code.
/// </summary>
/// <remarks>
///     Empty array → CORS rejects all cross-origin requests (no startup failure).<br />
///     Null → startup fails ([Required] validation).<br />
///     Values must be specific origin strings — wildcards are incompatible with AllowCredentials().
/// </remarks>
public sealed class CorsConfiguration
{
    [Required]
    public string[] AllowedOrigins { get; init; } = [];
}