using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Invoice.RAG.Pdf;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Invoice.RAG.Documents;

/// <summary>
/// Reads the text of a Word (.docx) file with the Open XML SDK (pure .NET, Word is not needed).
/// - Paragraphs and tables (cells joined with " | ") are read in document order.
/// - Word "pages" are only known where the file has a page break, so a file without page breaks is page 1.
/// - Paragraphs in a Heading/Title style are emitted with the "## " marker; TextChunker turns them into sections.
/// - Tracked-change deletions are ignored. Headers, footers, footnotes and text boxes are not read.
/// Legacy .doc files are not supported (save them as .docx).
/// </summary>
public static class DocxTextExtractor
{
    public const string HeadingMarker = "## ";

    public static IReadOnlyList<PageText> Extract(Stream docx)
    {
        using var document = WordprocessingDocument.Open(docx, false);
        var body = document.MainDocumentPart?.Document?.Body
            ?? throw new InvalidDataException("The Word document has no body.");

        var pages = new List<StringBuilder> { new() };
        Walk(body, pages);

        var result = new List<PageText>();
        foreach (var page in pages)
        {
            var text = Clean(page.ToString());
            if (text.Length > 0)
                result.Add(new PageText(result.Count + 1, text));
        }
        return result;
    }

    private static void Walk(DocumentFormat.OpenXml.OpenXmlElement parent, List<StringBuilder> pages)
    {
        foreach (var child in parent.ChildElements)
        {
            switch (child)
            {
                case W.Paragraph p:
                    WriteParagraph(p, pages);
                    break;
                case W.Table t:
                    WriteTable(t, pages);
                    break;
                case W.SdtBlock sdt when sdt.SdtContentBlock is { } content:
                    Walk(content, pages);   // content controls (common in templates)
                    break;
            }
        }
    }

    private static void WriteTable(W.Table table, List<StringBuilder> pages)
    {
        foreach (var row in table.Elements<W.TableRow>())
        {
            var cells = row.Elements<W.TableCell>()
                .Select(c => string.Join(" ", c.Descendants<W.Paragraph>().Select(PlainText)).Trim())
                .Where(s => s.Length > 0);

            var line = string.Join(" | ", cells);
            if (line.Length > 0)
                pages[^1].AppendLine(line);
        }
    }

    private static void WriteParagraph(W.Paragraph p, List<StringBuilder> pages)
    {
        var heading = IsHeading(p);
        var line = new StringBuilder();

        void Flush()
        {
            var text = line.ToString().Trim();
            line.Clear();
            if (text.Length == 0) return;

            if (heading)
            {
                pages[^1].Append(HeadingMarker);
                heading = false;
            }
            pages[^1].AppendLine(text);
        }

        foreach (var node in p.Descendants())
        {
            switch (node)
            {
                case W.Text t:
                    line.Append(t.Text);
                    break;
                case W.TabChar:
                    line.Append(' ');
                    break;
                case W.Break b when b.Type is { } type && type.Value == W.BreakValues.Page:
                    Flush();
                    NewPage(pages);
                    break;
                case W.Break:
                    line.Append(' ');
                    break;
                case W.LastRenderedPageBreak:
                    Flush();
                    NewPage(pages);
                    break;
            }
        }

        Flush();
    }

    // Used for table cells: text only, no page handling.
    private static string PlainText(W.Paragraph p)
    {
        var sb = new StringBuilder();
        foreach (var node in p.Descendants())
        {
            if (node is W.Text t) sb.Append(t.Text);
            else if (node is W.TabChar or W.Break) sb.Append(' ');
        }
        return sb.ToString().Trim();
    }

    private static bool IsHeading(W.Paragraph p)
    {
        var style = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return style is not null &&
               (style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ||
                style.Equals("Title", StringComparison.OrdinalIgnoreCase));
    }

    // Word writes both an explicit page break and a "last rendered" marker for the same break: start a page only once.
    private static void NewPage(List<StringBuilder> pages)
    {
        if (pages[^1].ToString().Trim().Length > 0)
            pages.Add(new StringBuilder());
    }

    private static string Clean(string text)
    {
        var t = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00A0', ' ');
        return Regex.Replace(t, @"[ \t]+", " ").Trim();
    }
}
