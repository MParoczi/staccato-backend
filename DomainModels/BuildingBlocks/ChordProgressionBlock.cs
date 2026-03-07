using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class ChordProgressionBlock : BuildingBlock
{
    public string TimeSignature { get; set; } = string.Empty;
    public List<ChordProgressionSection> Sections { get; set; } = new();

    public ChordProgressionBlock() : base(BuildingBlockType.ChordProgression) { }
}
