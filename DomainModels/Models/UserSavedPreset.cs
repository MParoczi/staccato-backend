namespace DomainModels.Models;

public class UserSavedPreset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StylesJson { get; set; } = string.Empty;
}
