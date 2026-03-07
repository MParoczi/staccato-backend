using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class TextBlock : BuildingBlock
{
    public List<TextSpan> Spans { get; set; } = new();

    public TextBlock() : base(BuildingBlockType.Text) { }
}
