namespace DomainModels.BuildingBlocks;

public class ChordBeat
{
    public Guid ChordId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Beats { get; set; }
}
