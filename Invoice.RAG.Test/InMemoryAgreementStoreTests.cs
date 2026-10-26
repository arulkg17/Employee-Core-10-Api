using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Invoice.RAG.Models;
using Invoice.RAG.Retrieval;
using Xunit;

namespace Invoice.RAG.Test;

public class InMemoryAgreementStoreTests
{
    [Fact]
    public async Task AddFindListGetChunksSaveTermsDeleteFlow()
    {
        var store = new InMemoryAgreementStore();
        var doc = new AgreementDocument { VendorId = 42, Sha256 = "h", Title = "T", UploadedUtc = System.DateTime.UtcNow };
        var chunks = new List<AgreementChunk> { new AgreementChunk { ChunkIndex = 0, Page = 1, Content = "abc", Embedding = new float[] { 1f, 0f } } };

        var id = await store.AddAgreementAsync(doc, chunks, CancellationToken.None);
        Assert.Equal(doc.Id, id);

        var found = await store.FindByHashAsync(42, "h", CancellationToken.None);
        Assert.NotNull(found);

        var listed = await store.ListAsync(42, CancellationToken.None);
        Assert.Contains(listed, d => d.Id == id);

        var retrievedChunks = await store.GetChunksAsync(id, null, CancellationToken.None);
        Assert.Contains(retrievedChunks, c => c.AgreementId == id && c.Content == "abc");

        await store.SaveTermsAsync(id, "{ }", true, "tester", CancellationToken.None);
        var terms = await store.GetTermsAsync(id, CancellationToken.None);
        Assert.NotNull(terms);
        Assert.Equal("{ }", terms!.TermsJson);

        var deleted = await store.DeleteAsync(id, CancellationToken.None);
        Assert.True(deleted);

        var after = await store.GetAsync(id, CancellationToken.None);
        Assert.Null(after);
    }
}
