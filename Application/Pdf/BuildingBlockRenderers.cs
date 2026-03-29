using DomainModels.BuildingBlocks;
using DomainModels.Enums;
using DomainModels.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Application.Pdf;

public static class BuildingBlockRenderers
{
    public static void Render(
        ColumnDescriptor column,
        BuildingBlock block,
        ModuleStyleData style,
        IReadOnlyDictionary<Guid, Chord> chords)
    {
        switch (block)
        {
            case SectionHeadingBlock b when b.Spans.Count > 0:
                RenderSectionHeading(column, b, style);
                break;
            case DateBlock b when b.Spans.Count > 0:
                RenderDate(column, b, style);
                break;
            case TextBlock b when b.Spans.Count > 0:
                RenderText(column, b, style);
                break;
            case BulletListBlock b when b.Items.Count > 0:
                RenderBulletList(column, b, style);
                break;
            case NumberedListBlock b when b.Items.Count > 0:
                RenderNumberedList(column, b, style);
                break;
            case CheckboxListBlock b when b.Items.Count > 0:
                RenderCheckboxList(column, b, style);
                break;
            case TableBlock b when b.Columns.Count > 0:
                RenderTable(column, b, style);
                break;
            case MusicalNotesBlock b when b.Notes.Count > 0:
                RenderMusicalNotes(column, b, style);
                break;
            case ChordProgressionBlock b when b.Sections.Count > 0:
                RenderChordProgression(column, b, style, chords);
                break;
            case ChordTablatureGroupBlock b when b.Items.Count > 0:
                RenderChordTablatureGroup(column, b, style, chords);
                break;
        }
    }

    // ── SectionHeading ──────────────────────────────────────────────────

    private static void RenderSectionHeading(
        ColumnDescriptor column, SectionHeadingBlock block, ModuleStyleData style)
    {
        column.Item().PaddingBottom(2).Text(text =>
        {
            text.DefaultTextStyle(s => s.FontSize(14).Bold().FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
            RenderSpans(text, block.Spans);
        });
    }

    // ── Date ────────────────────────────────────────────────────────────

    private static void RenderDate(
        ColumnDescriptor column, DateBlock block, ModuleStyleData style)
    {
        column.Item().PaddingBottom(2).Text(text =>
        {
            text.DefaultTextStyle(s => s.FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
            RenderSpans(text, block.Spans);
        });
    }

    // ── Text ────────────────────────────────────────────────────────────

    private static void RenderText(
        ColumnDescriptor column, TextBlock block, ModuleStyleData style)
    {
        column.Item().PaddingBottom(2).Text(text =>
        {
            text.DefaultTextStyle(s => s.FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
            RenderSpans(text, block.Spans);
        });
    }

    // ── BulletList ──────────────────────────────────────────────────────

    private static void RenderBulletList(
        ColumnDescriptor column, BulletListBlock block, ModuleStyleData style)
    {
        foreach (var item in block.Items)
        {
            if (item.Count == 0) continue;
            column.Item().PaddingBottom(1).Row(row =>
            {
                row.ConstantItem(10).Text("•")
                    .FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily);
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
                    RenderSpans(text, item);
                });
            });
        }
    }

    // ── NumberedList ────────────────────────────────────────────────────

    private static void RenderNumberedList(
        ColumnDescriptor column, NumberedListBlock block, ModuleStyleData style)
    {
        for (var i = 0; i < block.Items.Count; i++)
        {
            var item = block.Items[i];
            if (item.Count == 0) continue;
            var number = i + 1;
            column.Item().PaddingBottom(1).Row(row =>
            {
                row.ConstantItem(15).Text($"{number}.")
                    .FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily);
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
                    RenderSpans(text, item);
                });
            });
        }
    }

    // ── CheckboxList ────────────────────────────────────────────────────

    private static void RenderCheckboxList(
        ColumnDescriptor column, CheckboxListBlock block, ModuleStyleData style)
    {
        foreach (var item in block.Items)
        {
            if (item.Spans.Count == 0) continue;
            var marker = item.IsChecked ? "☑" : "☐";
            column.Item().PaddingBottom(1).Row(row =>
            {
                row.ConstantItem(12).Text(marker)
                    .FontSize(10).FontColor(style.BodyTextColor);
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(10).FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
                    RenderSpans(text, item.Spans);
                });
            });
        }
    }

    // ── Table ───────────────────────────────────────────────────────────

    private static void RenderTable(
        ColumnDescriptor column, TableBlock block, ModuleStyleData style)
    {
        column.Item().PaddingBottom(2).Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                for (var i = 0; i < block.Columns.Count; i++)
                    def.RelativeColumn();
            });

            // Header row
            foreach (var col in block.Columns)
            {
                table.Cell()
                    .Background("#F0F0F0")
                    .Border(0.5f)
                    .BorderColor("#CCCCCC")
                    .Padding(2, Unit.Millimetre)
                    .Text(text =>
                    {
                        text.DefaultTextStyle(s => s.FontSize(9).Bold().FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
                        RenderSpans(text, col.Header);
                    });
            }

            // Data rows
            foreach (var row in block.Rows)
            {
                foreach (var cell in row)
                {
                    table.Cell()
                        .Border(0.5f)
                        .BorderColor("#CCCCCC")
                        .Padding(2, Unit.Millimetre)
                        .Text(text =>
                        {
                            text.DefaultTextStyle(s => s.FontSize(9).FontColor(style.BodyTextColor).FontFamily(style.FontFamily));
                            RenderSpans(text, cell);
                        });
                }
            }
        });
    }

    // ── MusicalNotes ────────────────────────────────────────────────────

    private static void RenderMusicalNotes(
        ColumnDescriptor column, MusicalNotesBlock block, ModuleStyleData style)
    {
        column.Item().PaddingBottom(2).Row(row =>
        {
            foreach (var note in block.Notes)
            {
                row.AutoItem().Padding(1.5f, Unit.Millimetre)
                    .Width(24)
                    .Height(24)
                    .Background("#4A90D9")
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(note)
                    .FontSize(10)
                    .FontColor("#FFFFFF")
                    .Bold()
                    .FontFamily(style.FontFamily);
            }
        });
    }

    // ── ChordProgression ────────────────────────────────────────────────

    private static void RenderChordProgression(
        ColumnDescriptor column, ChordProgressionBlock block, ModuleStyleData style,
        IReadOnlyDictionary<Guid, Chord> chords)
    {
        foreach (var section in block.Sections)
        {
            if (!string.IsNullOrEmpty(section.Label))
            {
                column.Item().PaddingBottom(1).Row(row =>
                {
                    row.AutoItem().Text(section.Label)
                        .FontSize(9).Bold().FontColor(style.BodyTextColor).FontFamily(style.FontFamily);
                    if (section.Repeat > 1)
                        row.AutoItem().PaddingLeft(3).Text($"×{section.Repeat}")
                            .FontSize(8).FontColor("#888888").FontFamily(style.FontFamily);
                });
            }

            foreach (var measure in section.Measures)
            {
                column.Item().PaddingBottom(1).Row(row =>
                {
                    foreach (var beat in measure.Chords)
                    {
                        row.AutoItem().Padding(1, Unit.Millimetre)
                            .Background("#E8E8E8")
                            .Padding(2, Unit.Millimetre)
                            .Row(pill =>
                            {
                                pill.AutoItem().Text(beat.DisplayName)
                                    .FontSize(9).Bold().FontColor(style.BodyTextColor).FontFamily(style.FontFamily);
                                if (beat.Beats > 0)
                                    pill.AutoItem().PaddingLeft(2).Text($"({beat.Beats})")
                                        .FontSize(8).FontColor("#666666").FontFamily(style.FontFamily);
                            });
                    }
                });
            }
        }
    }

    // ── ChordTablatureGroup ─────────────────────────────────────────────

    private static void RenderChordTablatureGroup(
        ColumnDescriptor column, ChordTablatureGroupBlock block, ModuleStyleData style,
        IReadOnlyDictionary<Guid, Chord> chords)
    {
        column.Item().PaddingBottom(2).Row(row =>
        {
            foreach (var item in block.Items)
            {
                if (chords.TryGetValue(item.ChordId, out var chord) && chord.Positions.Count > 0)
                {
                    row.AutoItem().Padding(2, Unit.Millimetre).Column(col =>
                    {
                        col.Item().Text(item.Label.Length > 0 ? item.Label : chord.Name)
                            .FontSize(9).Bold().FontColor(style.BodyTextColor).FontFamily(style.FontFamily);
                        col.Item().PaddingTop(1)
                            .Width(40, Unit.Millimetre)
                            .Height(50, Unit.Millimetre)
                            .Canvas((canvasObj, size) =>
                                DrawChordDiagram((SKCanvas)canvasObj, size, chord.Positions[0], chord.Positions[0].Strings.Count));
                    });
                }
                else
                {
                    // Placeholder when chord not found
                    var displayName = item.Label.Length > 0 ? item.Label : "?";
                    row.AutoItem().Padding(2, Unit.Millimetre).Column(col =>
                    {
                        col.Item().Text(displayName)
                            .FontSize(9).Bold().FontColor(style.BodyTextColor).FontFamily(style.FontFamily);
                        col.Item().PaddingTop(1)
                            .Width(40, Unit.Millimetre)
                            .Height(50, Unit.Millimetre)
                            .Background("#F5F5F5")
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(displayName)
                            .FontSize(14)
                            .FontColor("#AAAAAA");
                    });
                }
            }
        });
    }

    private static void DrawChordDiagram(
        SKCanvas canvas, Size size, ChordPosition position, int stringCount)
    {
        if (stringCount < 2) stringCount = 6;
        const int fretCount = 4;

        var margin = new SKPoint(size.Width * 0.15f, size.Height * 0.15f);
        var diagramWidth = size.Width - margin.X * 2;
        var diagramHeight = size.Height - margin.Y * 2;
        var stringSpacing = diagramWidth / (stringCount - 1);
        var fretSpacing = diagramHeight / fretCount;

        using var linePaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1.5f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var dotPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextSize = size.Height * 0.07f,
            TextAlign = SKTextAlign.Center
        };

        // Draw string lines (vertical)
        for (var i = 0; i < stringCount; i++)
        {
            var x = margin.X + i * stringSpacing;
            canvas.DrawLine(x, margin.Y, x, margin.Y + diagramHeight, linePaint);
        }

        // Draw fret lines (horizontal)
        for (var i = 0; i <= fretCount; i++)
        {
            var y = margin.Y + i * fretSpacing;
            var paint = i == 0 && position.BaseFret <= 1
                ? new SKPaint { Color = SKColors.Black, StrokeWidth = 3f, IsAntialias = true, Style = SKPaintStyle.Stroke }
                : linePaint;
            canvas.DrawLine(margin.X, y, margin.X + diagramWidth, y, paint);
            if (i == 0 && position.BaseFret <= 1) paint.Dispose();
        }

        // Base fret label
        if (position.BaseFret > 1)
        {
            using var fretLabelPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = size.Height * 0.08f,
                TextAlign = SKTextAlign.Right
            };
            canvas.DrawText(
                position.BaseFret.ToString(),
                margin.X - 4,
                margin.Y + fretSpacing * 0.6f,
                fretLabelPaint);
        }

        // Draw barre
        if (position.Barre is not null)
        {
            var barreY = margin.Y + (position.Barre.Fret - position.BaseFret + 0.5f) * fretSpacing;
            var fromX = margin.X + (position.Barre.FromString - 1) * stringSpacing;
            var toX = margin.X + (position.Barre.StringTo - 1) * stringSpacing;
            using var barrePaint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = size.Height * 0.04f,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };
            canvas.DrawLine(fromX, barreY, toX, barreY, barrePaint);
        }

        // Draw string markers (open, muted, fretted)
        foreach (var s in position.Strings)
        {
            var stringX = margin.X + (s.StringNumber - 1) * stringSpacing;

            switch (s.State)
            {
                case ChordStringState.Muted:
                    var xSize = size.Height * 0.03f;
                    var xY = margin.Y - size.Height * 0.05f;
                    canvas.DrawLine(stringX - xSize, xY - xSize, stringX + xSize, xY + xSize, linePaint);
                    canvas.DrawLine(stringX - xSize, xY + xSize, stringX + xSize, xY - xSize, linePaint);
                    break;

                case ChordStringState.Open:
                    canvas.DrawCircle(stringX, margin.Y - size.Height * 0.05f, size.Height * 0.025f, linePaint);
                    break;

                case ChordStringState.Fretted when s.Fret.HasValue:
                    var fretY = margin.Y + (s.Fret.Value - position.BaseFret + 0.5f) * fretSpacing;
                    canvas.DrawCircle(stringX, fretY, size.Height * 0.035f, dotPaint);
                    break;
            }
        }
    }

    // ── Shared helpers ──────────────────────────────────────────────────

    private static void RenderSpans(TextDescriptor text, IReadOnlyList<TextSpan> spans)
    {
        foreach (var span in spans)
        {
            var segment = text.Span(span.Text);
            if (span.Bold) segment.Bold();
        }
    }
}
