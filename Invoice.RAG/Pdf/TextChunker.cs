
using System.Text;
using System.Text.RegularExpressions;
using Invoice.RAG.Documents;

namespace Invoice.RAG.Pdf;

public sealed record TextChunk(
    int Index,
    int Page,
    string? Section,
    string Text);

/// <summary>
/// Splits PDF text into chunks while keeping section metadata aligned
/// with the text in each chunk. Chunks never cross a detected section
/// heading or PDF page boundary.
/// </summary>
public sealed class TextChunker(int chunkSize, int overlap)
{
    private static readonly Regex HeadingRegex = new(
        @"^\s*(?:(?:ARTICLE|SECTION|CLAUSE)\s+)?\d{1,2}(?:\.\d{1,2})*[.)]?\s+\S.*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PageText> pages)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
        ArgumentOutOfRangeException.ThrowIfNegative(overlap);

        var chunks = new List<TextChunk>();
        string? section = null;
        var index = 0;

        foreach (var page in pages)
        {
            var buffer = new StringBuilder();
            var hasFreshText = false;

            foreach (var raw in page.Text.Split('\n'))
            {
                var line = raw.Trim();

                // Word headings arrive pre-marked by DocxTextExtractor.
                var marked = line.StartsWith(DocxTextExtractor.HeadingMarker, StringComparison.Ordinal);
                if (marked)
                    line = line[DocxTextExtractor.HeadingMarker.Length..].Trim();

                if (line.Length == 0)
                    continue;

                var isHeading =
                    marked ||
                    (line.Length <= 100 &&
                     !line.EndsWith('.') &&
                     HeadingRegex.IsMatch(line));

                if (isHeading)
                {
                    // Finish content belonging to the previous section.
                    if (hasFreshText)
                    {
                        AddChunk(buffer, page.PageNumber, section, chunks, ref index);
                    }

                    // Do not carry overlap from one section into another.
                    buffer.Clear();

                    section = line.Length > 200 ? line[..200] : line;   // Section column is NVARCHAR(200)
                    buffer.Append(line).Append('\n');
                    hasFreshText = true;
                    continue;
                }

                buffer.Append(line).Append('\n');
                hasFreshText = true;

                if (buffer.Length >= chunkSize)
                {
                    var text = buffer.ToString().Trim();

                    if (text.Length > 0)
                    {
                        chunks.Add(new TextChunk(
                            index++,
                            page.PageNumber,
                            section,
                            text));
                    }

                    buffer.Clear();
                    buffer.Append(Tail(text));
                    hasFreshText = false;
                }
            }

            if (hasFreshText)
            {
                AddChunk(buffer, page.PageNumber, section, chunks, ref index);
            }
        }

        return chunks;
    }

    private static void AddChunk(
        StringBuilder buffer,
        int pageNumber,
        string? section,
        List<TextChunk> chunks,
        ref int index)
    {
        var text = buffer.ToString().Trim();

        if (text.Length > 0)
        {
            chunks.Add(new TextChunk(
                index++,
                pageNumber,
                section,
                text));
        }

        buffer.Clear();
    }

    private string Tail(string text)
    {
        if (overlap <= 0 || text.Length == 0)
            return string.Empty;

        var start = Math.Max(0, text.Length - overlap);
        var space = text.IndexOf(' ', start);

        if (space > 0)
            start = space + 1;

        return start < text.Length
            ? text[start..] + "\n"
            : string.Empty;
    }
}
