using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public abstract class BuildingBlock
{
    protected BuildingBlock(BuildingBlockType type)
    {
        Type = type;
    }

    public BuildingBlockType Type { get; }
}