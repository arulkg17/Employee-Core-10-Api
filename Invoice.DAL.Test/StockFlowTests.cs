using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Model;

namespace Invoice.DAL.Test;

/// <summary>End-to-end stock scenarios: Receipt (in) -> Sales Invoice (out) -> cancellations.</summary>
[Collection(TransactionDbCollection.Name)]
public class StockFlowTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SeedData _seed = null!;
    private PoSeed _po = null!;
    private ReceiptRepositoryEFSp _receipts = null!;
    private SalesInvoiceRepositoryEFSp _invoices = null!;
    private StockRepositoryEFSp _stock = null!;

    public async Task InitializeAsync()
    {
        _context = TxTestHelper.CreateDbContext();

        await TxTestHelper.CleanupAsync(_context);

        _seed = await TxTestHelper.SeedAsync(_context);
        _po = await TxTestHelper.CreatePoAsync(_context, _seed, "TXT-PO-001");

        _receipts = new ReceiptRepositoryEFSp(_context);
        _invoices = new SalesInvoiceRepositoryEFSp(_context);
        _stock = new StockRepositoryEFSp(_context);
    }

    public async Task DisposeAsync()
    {
        await TxTestHelper.CleanupAsync(_context);
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByItemmasterIdAsync_ShouldReturnZeroStock_WhenNoMovements()
    {
        var row = await _stock.GetByItemmasterIdAsync(_seed.ItemAId);

        Assert.NotNull(row);
        Assert.Equal("TXA01", row!.ItemCode);
        Assert.Equal(0m, row.ReceivedQuantity);
        Assert.Equal(0m, row.SoldQuantity);
        Assert.Equal(0m, row.OnHandQuantity);
    }

    [Fact]
    public async Task GetByItemmasterIdAsync_ShouldReturnNull_WhenItemDoesNotExist()
    {
        Assert.Null(await _stock.GetByItemmasterIdAsync(-1));
    }

    [Fact]
    public async Task GetAllAsync_ShouldContainTestItems()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 7);

        var all = (await _stock.GetAllAsync()).ToList();

        Assert.Contains(all, x => x.ItemmasterId == _seed.ItemAId && x.OnHandQuantity == 7m);
        Assert.Contains(all, x => x.ItemmasterId == _seed.ItemBId && x.OnHandQuantity == 0m);
    }

    [Fact]
    public async Task DraftReceiptsAndDraftInvoices_ShouldNotChangeStock()
    {
        await TxTestHelper.CreateDraftReceiptAsync(_context, _po, qtyA: 5);
        await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 5);

        Assert.Equal(0m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task ReceiveThenSell_ShouldShowReceivedSoldAndOnHand()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);

        var invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4);
        await _invoices.PostAsync(invoiceId, TxTestHelper.TestUser);

        var row = await _stock.GetByItemmasterIdAsync(_seed.ItemAId);

        Assert.Equal(10m, row!.ReceivedQuantity);
        Assert.Equal(4m, row.SoldQuantity);
        Assert.Equal(6m, row.OnHandQuantity);
    }

    [Fact]
    public async Task CancelledInvoice_ShouldNotAffectStock()
    {
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);

        var invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4);
        await _invoices.PostAsync(invoiceId, TxTestHelper.TestUser);
        await _invoices.CancelAsync(invoiceId, TxTestHelper.TestUser);

        Assert.Equal(10m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task CancelReceipt_ShouldThrowBusinessRule_WhenItsStockWasAlreadySold()
    {
        var receiptId = await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);

        var invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4);
        await _invoices.PostAsync(invoiceId, TxTestHelper.TestUser);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _receipts.CancelAsync(receiptId, TxTestHelper.TestUser));

        Assert.Contains("already been sold", ex.Message);
        Assert.Equal("Posted", (await _receipts.GetByIdAsync(receiptId))!.Status);   // rolled back
        Assert.Equal(6m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task CancelReceipt_ShouldSucceed_AfterTheInvoiceIsCancelled()
    {
        var receiptId = await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10);

        var invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 4);
        await _invoices.PostAsync(invoiceId, TxTestHelper.TestUser);
        await _invoices.CancelAsync(invoiceId, TxTestHelper.TestUser);

        await _receipts.CancelAsync(receiptId, TxTestHelper.TestUser);

        Assert.Equal(0m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task CancelReceipt_ShouldSucceed_WhenOtherReceiptsStillCoverTheSoldQuantity()
    {
        var first = await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 4, number: "TXT-GRN-1");
        await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 6, number: "TXT-GRN-2");

        var invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 5);
        await _invoices.PostAsync(invoiceId, TxTestHelper.TestUser);

        // on hand = 10 - 5 = 5, cancelling the receipt of 4 leaves 1 >= 0
        await _receipts.CancelAsync(first, TxTestHelper.TestUser);

        Assert.Equal(1m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
    }

    [Fact]
    public async Task FullCycle_ShouldKeepPoAndStockConsistent()
    {
        var receiptId = await TxTestHelper.ReceiveStockAsync(_context, _po, qtyA: 10, qtyB: 5);

        var invoiceId = await TxTestHelper.CreateDraftInvoiceAsync(_context, _seed, qtyA: 10, qtyB: 5);
        await _invoices.PostAsync(invoiceId, TxTestHelper.TestUser);

        Assert.Equal(0m, await TxTestHelper.OnHandAsync(_context, _seed.ItemAId));
        Assert.Equal(0m, await TxTestHelper.OnHandAsync(_context, _seed.ItemBId));
        Assert.Equal("Received", (await new PurchaseOrderRepositoryEFSp(_context).GetByIdAsync(_po.PurchaseOrderId))!.Status);

        await _invoices.CancelAsync(invoiceId, TxTestHelper.TestUser);
        await _receipts.CancelAsync(receiptId, TxTestHelper.TestUser);

        Assert.Equal("Approved", (await new PurchaseOrderRepositoryEFSp(_context).GetByIdAsync(_po.PurchaseOrderId))!.Status);
    }

    [Fact]
    public async Task TransactionRunner_ShouldRollbackHeaderAndDetails_WhenALaterCallFails()
    {
        var runner = new EfTransactionRunner(_context);
        var details = new ReceiptDetailRepositoryEFSp(_context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => runner.ExecuteAsync(async () =>
        {
            var id = await _receipts.AddAsync(new Invoice.Data.Entities.ReceiptEntity
            {
                ReceiptNumber = "TXT-GRN-ROLLBACK",
                ReceiptDate = DateTime.Today,
                PurchaseOrderId = _po.PurchaseOrderId,
                CreatedBy = TxTestHelper.TestUser
            });

            // 99 > outstanding quantity (10) -> business rule error -> whole transaction must roll back
            await details.AddAsync(TxTestHelper.NewReceiptLine(id, _po.LineAId, _po.ItemAId, 99m, 100m));

            return id;
        }));

        var page = await _receipts.GetAllPagedAsync("ROLLBACK", null, null, null, 1, 10);

        Assert.Equal(0, page.TotalRecords);
    }

    [Fact]
    public async Task TransactionRunner_ShouldCommit_WhenAllCallsSucceed()
    {
        var runner = new EfTransactionRunner(_context);
        var details = new ReceiptDetailRepositoryEFSp(_context);

        var id = await runner.ExecuteAsync(async () =>
        {
            var receiptId = await _receipts.AddAsync(new Invoice.Data.Entities.ReceiptEntity
            {
                ReceiptNumber = "TXT-GRN-COMMIT",
                ReceiptDate = DateTime.Today,
                PurchaseOrderId = _po.PurchaseOrderId,
                CreatedBy = TxTestHelper.TestUser
            });

            await details.AddAsync(TxTestHelper.NewReceiptLine(receiptId, _po.LineAId, _po.ItemAId, 2m, 100m));

            return receiptId;
        });

        Assert.NotNull(await _receipts.GetByIdAsync(id));
        Assert.Single(await details.GetByReceiptIdAsync(id));
    }
}
