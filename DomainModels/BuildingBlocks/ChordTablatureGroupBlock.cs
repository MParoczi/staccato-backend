using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class ChordTablatureGroupBlock : BuildingBlock
{
    public List<ChordTablatureItem> Items { get; set; } = new();

    public ChordTablatureGroupBlock() : base(BuildingBlockType.ChordTablatureGroup) { }
}
