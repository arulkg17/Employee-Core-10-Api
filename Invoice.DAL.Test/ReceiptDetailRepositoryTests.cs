using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.Model;

namespace Invoice.DAL.Test;

[Collection(TransactionDbCollection.Name)]
public class ReceiptDetailRepositoryTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SeedData _seed = null!;
    private PoSeed _po = null!;
    private int _receiptId;
    private ReceiptDetailRepositoryEFSp _repository = null!;

    public async Task InitializeAsync()
    {
        _context = TxTestHelper.CreateDbContext();

        await TxTestHelper.CleanupAsync(_context);

        _seed = await TxTestHelper.SeedAsync(_context);
        _po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-001");

        _receiptId = await TxTestHelper.CreateDraftReceiptAsync(_context, _po, qtyA: 0);   // header only

        _repository = new ReceiptDetailRepositoryEFSp(_context);
    }

    public async Task DisposeAsync()
    {
        await TxTestHelper.CleanupAsync(_context);
        await _context.DisposeAsync();
    }

    private ReceiptDetailEntity LineA(decimal qty = 3m)
        => TxTestHelper.NewReceiptLine(_receiptId, _po.LineAId, _po.ItemAId, qty, 100m);

    private ReceiptDetailEntity LineB(decimal qty = 2m)
        => TxTestHelper.NewReceiptLine(_receiptId, _po.LineBId, _po.ItemBId, qty, 200m);

    [Fact]
    public async Task AddAsync_ShouldInsertDetail()
    {
        var id = await _repository.AddAsync(LineA(3));

        var saved = await _repository.GetByIdAsync(id);

        Assert.True(id > 0);
        Assert.NotNull(saved);
        Assert.Equal(_receiptId, saved!.ReceiptId);
        Assert.Equal(_po.LineAId, saved.PurchaseOrderDetailId);
        Assert.Equal(3m, saved.ReceivedQuantity);
        Assert.Equal(100m, saved.Rate);
        Assert.Equal(330m, saved.LineTotal);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenQuantityExceedsOutstanding()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(LineA(11)));          // PO line quantity is 10
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenQuantityIsZero()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(     // CK_ReceiptDetail_Quantity (error 547)
            () => _repository.AddAsync(LineA(0)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenPoLineBelongsToAnotherPo()
    {
        var otherPo = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-OTHER");

        var line = TxTestHelper.NewReceiptLine(
            _receiptId, otherPo.LineAId, otherPo.ItemAId, 1m, 100m);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.AddAsync(line));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenItemDoesNotMatchPoLine()
    {
        var line = TxTestHelper.NewReceiptLine(
            _receiptId, _po.LineAId, _po.ItemBId, 1m, 100m);   // line A is item A

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.AddAsync(line));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenSamePoLineIsAddedTwice()
    {
        await _repository.AddAsync(LineA(3));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.AddAsync(LineA(2)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenReceiptIsPosted()
    {
        var postedId = await TxTestHelper.ReceiveStockAsync(_context, _po, 2, number: "TXT-GRN-POSTED");

        var line = TxTestHelper.NewReceiptLine(postedId, _po.LineBId, _po.ItemBId, 1m, 200m);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.AddAsync(line));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowNotFound_WhenReceiptDoesNotExist()
    {
        var line = TxTestHelper.NewReceiptLine(-1, _po.LineAId, _po.ItemAId, 1m, 100m);

        await Assert.ThrowsAsync<NotFoundException>(() => _repository.AddAsync(line));
    }

    [Fact]
    public async Task GetByReceiptIdAsync_ShouldReturnDetailsOrderedById()
    {
        var first = await _repository.AddAsync(LineA());
        var second = await _repository.AddAsync(LineB());

        var details = (await _repository.GetByReceiptIdAsync(_receiptId)).ToList();

        Assert.Equal(2, details.Count);
        Assert.Equal(first, details[0].Id);
        Assert.Equal(second, details[1].Id);
    }

    [Fact]
    public async Task GetByReceiptIdAsync_ShouldReturnEmpty_WhenReceiptHasNoDetails()
    {
        Assert.Empty(await _repository.GetByReceiptIdAsync(_receiptId));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        Assert.Null(await _repository.GetByIdAsync(-1));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDetail()
    {
        var id = await _repository.AddAsync(LineA(3));

        var detail = (await _repository.GetByIdAsync(id))!;
        detail.ReceivedQuantity = 5m;
        detail.TaxAmount = 50m;
        detail.LineTotal = 550m;

        var updated = await _repository.UpdateAsync(detail);
        var saved = await _repository.GetByIdAsync(id);

        Assert.True(updated);
        Assert.Equal(5m, saved!.ReceivedQuantity);
        Assert.Equal(550m, saved.LineTotal);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenQuantityExceedsOutstanding()
    {
        var id = await _repository.AddAsync(LineA(3));

        var detail = (await _repository.GetByIdAsync(id))!;
        detail.ReceivedQuantity = 11m;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.UpdateAsync(detail));
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenIdDoesNotExist()
    {
        var detail = LineA();
        detail.Id = -1;

        Assert.False(await _repository.UpdateAsync(detail));
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteDetail()
    {
        var id = await _repository.AddAsync(LineA());

        var deleted = await _repository.DeleteAsync(id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenIdDoesNotExist()
    {
        Assert.False(await _repository.DeleteAsync(-1));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowBusinessRule_WhenReceiptIsPosted()
    {
        var postedId = await TxTestHelper.ReceiveStockAsync(_context, _po, 2, number: "TXT-GRN-POSTED");
        var detail = (await _repository.GetByReceiptIdAsync(postedId)).Single();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.DeleteAsync(detail.Id));
    }

    [Fact]
    public async Task DeleteByReceiptIdAsync_ShouldDeleteAllDetails()
    {
        await _repository.AddAsync(LineA());
        await _repository.AddAsync(LineB());

        var deleted = await _repository.DeleteByReceiptIdAsync(_receiptId);

        Assert.True(deleted);
        Assert.Empty(await _repository.GetByReceiptIdAsync(_receiptId));
    }

    [Fact]
    public async Task DeleteByReceiptIdAsync_ShouldReturnFalse_WhenReceiptHasNoDetails()
    {
        Assert.False(await _repository.DeleteByReceiptIdAsync(_receiptId));
    }
}
