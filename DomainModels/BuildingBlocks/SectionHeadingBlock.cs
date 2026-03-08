using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class SectionHeadingBlock : BuildingBlock
{
    public SectionHeadingBlock() : base(BuildingBlockType.SectionHeading)
    {
    }

    public List<TextSpan> Spans { get; set; } = new();
}