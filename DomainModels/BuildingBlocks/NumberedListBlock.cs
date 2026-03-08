using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class NumberedListBlock : BuildingBlock
{
    public NumberedListBlock() : base(BuildingBlockType.NumberedList)
    {
    }

    public List<List<TextSpan>> Items { get; set; } = new();
}