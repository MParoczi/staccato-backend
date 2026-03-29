using System.Globalization;
using DomainModels.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Pdf;

public static class CoverPageRenderer
{
    public static void Compose(IDocumentContainer container, PdfExportData data, float widthMm, float heightMm)
    {
        container.Page(page =>
        {
            page.Size(widthMm, heightMm, Unit.Millimetre);
            page.Margin(0);

            page.Content().Container()
                .Background(data.CoverColor)
                .AlignCenter()
                .AlignMiddle()
                .Column(column =>
                {
                    column.Item().AlignCenter()
                        .Text(data.NotebookTitle)
                        .FontSize(28)
                        .FontColor("#FFFFFF")
                        .Bold()
                        .FontFamily("Arial");

                    column.Item().PaddingTop(8).AlignCenter()
                        .Text(data.InstrumentName)
                        .FontSize(16)
                        .FontColor("#FFFFFF")
                        .FontFamily("Arial");

                    column.Item().PaddingTop(8).AlignCenter()
                        .Text(data.OwnerName)
                        .FontSize(14)
                        .FontColor("#FFFFFF")
                        .FontFamily("Arial");

                    column.Item().PaddingTop(8).AlignCenter()
                        .Text(FormatDate(data.CreatedAt, data.Language))
                        .FontSize(12)
                        .FontColor("#FFFFFF")
                        .FontFamily("Arial");
                });
        });
    }

    private static string FormatDate(DateTime date, Language language)
    {
        var culture = language == Language.Hungarian
            ? new CultureInfo("hu-HU")
            : new CultureInfo("en-US");

        return date.ToString("D", culture);
    }
}
