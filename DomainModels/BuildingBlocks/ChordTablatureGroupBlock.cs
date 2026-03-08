using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class ChordTablatureGroupBlock : BuildingBlock
{
    public ChordTablatureGroupBlock() : base(BuildingBlockType.ChordTablatureGroup)
    {
    }

    public List<ChordTablatureItem> Items { get; set; } = new();
}