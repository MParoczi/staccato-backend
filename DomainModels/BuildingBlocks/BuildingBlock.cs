using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public abstract class BuildingBlock
{
    public BuildingBlockType Type { get; }

    protected BuildingBlock(BuildingBlockType type)
    {
        Type = type;
    }
}
