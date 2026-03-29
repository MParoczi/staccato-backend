using DomainModels.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Pdf;

public static class LessonPageRenderer
{
    public static void Compose(
        IDocumentContainer container,
        PageRenderData page,
        PdfExportData data,
        float widthMm,
        float heightMm,
        int globalPageNumber)
    {
        container.Page(p =>
        {
            p.Size(widthMm, heightMm, Unit.Millimetre);
            p.Margin(0);

            p.Background().Container()
                .DrawDottedBackground(widthMm, heightMm);

            p.Content().Container().Layers(layers =>
            {
                layers.Layer().Container(); // base layer

                // Render modules sorted by ZIndex (lower drawn first)
                var sortedModules = page.Modules.OrderBy(m => m.ZIndex).ToList();

                foreach (var module in sortedModules)
                {
                    var moduleType = module.ModuleType;
                    data.Styles.TryGetValue(moduleType, out var style);
                    style ??= DefaultStyle;

                    layers.Layer().Container()
                        .Width(widthMm, Unit.Millimetre)
                        .Height(heightMm, Unit.Millimetre)
                        .Container()
                        .Canvas((_, _) => { }) // placeholder container for absolute positioning
                        ;
                    // Use direct absolute positioning via ModuleRenderer
                    layers.Layer()
                        .Width(widthMm, Unit.Millimetre)
                        .Height(heightMm, Unit.Millimetre)
                        .Container()
                        .Column(col =>
                        {
                            col.Item().Container()
                                .ModuleAt(module, style, data.Chords);
                        });
                }
            });

            p.Footer().AlignRight().Padding(10, Unit.Millimetre)
                .Text(text =>
                {
                    text.Span(globalPageNumber.ToString());
                });
        });
    }

    private static readonly ModuleStyleData DefaultStyle = new(
        BackgroundColor: "#FFFFFF",
        BorderColor: "#000000",
        BorderWidth: 1,
        BorderRadius: 0,
        HeaderBgColor: "#EEEEEE",
        HeaderTextColor: "#000000",
        BodyTextColor: "#000000",
        FontFamily: "Arial");

    private static IContainer ModuleAt(
        this IContainer container,
        ModuleRenderData module,
        ModuleStyleData style,
        IReadOnlyDictionary<Guid, Chord> chords)
    {
        ModuleRenderer.Render(container, module, style, chords);
        return container;
    }
}
