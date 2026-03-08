namespace EntityModels.Entities;

public class UserSavedPresetEntity : IEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StylesJson { get; set; } = string.Empty;

    public UserEntity User { get; set; } = null!;
    public Guid Id { get; set; }
}