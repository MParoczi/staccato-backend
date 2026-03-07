using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class CheckboxListBlock : BuildingBlock
{
    public List<CheckboxListItem> Items { get; set; } = new();

    public CheckboxListBlock() : base(BuildingBlockType.CheckboxList) { }
}
