using System.ComponentModel.DataAnnotations;

namespace Application.Options;

public sealed class RateLimitOptions
{
    [Required]
    [Range(1, int.MaxValue)]
    public int AuthWindowSeconds { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public int AuthMaxRequests { get; init; }
}