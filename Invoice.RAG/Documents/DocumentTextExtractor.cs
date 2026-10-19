using Invoice.RAG.Pdf;

namespace Invoice.RAG.Documents;

/// <summary>Picks the right reader for an uploaded agreement: .pdf or .docx.</summary>
public static class DocumentTextExtractor
{
    public static bool IsSupported(string extension) => extension is ".pdf" or ".docx";

    public static string ContentTypeFor(string pathOrName) =>
        Path.GetExtension(pathOrName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

    /// <summary>Do not trust the extension: a PDF starts with "%PDF-", a .docx is a zip ("PK\3\4").</summary>
    public static bool HasValidSignature(byte[] b, string extension) => extension switch
    {
        ".pdf" => b.Length >= 5 && b[0] == '%' && b[1] == 'P' && b[2] == 'D' && b[3] == 'F' && b[4] == '-',
        ".docx" => b.Length >= 4 && b[0] == 'P' && b[1] == 'K' && b[2] == 3 && b[3] == 4,
        _ => false
    };

    public static IReadOnlyList<PageText> Extract(Stream stream, string extension) => extension switch
    {
        ".pdf" => PdfTextExtractor.Extract(stream),
        ".docx" => DocxTextExtractor.Extract(stream),
        _ => throw new NotSupportedException($"Unsupported document type '{extension}'.")
    };
}
