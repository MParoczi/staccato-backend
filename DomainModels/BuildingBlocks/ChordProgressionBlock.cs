using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class ChordProgressionBlock : BuildingBlock
{
    public ChordProgressionBlock() : base(BuildingBlockType.ChordProgression)
    {
    }

    public string TimeSignature { get; set; } = string.Empty;
    public List<ChordProgressionSection> Sections { get; set; } = new();
}