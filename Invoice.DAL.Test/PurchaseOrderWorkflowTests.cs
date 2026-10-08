using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.Model;

namespace Invoice.DAL.Test;

/// <summary>Tests for the PO changes: Approve, Cancel, edit/delete guards, ReceivedQuantity.</summary>
[Collection(TransactionDbCollection.Name)]
public class PurchaseOrderWorkflowTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SeedData _seed = null!;
    private PurchaseOrderRepositoryEFSp _repository = null!;
    private PurchaseOrderDetailRepositoryEFSp _detailRepository = null!;

    public async Task InitializeAsync()
    {
        _context = TxTestHelper.CreateDbContext();

        await TxTestHelper.CleanupAsync(_context);

        _seed = await TxTestHelper.SeedAsync(_context);

        _repository = new PurchaseOrderRepositoryEFSp(_context);
        _detailRepository = new PurchaseOrderDetailRepositoryEFSp(_context);
    }

    public async Task DisposeAsync()
    {
        await TxTestHelper.CleanupAsync(_context);
        await _context.DisposeAsync();
    }

    private async Task<int> CreateDraftPoAsync(string number = "TXT-PO-DRAFT")
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, number, status: "Draft");
        return po.PurchaseOrderId;
    }

    // ------------------------------------------------------------
    // Approve
    // ------------------------------------------------------------
    [Fact]
    public async Task ApproveAsync_ShouldApproveDraftPo()
    {
        var id = await CreateDraftPoAsync();

        var approved = await _repository.ApproveAsync(id, TxTestHelper.TestUser);

        var po = await _repository.GetByIdAsync(id);

        Assert.True(approved);
        Assert.Equal("Approved", po!.Status);
        Assert.Equal(TxTestHelper.TestUser, po.UpdatedBy);
    }

    [Fact]
    public async Task ApproveAsync_ShouldThrowBusinessRule_WhenPoIsNotDraft()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-APPROVED");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.ApproveAsync(po.PurchaseOrderId, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task ApproveAsync_ShouldThrowBusinessRule_WhenPoHasNoLines()
    {
        var emptyPo = new PurchaseOrderEntity
        {
            PONumber = "TXT-PO-EMPTY",
            PODate = DateTime.Now,
            VendorId = _seed.VendorId,
            Status = "Draft",
            CreatedBy = TxTestHelper.TestUser,
            CreatedDate = DateTime.Now
        };

        _context.PurchaseOrders.Add(emptyPo);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.ApproveAsync(emptyPo.Id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task ApproveAsync_ShouldThrowNotFound_WhenPoDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.ApproveAsync(-1, TxTestHelper.TestUser));
    }

    // ------------------------------------------------------------
    // Cancel
    // ------------------------------------------------------------
    [Fact]
    public async Task CancelAsync_ShouldCancelDraftPo()
    {
        var id = await CreateDraftPoAsync();

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        Assert.Equal("Cancelled", (await _repository.GetByIdAsync(id))!.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelApprovedPo_WithoutReceipts()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-APPROVED");

        await _repository.CancelAsync(po.PurchaseOrderId, TxTestHelper.TestUser);

        Assert.Equal("Cancelled", (await _repository.GetByIdAsync(po.PurchaseOrderId))!.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowBusinessRule_WhenPoIsPartiallyReceived()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-PARTIAL");
        await TxTestHelper.ReceiveStockAsync(_context, po, qtyA: 3);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.CancelAsync(po.PurchaseOrderId, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowBusinessRule_WhenADraftReceiptExists()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-OPENGRN");
        await TxTestHelper.CreateDraftReceiptAsync(_context, po, qtyA: 3);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.CancelAsync(po.PurchaseOrderId, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task CancelAsync_ShouldSucceed_AfterTheReceiptIsCancelled()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-AFTERCANCEL");
        var receiptId = await TxTestHelper.ReceiveStockAsync(_context, po, qtyA: 3);

        await new ReceiptRepositoryEFSp(_context).CancelAsync(receiptId, TxTestHelper.TestUser);
        await _repository.CancelAsync(po.PurchaseOrderId, TxTestHelper.TestUser);

        Assert.Equal("Cancelled", (await _repository.GetByIdAsync(po.PurchaseOrderId))!.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowBusinessRule_WhenAlreadyCancelled()
    {
        var id = await CreateDraftPoAsync();

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.CancelAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowNotFound_WhenPoDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.CancelAsync(-1, TxTestHelper.TestUser));
    }

    // ------------------------------------------------------------
    // Edit / delete guards
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenPoIsApproved()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-APPROVED");

        var entity = (await _repository.GetByIdAsync(po.PurchaseOrderId))!;
        entity.Notes = "changed";

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.UpdateAsync(entity));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenStatusIsSetToReceived()
    {
        var id = await CreateDraftPoAsync();

        var entity = (await _repository.GetByIdAsync(id))!;
        entity.Status = "Received";

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.UpdateAsync(entity));
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenPoDoesNotExist()
    {
        var updated = await _repository.UpdateAsync(new PurchaseOrderEntity
        {
            Id = -1,
            PONumber = "X",
            PODate = DateTime.Today,
            Status = "Draft"
        });

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteDraftPo_AndKeepItsLines()
    {
        var id = await CreateDraftPoAsync();

        var deleted = await _repository.DeleteAsync(id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(id));
        Assert.Equal(2, (await _detailRepository.GetByPurchaseOrderIdAsync(id)).Count());
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowBusinessRule_WhenPoIsApproved()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-APPROVED");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.DeleteAsync(po.PurchaseOrderId));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenPoDoesNotExist()
    {
        Assert.False(await _repository.DeleteAsync(-1));
    }

    // ------------------------------------------------------------
    // ReceivedQuantity is exposed by the detail reads
    // ------------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnReceivedQuantity()
    {
        var po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-RQ");
        await TxTestHelper.ReceiveStockAsync(_context, po, qtyA: 4);

        var line = await _detailRepository.GetByIdAsync(po.LineAId);

        Assert.Equal(4m, line!.ReceivedQuantity);
    }

    // ------------------------------------------------------------
    // Regression: GetAllPagedAsync used to fail when the connection was already open
    // ------------------------------------------------------------
    [Fact]
    public async Task GetAllPagedAsync_ShouldWork_AfterAnotherCallOnTheSameContext()
    {
        await CreateDraftPoAsync("TXT-PO-PAGE1");
        await _repository.GetAllAsync();

        var page = await _repository.GetAllPagedAsync("TXT-PO-PAGE", null, null, 1, 10);

        Assert.Equal(1, page.TotalRecords);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByStatus()
    {
        await CreateDraftPoAsync("TXT-PO-D");
        await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-A");

        var approved = await _repository.GetAllPagedAsync("TXT-PO-", null, "Approved", 1, 10);

        Assert.Equal(1, approved.TotalRecords);
        Assert.Equal("TXT-PO-A", approved.Data.Single().PONumber);
    }
}
