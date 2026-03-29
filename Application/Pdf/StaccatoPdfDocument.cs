using DomainModels.Constants;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Application.Pdf;

public class StaccatoPdfDocument(PdfExportData data) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title = data.NotebookTitle,
        Author = data.OwnerName
    };

    public void Compose(IDocumentContainer container)
    {
        var dims = PageSizeDimensions.Dimensions[data.PageSize];
        var widthMm = (float)dims.WidthMm;
        var heightMm = (float)dims.HeightMm;

        // 1. Cover page (unnumbered)
        CoverPageRenderer.Compose(container, data, widthMm, heightMm);

        // 2. Calculate page numbers for the table of contents.
        //    Index starts at page 1. Lesson pages follow sequentially.
        //    We need to know how many index pages there will be to calculate lesson page offsets.
        //    For simplicity, we assume the index fits on one page and add more if needed.
        var tocEntries = new List<(string LessonTitle, int PageNumber)>();
        var currentPage = 2; // index is page 1, first lesson page is page 2

        // Pre-calculate: count total lesson pages to build TOC
        foreach (var lesson in data.Lessons)
        {
            tocEntries.Add((lesson.Title, currentPage));
            currentPage += lesson.Pages.Count;
        }

        // 3. Index page(s)
        IndexPageRenderer.Compose(container, data, widthMm, heightMm, tocEntries, 1);

        // 4. Lesson pages
        var globalPage = 2; // page 1 is index
        foreach (var lesson in data.Lessons)
        {
            foreach (var page in lesson.Pages)
            {
                LessonPageRenderer.Compose(container, page, data, widthMm, heightMm, globalPage);
                globalPage++;
            }
        }
    }

    public byte[] GeneratePdf()
    {
        return Document.Create(Compose).GeneratePdf();
    }
}
