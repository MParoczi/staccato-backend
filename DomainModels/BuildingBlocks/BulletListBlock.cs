using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class BulletListBlock : BuildingBlock
{
    public List<List<TextSpan>> Items { get; set; } = new();

    public BulletListBlock() : base(BuildingBlockType.BulletList) { }
}
