namespace EntityModels.Entities;

public class SystemStylePresetEntity : IEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public string StylesJson { get; set; } = string.Empty;
    public Guid Id { get; set; }
}