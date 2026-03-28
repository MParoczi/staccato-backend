namespace DomainModels.Models;

public sealed record GoogleUserInfo(
    string GoogleId,
    string Email,
    string? Name,
    string? PictureUrl);