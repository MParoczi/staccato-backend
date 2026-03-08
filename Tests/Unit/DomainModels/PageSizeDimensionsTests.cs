using DomainModels.Constants;
using DomainModels.Enums;

namespace Tests.Unit.DomainModels;

public class PageSizeDimensionsTests
{
    [Fact]
    public void Dimensions_ContainsAllFivePageSizeValues()
    {
        var allSizes = Enum.GetValues<PageSize>();
        foreach (var size in allSizes)
            Assert.True(PageSizeDimensions.Dimensions.ContainsKey(size), $"Missing entry for PageSize.{size}");
    }

    [Theory]
    [InlineData(PageSize.A4, 210, 297, 42, 59)]
    [InlineData(PageSize.A5, 148, 210, 29, 42)]
    [InlineData(PageSize.A6, 105, 148, 21, 29)]
    [InlineData(PageSize.B5, 176, 250, 35, 50)]
    [InlineData(PageSize.B6, 125, 176, 25, 35)]
    public void Dimensions_ReturnsCorrectValues(PageSize size, int widthMm, int heightMm, int gridWidth, int gridHeight)
    {
        var dims = PageSizeDimensions.Dimensions[size];
        Assert.Equal(widthMm, dims.WidthMm);
        Assert.Equal(heightMm, dims.HeightMm);
        Assert.Equal(gridWidth, dims.GridWidth);
        Assert.Equal(gridHeight, dims.GridHeight);
    }

    [Theory]
    [InlineData(210, 297, 42, 59)]
    [InlineData(148, 210, 29, 42)]
    [InlineData(105, 148, 21, 29)]
    [InlineData(176, 250, 35, 50)]
    [InlineData(125, 176, 25, 35)]
    public void GridDimensions_SatisfyFloorDivisionRelationship(int widthMm, int heightMm, int gridWidth, int gridHeight)
    {
        Assert.Equal(gridWidth, widthMm / 5);
        Assert.Equal(gridHeight, heightMm / 5);
    }
}