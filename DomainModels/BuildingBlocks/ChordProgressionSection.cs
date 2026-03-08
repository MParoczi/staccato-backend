namespace DomainModels.BuildingBlocks;

public class ChordProgressionSection
{
    public string Label { get; set; } = string.Empty;
    public int Repeat { get; set; }
    public List<ChordMeasure> Measures { get; set; } = new();
}