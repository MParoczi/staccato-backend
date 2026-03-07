using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class TableBlock : BuildingBlock
{
    public List<TableColumn> Columns { get; set; } = new();
    public List<List<List<TextSpan>>> Rows { get; set; } = new();

    public TableBlock() : base(BuildingBlockType.Table) { }
}
