using DomainModels.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Pdf;

public static class IndexPageRenderer
{
    private static readonly IReadOnlyDictionary<Language, string> HeadingText =
        new Dictionary<Language, string>
        {
            { Language.English, "Table of Contents" },
            { Language.Hungarian, "Tartalomjegyzék" }
        };

    public static int Compose(
        IDocumentContainer container,
        PdfExportData data,
        float widthMm,
        float heightMm,
        IReadOnlyList<(string LessonTitle, int PageNumber)> tocEntries,
        int startPageNumber)
    {
        var pageCount = 0;

        container.Page(page =>
        {
            page.Size(widthMm, heightMm, Unit.Millimetre);
            page.Margin(0);

            page.Background().Container()
                .DrawDottedBackground(widthMm, heightMm);

            page.Content().Padding(15, Unit.Millimetre).Column(column =>
            {
                column.Item().PaddingBottom(10)
                    .Text(HeadingText.GetValueOrDefault(data.Language, "Table of Contents"))
                    .FontSize(20)
                    .Bold()
                    .FontFamily("Arial");

                foreach (var (title, pageNumber) in tocEntries)
                {
                    column.Item().PaddingVertical(2).Row(row =>
                    {
                        row.RelativeItem()
                            .Text(title)
                            .FontSize(11)
                            .FontFamily("Arial");

                        row.ConstantItem(30)
                            .AlignRight()
                            .Text(pageNumber.ToString())
                            .FontSize(11)
                            .FontFamily("Arial");
                    });
                }
            });

            page.Footer().AlignRight().Padding(10, Unit.Millimetre)
                .Text(text =>
                {
                    text.Span(startPageNumber.ToString());
                });
        });

        return pageCount;
    }
}
