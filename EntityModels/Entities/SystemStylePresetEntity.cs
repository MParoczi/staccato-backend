namespace EntityModels.Entities;

public class SystemStylePresetEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public string StylesJson { get; set; } = string.Empty;
}
