using DomainModels.Constants;
using DomainModels.Enums;

namespace Tests.Unit.DomainModels;

public class ModuleTypeConstraintsTests
{
    [Fact]
    public void AllowedBlocks_ContainsAllTwelveModuleTypeValues()
    {
        var allTypes = Enum.GetValues<ModuleType>();
        foreach (var type in allTypes)
            Assert.True(ModuleTypeConstraints.AllowedBlocks.ContainsKey(type), $"Missing AllowedBlocks entry for ModuleType.{type}");
    }

    [Fact]
    public void MinimumSizes_ContainsAllTwelveModuleTypeValues()
    {
        var allTypes = Enum.GetValues<ModuleType>();
        foreach (var type in allTypes)
            Assert.True(ModuleTypeConstraints.MinimumSizes.ContainsKey(type), $"Missing MinimumSizes entry for ModuleType.{type}");
    }

    [Fact]
    public void AllowedBlocks_Breadcrumb_IsEmpty()
    {
        var blocks = ModuleTypeConstraints.AllowedBlocks[ModuleType.Breadcrumb];
        Assert.Empty(blocks);
    }

    [Fact]
    public void AllowedBlocks_ChordTablature_ContainsChordTablatureGroup()
    {
        var blocks = ModuleTypeConstraints.AllowedBlocks[ModuleType.ChordTablature];
        Assert.Contains(BuildingBlockType.ChordTablatureGroup, blocks);
    }

    [Fact]
    public void AllowedBlocks_FreeText_ContainsAllTenBuildingBlockTypes()
    {
        var blocks = ModuleTypeConstraints.AllowedBlocks[ModuleType.FreeText];
        Assert.Equal(10, blocks.Count);
        foreach (var type in Enum.GetValues<BuildingBlockType>())
            Assert.Contains(type, blocks);
    }

    [Theory]
    [InlineData(ModuleType.Title, 20, 4)]
    [InlineData(ModuleType.Breadcrumb, 20, 3)]
    [InlineData(ModuleType.Subtitle, 10, 3)]
    [InlineData(ModuleType.Theory, 8, 5)]
    [InlineData(ModuleType.Practice, 8, 5)]
    [InlineData(ModuleType.Example, 8, 5)]
    [InlineData(ModuleType.Important, 8, 4)]
    [InlineData(ModuleType.Tip, 8, 4)]
    [InlineData(ModuleType.Homework, 8, 5)]
    [InlineData(ModuleType.Question, 8, 4)]
    [InlineData(ModuleType.ChordTablature, 8, 10)]
    [InlineData(ModuleType.FreeText, 4, 4)]
    public void MinimumSizes_ReturnsCorrectValues(ModuleType type, int minWidth, int minHeight)
    {
        var sizes = ModuleTypeConstraints.MinimumSizes[type];
        Assert.Equal(minWidth, sizes.MinWidth);
        Assert.Equal(minHeight, sizes.MinHeight);
    }
}