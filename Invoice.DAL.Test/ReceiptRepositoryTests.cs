using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.Model;

namespace Invoice.DAL.Test;

[Collection(TransactionDbCollection.Name)]
public class ReceiptRepositoryTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SeedData _seed = null!;
    private PoSeed _po = null!;
    private ReceiptRepositoryEFSp _repository = null!;
    private PurchaseOrderRepositoryEFSp _poRepository = null!;
    private PurchaseOrderDetailRepositoryEFSp _poDetailRepository = null!;

    public async Task InitializeAsync()
    {
        _context = TxTestHelper.CreateDbContext();

        await TxTestHelper.CleanupAsync(_context);

        _seed = await TxTestHelper.SeedAsync(_context);
        _po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-001");

        _repository = new ReceiptRepositoryEFSp(_context);
        _poRepository = new PurchaseOrderRepositoryEFSp(_context);
        _poDetailRepository = new PurchaseOrderDetailRepositoryEFSp(_context);
    }

    public async Task DisposeAsync()
    {
        await TxTestHelper.CleanupAsync(_context);
        await _context.DisposeAsync();
    }

    private ReceiptEntity NewReceipt(string? number = "TXT-GRN-001", int? poId = null) => new()
    {
        ReceiptNumber = number ?? string.Empty,
        ReceiptDate = DateTime.Today,
        PurchaseOrderId = poId ?? _po.PurchaseOrderId,
        Notes = "TX receipt",
        SubTotal = 100,
        TaxAmount = 10,
        TotalAmount = 110,
        CreatedBy = TxTestHelper.TestUser
    };

    // ------------------------------------------------------------
    // AddAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task AddAsync_ShouldInsertDraftReceipt_AndTakeVendorFromPo()
    {
        var id = await _repository.AddAsync(NewReceipt());

        var saved = await _repository.GetByIdAsync(id);

        Assert.True(id > 0);
        Assert.NotNull(saved);
        Assert.Equal("TXT-GRN-001", saved!.ReceiptNumber);
        Assert.Equal("Draft", saved.Status);
        Assert.Equal(_seed.VendorId, saved.VendorId);
        Assert.Equal(_po.PurchaseOrderId, saved.PurchaseOrderId);
        Assert.Equal(110m, saved.TotalAmount);
        Assert.False(saved.IsDeleted);
    }

    [Fact]
    public async Task AddAsync_ShouldGenerateNumber_WhenNumberIsEmpty()
    {
        var id = await _repository.AddAsync(NewReceipt(number: null));

        var saved = await _repository.GetByIdAsync(id);

        Assert.StartsWith("GRN-", saved!.ReceiptNumber);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenPoIsDraft()
    {
        var draftPo = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-DRAFT", status: "Draft");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(NewReceipt(poId: draftPo.PurchaseOrderId)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowNotFound_WhenPoDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.AddAsync(NewReceipt(poId: -1)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenReceiptNumberIsDuplicate()
    {
        await _repository.AddAsync(NewReceipt("TXT-GRN-DUP"));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(NewReceipt("TXT-GRN-DUP")));
    }

    // ------------------------------------------------------------
    // GetById / GetAll
    // ------------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        Assert.Null(await _repository.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyNotDeletedReceipts()
    {
        var keep = await _repository.AddAsync(NewReceipt("TXT-GRN-KEEP"));
        var remove = await _repository.AddAsync(NewReceipt("TXT-GRN-REMOVE"));

        await _repository.DeleteAsync(remove, TxTestHelper.TestUser);

        var all = (await _repository.GetAllAsync())
            .Where(x => x.ReceiptNumber.StartsWith("TXT-GRN-"))
            .ToList();

        Assert.Single(all);
        Assert.Equal(keep, all[0].Id);
    }

    // ------------------------------------------------------------
    // UpdateAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateAsync_ShouldUpdateDraftReceipt()
    {
        var id = await _repository.AddAsync(NewReceipt());

        var updated = await _repository.UpdateAsync(new ReceiptEntity
        {
            Id = id,
            ReceiptDate = DateTime.Today.AddDays(1),
            Notes = "changed",
            SubTotal = 200,
            TaxAmount = 20,
            TotalAmount = 220,
            UpdatedBy = TxTestHelper.TestUser
        });

        var saved = await _repository.GetByIdAsync(id);

        Assert.True(updated);
        Assert.Equal("changed", saved!.Notes);
        Assert.Equal(220m, saved.TotalAmount);
        Assert.Equal(TxTestHelper.TestUser, saved.UpdatedBy);
        Assert.NotNull(saved.UpdatedDate);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenReceiptDoesNotExist()
    {
        var updated = await _repository.UpdateAsync(new ReceiptEntity
        {
            Id = -1,
            ReceiptDate = DateTime.Today
        });

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenReceiptIsPosted()
    {
        var id = await TxTestHelper.ReceiveStockAsync(_context, _po, 5);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.UpdateAsync(new ReceiptEntity { Id = id, ReceiptDate = DateTime.Today }));
    }

    // ------------------------------------------------------------
    // DeleteAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteDraftReceipt()
    {
        var id = await _repository.AddAsync(NewReceipt());

        var deleted = await _repository.DeleteAsync(id, TxTestHelper.TestUser);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenReceiptDoesNotExist()
    {
        Assert.False(await _repository.DeleteAsync(-1));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowBusinessRule_WhenReceiptIsPosted()
    {
        var id = await TxTestHelper.ReceiveStockAsync(_context, _po, 5);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.DeleteAsync(id));
    }

    // ------------------------------------------------------------
    // Paged
    // ------------------------------------------------------------
    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnRequestedPageAndTotal()
    {
        for (var i = 1; i <= 5; i++)
            await _repository.AddAsync(NewReceipt($"TXT-GRN-P{i:00}"));

        var page = await _repository.GetAllPagedAsync(null, null, null, null, 1, 2);

        Assert.Equal(5, page.TotalRecords);
        Assert.Equal(2, page.Data.Count());
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnSecondPage()
    {
        for (var i = 1; i <= 5; i++)
            await _repository.AddAsync(NewReceipt($"TXT-GRN-P{i:00}"));

        var page = await _repository.GetAllPagedAsync(null, null, null, null, 3, 2);

        Assert.Equal(5, page.TotalRecords);
        Assert.Single(page.Data);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByNumber()
    {
        await _repository.AddAsync(NewReceipt("TXT-GRN-AAA"));
        await _repository.AddAsync(NewReceipt("TXT-GRN-BBB"));

        var page = await _repository.GetAllPagedAsync("AAA", null, null, null, 1, 10);

        Assert.Equal(1, page.TotalRecords);
        Assert.Equal("TXT-GRN-AAA", page.Data.Single().ReceiptNumber);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByStatus()
    {
        await _repository.AddAsync(NewReceipt("TXT-GRN-DRAFT"));
        await TxTestHelper.ReceiveStockAsync(_context, _po, 2, number: "TXT-GRN-POSTED");

        var posted = await _repository.GetAllPagedAsync(null, null, null, "Posted", 1, 10);
        var draft = await _repository.GetAllPagedAsync(null, null, null, "Draft", 1, 10);

        Assert.Equal(1, posted.TotalRecords);
        Assert.Equal("TXT-GRN-POSTED", posted.Data.Single().ReceiptNumber);
        Assert.Equal(1, draft.TotalRecords);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByPurchaseOrderAndVendor()
    {
        var otherPo = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-002");

        await _repository.AddAsync(NewReceipt("TXT-GRN-PO1", _po.PurchaseOrderId));
        await _repository.AddAsync(NewReceipt("TXT-GRN-PO2", otherPo.PurchaseOrderId));

        var byPo = await _repository.GetAllPagedAsync(null, otherPo.PurchaseOrderId, null, null, 1, 10);
        var byVendor = await _repository.GetAllPagedAsync(null, null, _seed.VendorId, null, 1, 10);

        Assert.Equal(1, byPo.TotalRecords);
        Assert.Equal("TXT-GRN-PO2", byPo.Data.Single().ReceiptNumber);
        Assert.Equal(2, byVendor.TotalRecords);
    }

    // ------------------------------------------------------------
    // PostAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task PostAsync_ShouldPostReceipt_AndMarkPoPartiallyReceived()
    {
        var id = await TxTestHelper.CreateDraftReceiptAsync(_context, _po, qtyA: 4);

        var posted = await _repository.PostAsync(id, TxTestHelper.TestUser);

        var receipt = await _repository.GetByIdAsync(id);
        var po = await _poRepository.GetByIdAsync(_po.PurchaseOrderId);
        var lines = (await _poDetailRepository.GetByPurchaseOrderIdAsync(_po.PurchaseOrderId)).ToList();

        Assert.True(posted);
        Assert.Equal("Posted", receipt!.Status);
        Assert.Equal("PartiallyReceived", po!.Status);
        Assert.Equal(4m, lines.Single(x => x.Id == _po.LineAId).ReceivedQuantity);
        Assert.Equal(0m, lines.Single(x => x.Id == _po.LineBId).ReceivedQuantity);
    }

    [Fact]
    public async Task PostAsync_ShouldMarkPoReceived_WhenAllLinesAreFullyReceived()
    {
        var id = await TxTestHelper.CreateDraftReceiptAsync(_context, _po, qtyA: 10, qtyB: 5);

        await _repository.PostAsync(id, TxTestHelper.TestUser);

        var po = await _poRepository.GetByIdAsync(_po.PurchaseOrderId);

        Assert.Equal("Received", po!.Status);
    }

    [Fact]
    public async Task PostAsync_ShouldIncreaseStock()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 6, qtyB: 2);

        Assert.Equal(6m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
        Assert.Equal(2m, await TxTestHelper.OnHandAsync(_context, _seed.ItemBId));
    }

    [Fact]
    public async Task PostAsync_ShouldAllowSecondReceipt_ForRemainingQuantity()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 6, number: "TXT-GRN-1");
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 4, qtyB: 5, number: "TXT-GRN-2");

        var po = await _poRepository.GetByIdAsync(_po.PurchaseOrderId);

        Assert.Equal("Received", po!.Status);
        Assert.Equal(10m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenReceiptIsAlreadyPosted()
    {
        var id = await TxTestHelper.ReceiveStockAsync(_context, _po, 3);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenReceiptHasNoLines()
    {
        var id = await _repository.AddAsync(NewReceipt());

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenTwoDraftsTogetherExceedPoQuantity()
    {
        // each draft is valid alone (6 <= 10) but together they are 12 > 10
        var first = await TxTestHelper.CreateDraftReceiptAsync(_context, _po, 6, number: "TXT-GRN-D1");
        var second = await TxTestHelper.CreateDraftReceiptAsync(_context, _po, 6, number: "TXT-GRN-D2");

        await _repository.PostAsync(first, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(second, TxTestHelper.TestUser));

        var lines = await _poDetailRepository.GetByPurchaseOrderIdAsync(_po.PurchaseOrderId);

        Assert.Equal(6m, lines.Single(x => x.Id == _po.LineAId).ReceivedQuantity);   // second post rolled back
    }

    [Fact]
    public async Task PostAsync_ShouldThrowNotFound_WhenReceiptDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.PostAsync(-1, TxTestHelper.TestUser));
    }

    // ------------------------------------------------------------
    // CancelAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task CancelAsync_ShouldCancelDraftReceipt()
    {
        var id = await _repository.AddAsync(NewReceipt());

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        var receipt = await _repository.GetByIdAsync(id);

        Assert.Equal("Cancelled", receipt!.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldReversePoQuantitiesAndStock_WhenReceiptWasPosted()
    {
        var id = await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 4);

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        var po = await _poRepository.GetByIdAsync(_po.PurchaseOrderId);
        var lines = await _poDetailRepository.GetByPurchaseOrderIdAsync(_po.PurchaseOrderId);

        Assert.Equal("Approved", po!.Status);
        Assert.Equal(0m, lines.Single(x => x.Id == _po.LineAId).ReceivedQuantity);
        Assert.Equal(0m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowBusinessRule_WhenReceiptIsAlreadyCancelled()
    {
        var id = await _repository.AddAsync(NewReceipt());

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.CancelAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowNotFound_WhenReceiptDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.CancelAsync(-1, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenPoIsFullyReceived()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10, qtyB: 5);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(NewReceipt("TXT-GRN-LATE")));
    }
}
