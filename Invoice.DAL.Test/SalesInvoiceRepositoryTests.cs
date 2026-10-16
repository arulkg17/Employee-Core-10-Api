using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.Model;

namespace Invoice.DAL.Test;

[Collection(TransactionDbCollection.Name)]
public class SalesInvoiceRepositoryTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SeedData _seed = null!;
    private PoSeed _po = null!;
    private SalesInvoiceRepositoryEFSp _repository = null!;

    public async Task InitializeAsync()
    {
        _context = TxTestHelper.CreateDbContext();

        await TxTestHelper.CleanupAsync(_context);

        _seed = await TxTestHelper.SeedAsync(_context);
        _po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-001");

        _repository = new SalesInvoiceRepositoryEFSp(_context);
    }

    public async Task DisposeAsync()
    {
        await TxTestHelper.CleanupAsync(_context);
        await _context.DisposeAsync();
    }

    private SalesInvoiceEntity NewInvoice(string? number = "TXT-INV-001", int? customerId = null) => new()
    {
        InvoiceNumber = number ?? string.Empty,
        InvoiceDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30),
        CustomerId = customerId ?? _seed.CustomerId,
        Notes = "TX invoice",
        SubTotal = 100,
        TaxAmount = 10,
        TotalAmount = 110,
        CreatedBy = TxTestHelper.TestUser
    };

    // ------------------------------------------------------------
    // AddAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task AddAsync_ShouldInsertDraftInvoice()
    {
        var id = await _repository.AddAsync(NewInvoice());

        var saved = await _repository.GetByIdAsync(id);

        Assert.True(id > 0);
        Assert.NotNull(saved);
        Assert.Equal("TXT-INV-001", saved!.InvoiceNumber);
        Assert.Equal("Draft", saved.Status);
        Assert.Equal(_seed.CustomerId, saved.CustomerId);
        Assert.Equal(110m, saved.TotalAmount);
        Assert.NotNull(saved.DueDate);
    }

    [Fact]
    public async Task AddAsync_ShouldGenerateNumber_WhenNumberIsEmpty()
    {
        var id = await _repository.AddAsync(NewInvoice(number: null));

        var saved = await _repository.GetByIdAsync(id);

        Assert.StartsWith("INV-", saved!.InvoiceNumber);
    }

    [Fact]
    public async Task AddAsync_ShouldAllowMissingDueDate()
    {
        var invoice = NewInvoice();
        invoice.DueDate = null;

        var id = await _repository.AddAsync(invoice);

        Assert.Null((await _repository.GetByIdAsync(id))!.DueDate);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenCustomerIsInactive()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(NewInvoice(customerId: _seed.InactiveCustomerId)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenCustomerDoesNotExist()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(NewInvoice(customerId: -1)));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenDueDateIsBeforeInvoiceDate()
    {
        var invoice = NewInvoice();
        invoice.DueDate = invoice.InvoiceDate.AddDays(-1);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.AddAsync(invoice));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenInvoiceNumberIsDuplicate()
    {
        await _repository.AddAsync(NewInvoice("TXT-INV-DUP"));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.AddAsync(NewInvoice("TXT-INV-DUP")));
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
    public async Task GetAllAsync_ShouldReturnOnlyNotDeletedInvoices()
    {
        var keep = await _repository.AddAsync(NewInvoice("TXT-INV-KEEP"));
        var remove = await _repository.AddAsync(NewInvoice("TXT-INV-REMOVE"));

        await _repository.DeleteAsync(remove, TxTestHelper.TestUser);

        var all = (await _repository.GetAllAsync())
            .Where(x => x.InvoiceNumber.StartsWith("TXT-INV-"))
            .ToList();

        Assert.Single(all);
        Assert.Equal(keep, all[0].Id);
    }

    // ------------------------------------------------------------
    // UpdateAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateAsync_ShouldUpdateDraftInvoice()
    {
        var id = await _repository.AddAsync(NewInvoice());

        var updated = await _repository.UpdateAsync(new SalesInvoiceEntity
        {
            Id = id,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(10),
            CustomerId = _seed.CustomerId,
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
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenInvoiceDoesNotExist()
    {
        var invoice = NewInvoice();
        invoice.Id = -1;

        Assert.False(await _repository.UpdateAsync(invoice));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenInvoiceIsPosted()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, 10);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, 2);
        await _repository.PostAsync(id, TxTestHelper.TestUser);

        var invoice = NewInvoice();
        invoice.Id = id;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.UpdateAsync(invoice));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenCustomerIsInactive()
    {
        var id = await _repository.AddAsync(NewInvoice());

        var invoice = NewInvoice(customerId: _seed.InactiveCustomerId);
        invoice.Id = id;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.UpdateAsync(invoice));
    }

    // ------------------------------------------------------------
    // DeleteAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteDraftInvoice()
    {
        var id = await _repository.AddAsync(NewInvoice());

        var deleted = await _repository.DeleteAsync(id, TxTestHelper.TestUser);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenInvoiceDoesNotExist()
    {
        Assert.False(await _repository.DeleteAsync(-1));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowBusinessRule_WhenInvoiceIsPosted()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, 10);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, 2);
        await _repository.PostAsync(id, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _repository.DeleteAsync(id));
    }

    // ------------------------------------------------------------
    // Paged
    // ------------------------------------------------------------
    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnRequestedPageAndTotal()
    {
        for (var i = 1; i <= 5; i++)
            await _repository.AddAsync(NewInvoice($"TXT-INV-P{i:00}"));

        var page = await _repository.GetAllPagedAsync(null, null, null, 1, 2);

        Assert.Equal(5, page.TotalRecords);
        Assert.Equal(2, page.Data.Count());
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByNumber()
    {
        await _repository.AddAsync(NewInvoice("TXT-INV-AAA"));
        await _repository.AddAsync(NewInvoice("TXT-INV-BBB"));

        var page = await _repository.GetAllPagedAsync("BBB", null, null, 1, 10);

        Assert.Equal(1, page.TotalRecords);
        Assert.Equal("TXT-INV-BBB", page.Data.Single().InvoiceNumber);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByCustomer()
    {
        await _repository.AddAsync(NewInvoice("TXT-INV-C1"));

        var matching = await _repository.GetAllPagedAsync(null, _seed.CustomerId, null, 1, 10);
        var other = await _repository.GetAllPagedAsync(null, _seed.InactiveCustomerId, null, 1, 10);

        Assert.Equal(1, matching.TotalRecords);
        Assert.Equal(0, other.TotalRecords);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByStatus()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, 10);

        await _repository.AddAsync(NewInvoice("TXT-INV-DRAFT"));
        var postedId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, 1, number: "TXT-INV-POSTED");
        await _repository.PostAsync(postedId, TxTestHelper.TestUser);

        var posted = await _repository.GetAllPagedAsync(null, null, "Posted", 1, 10);
        var draft = await _repository.GetAllPagedAsync(null, null, "Draft", 1, 10);

        Assert.Equal(1, posted.TotalRecords);
        Assert.Equal("TXT-INV-POSTED", posted.Data.Single().InvoiceNumber);
        Assert.Equal(1, draft.TotalRecords);
    }

    // ------------------------------------------------------------
    // PostAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task PostAsync_ShouldPostInvoice_AndReduceStock()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10, qtyB: 5);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4, qtyB: 1);

        var posted = await _repository.PostAsync(id, TxTestHelper.TestUser);

        var invoice = await _repository.GetByIdAsync(id);

        Assert.True(posted);
        Assert.Equal("Posted", invoice!.Status);
        Assert.Equal(6m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
        Assert.Equal(4m, await TxTestHelper.OnHandAsync(_context, _seed.ItemBId));
    }

    [Fact]
    public async Task PostAsync_ShouldAllowSellingAllStock()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 10);

        await _repository.PostAsync(id, TxTestHelper.TestUser);

        Assert.Equal(0m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenStockIsInsufficient()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 3);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));

        Assert.Contains("Insufficient stock", ex.Message);
        Assert.Contains("TXA01", ex.Message);
        Assert.Equal("Draft", (await _repository.GetByIdAsync(id))!.Status);
        Assert.Equal(3m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenNothingWasEverReceived()
    {
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 1);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldSumQuantitiesOfRepeatedItemLines()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 5);

        var id = await _repository.AddAsync(NewInvoice());
        var details = new SalesInvoiceDetailRepositoryEFSp(_context);

        await details.AddAsync(TxTestHelper.NewInvoiceLine(id, _seed.ItemAId, 3m, 150m));
        await details.AddAsync(TxTestHelper.NewInvoiceLine(id, _seed.ItemAId, 3m, 150m));   // 3 + 3 = 6 > 5

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenInvoiceHasNoLines()
    {
        var id = await _repository.AddAsync(NewInvoice());

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowBusinessRule_WhenInvoiceIsAlreadyPosted()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 1);

        await _repository.PostAsync(id, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldThrowNotFound_WhenInvoiceDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.PostAsync(-1, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task PostAsync_ShouldNotLetTwoInvoicesSellTheSameStock()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 5);

        var first = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, 4, number: "TXT-INV-1");
        var second = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, 4, number: "TXT-INV-2");

        await _repository.PostAsync(first, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.PostAsync(second, TxTestHelper.TestUser));
    }

    // ------------------------------------------------------------
    // CancelAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task CancelAsync_ShouldCancelDraftInvoice()
    {
        var id = await _repository.AddAsync(NewInvoice());

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        Assert.Equal("Cancelled", (await _repository.GetByIdAsync(id))!.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnStock_WhenInvoiceWasPosted()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);
        var id = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4);
        await _repository.PostAsync(id, TxTestHelper.TestUser);

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        Assert.Equal(10m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowBusinessRule_WhenInvoiceIsAlreadyCancelled()
    {
        var id = await _repository.AddAsync(NewInvoice());

        await _repository.CancelAsync(id, TxTestHelper.TestUser);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _repository.CancelAsync(id, TxTestHelper.TestUser));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowNotFound_WhenInvoiceDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _repository.CancelAsync(-1, TxTestHelper.TestUser));
    }
}
