using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class MusicalNotesBlock : BuildingBlock
{
    public List<string> Notes { get; set; } = new();

    public MusicalNotesBlock() : base(BuildingBlockType.MusicalNotes) { }
}
