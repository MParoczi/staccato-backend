using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class DateBlock : BuildingBlock
{
    public List<TextSpan> Spans { get; set; } = new();

    public DateBlock() : base(BuildingBlockType.Date) { }
}
