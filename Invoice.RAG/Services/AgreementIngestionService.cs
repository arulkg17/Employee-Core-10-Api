using System.Security.Cryptography;
using Invoice.RAG.Configuration;
using Invoice.RAG.Documents;
using Invoice.RAG.Models;
using Invoice.RAG.Ollama;
using Invoice.RAG.Pdf;
using Invoice.RAG.Retrieval;
using Microsoft.Extensions.Options;

namespace Invoice.RAG.Services;

public sealed class AgreementIngestionService(
    IAgreementStore store,
    OllamaClient ollama,
    CoreApiClient coreApi,
    IOptions<RagOptions> options,
    ILogger<AgreementIngestionService> logger)
{
    private const int EmbedBatchSize = 16;

    public async Task<AgreementDocument> IngestAsync(
        IFormFile file, int vendorId, string? title, DateOnly? effectiveDate, string? uploadedBy, CancellationToken ct)
    {
        var o = options.Value;

        if (vendorId <= 0) throw new RagException(400, "vendorId is required.");
        if (file.Length == 0) throw new RagException(400, "The file is empty.");
        if (file.Length > o.MaxUploadBytes) throw new RagException(413, $"The file is larger than {o.MaxUploadBytes / (1024 * 1024)} MB.");

        var fileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!DocumentTextExtractor.IsSupported(extension))
            throw new RagException(400, "Only PDF (.pdf) and Word (.docx) files are accepted.");

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        // Do not trust the extension: check the file signature too.
        if (!DocumentTextExtractor.HasValidSignature(bytes, extension))
            throw new RagException(400, $"The file is not a valid {(extension == ".pdf" ? "PDF" : "Word (.docx)")} document.");

        // The vendor must exist in the business system (and the caller must be allowed to read it).
        var vendor = await coreApi.GetVendorAsync(vendorId, ct)
            ?? throw new RagException(404, $"Vendor {vendorId} was not found.");

        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var existing = await store.FindByHashAsync(vendorId, sha, ct);
        if (existing is not null)
            throw new RagException(409, $"This document is already uploaded for this vendor (agreement {existing.Id}).");

        IReadOnlyList<PageText> pages;
        try
        {
            pages = DocumentTextExtractor.Extract(new MemoryStream(bytes), extension);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document could not be read");
            throw new RagException(422, "The document could not be read (it may be corrupt or password-protected).");
        }

        var totalChars = pages.Sum(p => p.Text.Length);
        if (totalChars < 200)
            throw new RagException(422, "No extractable text was found. Scanned PDFs need OCR before upload.");
        if (totalChars > 2_000_000)
            throw new RagException(422, "The document is too large to index (more than about 2 million characters).");

        var chunks = new TextChunker(o.ChunkSize, o.ChunkOverlap).Chunk(pages);
        if (chunks.Count == 0)
            throw new RagException(422, "No text could be split into searchable sections.");

        // Embed (the section heading is included so "5. Payment Terms" helps the match).
        var texts = chunks
            .Select(c => o.Ollama.DocumentPrefix + (c.Section is null ? "" : c.Section + "\n") + c.Text)
            .ToList();

        var vectors = new List<float[]>(texts.Count);
        for (var i = 0; i < texts.Count; i += EmbedBatchSize)
            vectors.AddRange(await ollama.EmbedAsync(texts.Skip(i).Take(EmbedBatchSize).ToList(), ct));

        var agreementChunks = chunks.Select((c, i) => new AgreementChunk
        {
            ChunkIndex = c.Index,
            Page = c.Page,
            Section = c.Section,
            Content = c.Text,
            Embedding = VectorMath.Normalize(vectors[i])
        }).ToList();

        // Keep the original file (named by a random id, never by the uploaded name).
        var folder = Path.GetFullPath(o.StoragePath);
        Directory.CreateDirectory(folder);
        var storedPath = Path.Combine(folder, $"{Guid.NewGuid():N}{extension}");
        await File.WriteAllBytesAsync(storedPath, bytes, ct);

        var doc = new AgreementDocument
        {
            VendorId = vendorId,
            Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(fileName) : title.Trim(),
            FileName = fileName.Length > 260 ? fileName[..260] : fileName,
            StoredPath = storedPath,
            Sha256 = sha,
            EffectiveDate = effectiveDate,
            PageCount = pages.Count,
            ChunkCount = agreementChunks.Count,
            UploadedBy = uploadedBy,
            UploadedUtc = DateTime.UtcNow
        };

        try
        {
            await store.AddAgreementAsync(doc, agreementChunks, ct);
        }
        catch
        {
            File.Delete(storedPath);
            throw;
        }

        logger.LogInformation("Agreement {Id} for vendor {Vendor} indexed: {Pages} pages, {Chunks} chunks",
            doc.Id, vendor.Name ?? vendorId.ToString(), doc.PageCount, doc.ChunkCount);

        return doc;
    }

    public async Task DeleteAsync(int agreementId, CancellationToken ct)
    {
        var doc = await store.GetAsync(agreementId, ct) ?? throw new RagException(404, "Agreement not found.");

        await store.DeleteAsync(agreementId, ct);

        if (File.Exists(doc.StoredPath))
            File.Delete(doc.StoredPath);
    }
}
