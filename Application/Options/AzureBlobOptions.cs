using System.ComponentModel.DataAnnotations;

namespace Application.Options;

public sealed class AzureBlobOptions
{
    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string ContainerName { get; init; } = string.Empty;
}