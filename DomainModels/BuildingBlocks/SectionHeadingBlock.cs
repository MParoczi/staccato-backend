using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class SectionHeadingBlock : BuildingBlock
{
    public List<TextSpan> Spans { get; set; } = new();

    public SectionHeadingBlock() : base(BuildingBlockType.SectionHeading) { }
}
