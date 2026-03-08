using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class BulletListBlock : BuildingBlock
{
    public BulletListBlock() : base(BuildingBlockType.BulletList)
    {
    }

    public List<List<TextSpan>> Items { get; set; } = new();
}