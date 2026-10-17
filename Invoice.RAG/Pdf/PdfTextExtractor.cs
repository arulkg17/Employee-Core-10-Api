using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Invoice.RAG.Pdf;

public sealed record PageText(int PageNumber, string Text);

public static class PdfTextExtractor
{
    /// <summary>
    /// Reads the text layer of a PDF page by page. Scanned PDFs have no text layer (they need OCR first).
    /// If ContentOrderTextExtractor does not compile with your PdfPig version, use page.Text instead.
    /// </summary>
    public static IReadOnlyList<PageText> Extract(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        var pages = new List<PageText>();

        foreach (var page in document.GetPages())
            pages.Add(new PageText(page.Number, Clean(ContentOrderTextExtractor.GetText(page))));

        return pages;
    }

    private static string Clean(string text)
    {
        var t = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00A0', ' ');
        return Regex.Replace(t, @"[ \t]+", " ").Trim();
    }
}
