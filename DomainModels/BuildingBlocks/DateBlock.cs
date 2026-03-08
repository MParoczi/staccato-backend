using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class DateBlock : BuildingBlock
{
    public DateBlock() : base(BuildingBlockType.Date)
    {
    }

    public List<TextSpan> Spans { get; set; } = new();
}