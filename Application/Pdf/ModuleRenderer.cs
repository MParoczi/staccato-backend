using DomainModels.Enums;
using DomainModels.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Pdf;

public static class ModuleRenderer
{
    private const float GridUnitMm = 5f;

    public static void Render(
        IContainer container,
        ModuleRenderData module,
        ModuleStyleData style,
        IReadOnlyDictionary<Guid, Chord> chords)
    {
        var xMm = module.GridX * GridUnitMm;
        var yMm = module.GridY * GridUnitMm;
        var wMm = module.GridWidth * GridUnitMm;
        var hMm = module.GridHeight * GridUnitMm;

        container
            .PaddingLeft(xMm, Unit.Millimetre)
            .PaddingTop(yMm, Unit.Millimetre)
            .Width(wMm, Unit.Millimetre)
            .Height(hMm, Unit.Millimetre)
            .Border(style.BorderWidth)
            .BorderColor(style.BorderColor)
            .Background(style.BackgroundColor)
            .Column(column =>
            {
                // Module header
                column.Item()
                    .Background(style.HeaderBgColor)
                    .Padding(3, Unit.Millimetre)
                    .Text(module.ModuleType.ToString())
                    .FontSize(9)
                    .FontColor(style.HeaderTextColor)
                    .FontFamily(style.FontFamily)
                    .Bold();

                // Module body
                column.Item()
                    .Padding(3, Unit.Millimetre)
                    .Column(body =>
                    {
                        foreach (var block in module.BuildingBlocks)
                            BuildingBlockRenderers.Render(body, block, style, chords);
                    });
            });
    }
}
