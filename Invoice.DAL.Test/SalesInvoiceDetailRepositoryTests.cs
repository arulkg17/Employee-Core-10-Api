using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.Model;

namespace Invoice.DAL.Test;

[Collection(TransactionDbCollection.Name)]
public class SalesInvoiceDetailRepositoryTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SeedData _seed = null!;
    private int _invoiceId;
    private SalesInvoiceDetailRepositoryEFSp _repository = null!;

    public async Task InitializeAsync()
    {
        _context = TxTestHelper.CreateDbContext();

        await TxTestHelper.CleanupAsync(_context);

        _seed = await TxTestHelper.SeedAsync(_context);
        _invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 0);   // header only

        _repository = new SalesInvoiceDetailRepositoryEFSp(_context);
    }

    public async Task DisposeAsync()
    {
        await TxTestHelper.CleanupAsync(_context);
        await _context.DisposeAsync();
    }

    private SalesInvoiceDetailEntity Line(int? itemId = null, decimal qty = 2m, decimal rate = 150m)
        => TxTestHelper.NewInvoiceLine(_invoiceId, itemId ?? _seed.ItemAId, qty, rate);

    [Fact]
    public async Task AddAsync_ShouldInsertDetail()
    {
        var id = await _repository.AddAsync(Line(qty: 2, rate: 150));

        var saved = await _repository.GetByIdAsync(id);

        Assert.True(id > 0);
        Assert.NotNull(saved);
        Assert.Equal(_invoiceId, saved!.SalesInvoiceId);
        Assert.Equal(_seed.ItemAId, saved.ItemmasterId);
        Assert.Equal(2m, saved.Quantity);
        Assert.Equal(330m, saved.LineTotal);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenItemIsInactive()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(Line(itemId: _seed.InactiveItemId)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenQuantityIsZero()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(     // CK_SalesInvoiceDetail_Quantity
            () => _repository.AddAsync(Line(qty: 0)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenRateIsNegative()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(Line(rate: -1)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowNotFound_WhenInvoiceDoesNotExist()
    {
        var line = TxTestHelper.NewInvoiceLine(-1, _seed.ItemAId, 1m, 100m);

        await Assert.ThrowsAsync<NotFoundException>(() => _repository.AddAsync(line));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenInvoiceIsCancelled()
    {
        await new SalesInvoiceRepositoryEFSp(_context).CancelAsync(_invoiceId, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.AddAsync(Line()));
    }

    [Fact]
    public async Task GetBySalesInvoiceIdAsync_ShouldReturnDetailsOrderedById()
    {
        var first = await _repository.AddAsync(Line(_seed.ItemAId));
        var second = await _repository.AddAsync(Line(_seed.ItemBId));

        var details = (await _repository.GetBySalesInvoiceIdAsync(_invoiceId)).ToList();

        Assert.Equal(2, details.Count);
        Assert.Equal(first, details[0].Id);
        Assert.Equal(second, details[1].Id);
    }

    [Fact]
    public async Task GetBySalesInvoiceIdAsync_ShouldReturnEmpty_WhenInvoiceHasNoDetails()
    {
        Assert.Empty(await _repository.GetBySalesInvoiceIdAsync(_invoiceId));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        Assert.Null(await _repository.GetByIdAsync(-1));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDetail()
    {
        var id = await _repository.AddAsync(Line());

        var detail = (await _repository.GetByIdAsync(id))!;
        detail.ItemmasterId = _seed.ItemBId;
        detail.Quantity = 7m;
        detail.LineTotal = 999m;

        var updated = await _repository.UpdateAsync(detail);
        var saved = await _repository.GetByIdAsync(id);

        Assert.True(updated);
        Assert.Equal(_seed.ItemBId, saved!.ItemmasterId);
        Assert.Equal(7m, saved.Quantity);
        Assert.Equal(999m, saved.LineTotal);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenItemIsInactive()
    {
        var id = await _repository.AddAsync(Line());

        var detail = (await _repository.GetByIdAsync(id))!;
        detail.ItemmasterId = _seed.InactiveItemId;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.UpdateAsync(detail));
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenIdDoesNotExist()
    {
        var detail = Line();
        detail.Id = -1;

        Assert.False(await _repository.UpdateAsync(detail));
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteDetail()
    {
        var id = await _repository.AddAsync(Line());

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
    public async Task DeleteAsync_ShouldThrowBusinessRule_WhenInvoiceIsPosted()
    {
        var poSeed = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-001");
        await TxTestHelper.ReceiveStockAsync(_context, poSeed, 10);

        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, 1, number: "TXT-INV-POSTED");
        await new SalesInvoiceRepositoryEFSp(_context).PostAsync(id, TxTestHelper.TestUser);

        var detail = (await _repository.GetBySalesInvoiceIdAsync(id)).Single();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.DeleteAsync(detail.Id));
    }

    [Fact]
    public async Task DeleteBySalesInvoiceIdAsync_ShouldDeleteAllDetails()
    {
        await _repository.AddAsync(Line(_seed.ItemAId));
        await _repository.AddAsync(Line(_seed.ItemBId));

        var deleted = await _repository.DeleteBySalesInvoiceIdAsync(_invoiceId);

        Assert.True(deleted);
        Assert.Empty(await _repository.GetBySalesInvoiceIdAsync(_invoiceId));
    }

    [Fact]
    public async Task DeleteBySalesInvoiceIdAsync_ShouldReturnFalse_WhenInvoiceHasNoDetails()
    {
        Assert.False(await _repository.DeleteBySalesInvoiceIdAsync(_invoiceId));
    }
}
