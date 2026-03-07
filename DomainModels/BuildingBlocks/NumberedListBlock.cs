using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class NumberedListBlock : BuildingBlock
{
    public List<List<TextSpan>> Items { get; set; } = new();

    public NumberedListBlock() : base(BuildingBlockType.NumberedList) { }
}
