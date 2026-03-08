using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class TextBlock : BuildingBlock
{
    public TextBlock() : base(BuildingBlockType.Text)
    {
    }

    public List<TextSpan> Spans { get; set; } = new();
}