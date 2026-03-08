using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class CheckboxListBlock : BuildingBlock
{
    public CheckboxListBlock() : base(BuildingBlockType.CheckboxList)
    {
    }

    public List<CheckboxListItem> Items { get; set; } = new();
}