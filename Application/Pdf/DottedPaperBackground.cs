using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Application.Pdf;

public static class DottedPaperBackground
{
    private const float DotSpacingMm = 5f;
    private const float DotRadiusMm = 0.25f; // 0.5mm diameter
    private static readonly SKColor DotColor = SKColor.Parse("#CCCCCC");

    public static void DrawDottedBackground(this IContainer container, float widthMm, float heightMm)
    {
        container.Canvas((canvasObj, size) =>
        {
            var canvas = (SKCanvas)canvasObj;
            using var paint = new SKPaint
            {
                Color = DotColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var mmToPoints = size.Width / widthMm;
            var dotRadius = DotRadiusMm * mmToPoints;
            var spacingPx = DotSpacingMm * mmToPoints;

            for (var y = spacingPx; y < size.Height; y += spacingPx)
            for (var x = spacingPx; x < size.Width; x += spacingPx)
                canvas.DrawCircle(x, y, dotRadius, paint);
        });
    }
}
