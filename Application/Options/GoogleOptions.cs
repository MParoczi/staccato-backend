using System.ComponentModel.DataAnnotations;

namespace Application.Options;

public sealed class GoogleOptions
{
    [Required]
    public string ClientId { get; init; } = string.Empty;
}