using DomainModels.Enums;

namespace DomainModels.BuildingBlocks;

public class MusicalNotesBlock : BuildingBlock
{
    public MusicalNotesBlock() : base(BuildingBlockType.MusicalNotes)
    {
    }

    public List<string> Notes { get; set; } = new();
}