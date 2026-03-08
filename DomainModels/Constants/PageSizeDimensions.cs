using DomainModels.Enums;

namespace DomainModels.Constants;

public static class PageSizeDimensions
{
    public static readonly IReadOnlyDictionary<PageSize, (int WidthMm, int HeightMm, int GridWidth, int GridHeight)> Dimensions =
        new Dictionary<PageSize, (int WidthMm, int HeightMm, int GridWidth, int GridHeight)>
        {
            { PageSize.A4, (210, 297, 42, 59) },
            { PageSize.A5, (148, 210, 29, 42) },
            { PageSize.A6, (105, 148, 21, 29) },
            { PageSize.B5, (176, 250, 35, 50) },
            { PageSize.B6, (125, 176, 25, 35) }
        };
}