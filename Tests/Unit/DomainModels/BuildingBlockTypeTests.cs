using DomainModels.BuildingBlocks;
using DomainModels.Enums;

namespace Tests.Unit.DomainModels;

public class BuildingBlockTypeTests
{
    // SC-004: All 10 concrete block types instantiate with correct Type discriminator

    [Fact]
    public void SectionHeadingBlock_Type_IsSectionHeading() =>
        Assert.Equal(BuildingBlockType.SectionHeading, new SectionHeadingBlock().Type);

    [Fact]
    public void DateBlock_Type_IsDate() =>
        Assert.Equal(BuildingBlockType.Date, new DateBlock().Type);

    [Fact]
    public void TextBlock_Type_IsText() =>
        Assert.Equal(BuildingBlockType.Text, new TextBlock().Type);

    [Fact]
    public void BulletListBlock_Type_IsBulletList() =>
        Assert.Equal(BuildingBlockType.BulletList, new BulletListBlock().Type);

    [Fact]
    public void NumberedListBlock_Type_IsNumberedList() =>
        Assert.Equal(BuildingBlockType.NumberedList, new NumberedListBlock().Type);

    [Fact]
    public void CheckboxListBlock_Type_IsCheckboxList() =>
        Assert.Equal(BuildingBlockType.CheckboxList, new CheckboxListBlock().Type);

    [Fact]
    public void TableBlock_Type_IsTable() =>
        Assert.Equal(BuildingBlockType.Table, new TableBlock().Type);

    [Fact]
    public void MusicalNotesBlock_Type_IsMusicalNotes() =>
        Assert.Equal(BuildingBlockType.MusicalNotes, new MusicalNotesBlock().Type);

    [Fact]
    public void ChordProgressionBlock_Type_IsChordProgression() =>
        Assert.Equal(BuildingBlockType.ChordProgression, new ChordProgressionBlock().Type);

    [Fact]
    public void ChordTablatureGroupBlock_Type_IsChordTablatureGroup() =>
        Assert.Equal(BuildingBlockType.ChordTablatureGroup, new ChordTablatureGroupBlock().Type);

    // SC-004: Case-sensitive string equality — discriminator name matches enum member name exactly
    [Fact]
    public void ChordProgressionBlock_TypeName_IsCaseSensitivelyChordProgression() =>
        Assert.Equal("ChordProgression", new ChordProgressionBlock().Type.ToString());
}
