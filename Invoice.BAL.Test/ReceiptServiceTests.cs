using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.BAL.Test;

public class ReceiptServiceTests
{
    private readonly Mock<IReceiptRepository> _repository = new();
    private readonly Mock<IReceiptDetailRepository> _detailRepository = new();
    private readonly Mock<IPurchaseOrderRepository> _poRepository = new();
    private readonly Mock<IPurchaseOrderDetailRepository> _poDetailRepository = new();
    private readonly FakeTransactionRunner _transaction = new();
    private readonly ReceiptServiceEFSp _service;

    public ReceiptServiceTests()
    {
        _service = new ReceiptServiceEFSp(
            _repository.Object,
            _detailRepository.Object,
            _poRepository.Object,
            _poDetailRepository.Object,
            _transaction,
            TestMapper.Create());
    }

    // PO 10: Approved, vendor 7. Line 100 = item 1, qty 10 x 100, 10% tax, discount 50
    //                            Line 101 = item 2, qty 5 x 200, 10% tax, 2 already received
    private void SetupPurchaseOrder(string status = "Approved")
    {
        _poRepository
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(new PurchaseOrderEntity { Id = 10, VendorId = 7, Status = status });

        _poDetailRepository
            .Setup(x => x.GetByPurchaseOrderIdAsync(10))
            .ReturnsAsync(new List<PurchaseOrderDetailEntity>
            {
                new() { Id = 100, PurchaseOrderId = 10, ItemmasterId = 1, Quantity = 10, Rate = 100, DiscountAmount = 50, TaxPercent = 10, ReceivedQuantity = 0 },
                new() { Id = 101, PurchaseOrderId = 10, ItemmasterId = 2, Quantity = 5, Rate = 200, DiscountAmount = 0, TaxPercent = 10, ReceivedQuantity = 2 }
            });
    }

    private static ReceiptDto NewDto(params (int PoLineId, decimal Qty)[] lines) => new()
    {
        ReceiptDate = new DateTime(2026, 10, 6),
        PurchaseOrderId = 10,
        CreatedBy = "tester",
        Details = lines.Select(l => new ReceiptDetailDto
        {
            PurchaseOrderDetailId = l.PoLineId,
            ReceivedQuantity = l.Qty
        }).ToList()
    };

    // ------------------------------------------------------------
    // AddAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task AddAsync_ShouldSaveHeaderAndDetails_AndReturnId()
    {
        SetupPurchaseOrder();
        _repository.Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>())).ReturnsAsync(55);

        var saved = new List<ReceiptDetailEntity>();
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDetailEntity>()))
            .Callback<ReceiptDetailEntity>(saved.Add)
            .ReturnsAsync(1);

        var id = await _service.AddAsync(NewDto((100, 4), (101, 3)));

        Assert.Equal(55, id);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, d => Assert.Equal(55, d.ReceiptId));
        _repository.Verify(x => x.AddAsync(It.IsAny<ReceiptEntity>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldRunInsideOneTransaction()
    {
        SetupPurchaseOrder();
        _repository.Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>())).ReturnsAsync(1);

        await _service.AddAsync(NewDto((100, 1)));

        Assert.Equal(1, _transaction.Calls);
    }

    [Fact]
    public async Task AddAsync_ShouldTakeVendorFromPo_AndForceDraftStatus()
    {
        SetupPurchaseOrder();

        ReceiptEntity? captured = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>()))
            .Callback<ReceiptEntity>(e => captured = e)
            .ReturnsAsync(1);

        var dto = NewDto((100, 1));
        dto.VendorId = 999;              // client value must be ignored
        dto.Status = "Posted";           // client value must be ignored

        await _service.AddAsync(dto);

        Assert.NotNull(captured);
        Assert.Equal(7, captured!.VendorId);
        Assert.Equal("Draft", captured.Status);
        Assert.Equal("tester", captured.CreatedBy);
    }

    [Fact]
    public async Task AddAsync_ShouldTakeItemRateAndTaxFromPoLine_NotFromClient()
    {
        SetupPurchaseOrder();
        _repository.Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>())).ReturnsAsync(1);

        ReceiptDetailEntity? captured = null;
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDetailEntity>()))
            .Callback<ReceiptDetailEntity>(d => captured = d)
            .ReturnsAsync(1);

        var dto = NewDto((100, 4));
        dto.Details[0].ItemmasterId = 999;
        dto.Details[0].Rate = 1;
        dto.Details[0].TaxPercent = 99;

        await _service.AddAsync(dto);

        Assert.Equal(1, captured!.ItemmasterId);
        Assert.Equal(100m, captured.Rate);
        Assert.Equal(10m, captured.TaxPercent);
        Assert.Equal(100, captured.PurchaseOrderDetailId);
    }

    [Fact]
    public async Task AddAsync_ShouldProRateDiscountAndCalculateLineAmounts()
    {
        SetupPurchaseOrder();
        _repository.Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>())).ReturnsAsync(1);

        ReceiptDetailEntity? captured = null;
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDetailEntity>()))
            .Callback<ReceiptDetailEntity>(d => captured = d)
            .ReturnsAsync(1);

        await _service.AddAsync(NewDto((100, 4)));

        // discount 50 * 4 / 10 = 20 ; gross 400 ; taxable 380 ; tax 38 ; total 418
        Assert.Equal(20m, captured!.DiscountAmount);
        Assert.Equal(38m, captured.TaxAmount);
        Assert.Equal(418m, captured.LineTotal);
    }

    [Fact]
    public async Task AddAsync_ShouldCalculateHeaderTotals()
    {
        SetupPurchaseOrder();

        ReceiptEntity? captured = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>()))
            .Callback<ReceiptEntity>(e => captured = e)
            .ReturnsAsync(1);

        await _service.AddAsync(NewDto((100, 4), (101, 3)));

        // line 100: taxable 380, tax 38 ; line 101: 3 x 200 = 600, tax 60
        Assert.Equal(980m, captured!.SubTotal);
        Assert.Equal(98m, captured.TaxAmount);
        Assert.Equal(1078m, captured.TotalAmount);
    }

    [Fact]
    public async Task AddAsync_ShouldAcceptPartiallyReceivedPo()
    {
        SetupPurchaseOrder("PartiallyReceived");
        _repository.Setup(x => x.AddAsync(It.IsAny<ReceiptEntity>())).ReturnsAsync(9);

        Assert.Equal(9, await _service.AddAsync(NewDto((101, 3))));    // exactly the outstanding 3
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenThereAreNoDetails()
    {
        SetupPurchaseOrder();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto()));

        _repository.Verify(x => x.AddAsync(It.IsAny<ReceiptEntity>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowNotFound_WhenPoDoesNotExist()
    {
        _poRepository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync((PurchaseOrderEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.AddAsync(NewDto((100, 1))));
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Received")]
    [InlineData("Cancelled")]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenPoStatusDoesNotAllowReceiving(string status)
    {
        SetupPurchaseOrder(status);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto((100, 1))));

        _repository.Verify(x => x.AddAsync(It.IsAny<ReceiptEntity>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenQuantityExceedsOutstanding()
    {
        SetupPurchaseOrder();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AddAsync(NewDto((101, 4))));          // outstanding is 5 - 2 = 3

        Assert.Contains("exceeds the outstanding quantity", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenQuantityIsNotPositive(int quantity)
    {
        SetupPurchaseOrder();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto((100, quantity))));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenPoLineDoesNotBelongToPo()
    {
        SetupPurchaseOrder();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto((999, 1))));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenSamePoLineAppearsTwice()
    {
        SetupPurchaseOrder();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AddAsync(NewDto((100, 1), (100, 2))));
    }

    // ------------------------------------------------------------
    // Read
    // ------------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenReceiptDoesNotExist()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((ReceiptEntity?)null);

        Assert.Null(await _service.GetByIdAsync(1));

        _detailRepository.Verify(x => x.GetByReceiptIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReceiptWithDetails()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new ReceiptEntity { Id = 1, ReceiptNumber = "GRN-1" });
        _detailRepository
            .Setup(x => x.GetByReceiptIdAsync(1))
            .ReturnsAsync(new List<ReceiptDetailEntity> { new() { Id = 5, ReceiptId = 1, ReceivedQuantity = 3 } });

        var result = await _service.GetByIdAsync(1);

        Assert.Equal("GRN-1", result!.ReceiptNumber);
        Assert.Single(result.Details);
        Assert.Equal(3m, result.Details[0].ReceivedQuantity);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadDetailsForEveryReceipt()
    {
        _repository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ReceiptEntity>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        });

        _detailRepository
            .Setup(x => x.GetByReceiptIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ReceiptDetailEntity> { new() { Id = 1 } });

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Single(r.Details));
        _detailRepository.Verify(x => x.GetByReceiptIdAsync(It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnMappedPageWithTotal()
    {
        _repository
            .Setup(x => x.GetAllPagedAsync("GRN", 10, null, "Draft", 2, 5))
            .ReturnsAsync(new PagedResultDto<ReceiptEntity>
            {
                Data = new List<ReceiptEntity> { new() { Id = 1 } },
                TotalRecords = 11
            });

        _detailRepository
            .Setup(x => x.GetByReceiptIdAsync(1))
            .ReturnsAsync(new List<ReceiptDetailEntity> { new() { Id = 1 } });

        var result = await _service.GetAllPagedAsync("GRN", 10, null, "Draft", 2, 5);

        Assert.Equal(11, result.TotalRecords);
        Assert.Single(result.Data);
        Assert.Single(result.Data.First().Details);
    }

    // ------------------------------------------------------------
    // UpdateAsync
    // ------------------------------------------------------------
    private void SetupExistingReceipt(string status = "Draft")
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new ReceiptEntity
        {
            Id = 5,
            ReceiptNumber = "GRN-000005",
            PurchaseOrderId = 10,
            VendorId = 7,
            Status = status
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenReceiptDoesNotExist()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync((ReceiptEntity?)null);

        var dto = NewDto((100, 1));
        dto.Id = 5;

        Assert.False(await _service.UpdateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenReceiptIsNotDraft()
    {
        SetupExistingReceipt("Posted");

        var dto = NewDto((100, 1));
        dto.Id = 5;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(dto));

        _repository.Verify(x => x.UpdateAsync(It.IsAny<ReceiptEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceDetails_AndKeepPoVendorAndNumber()
    {
        SetupExistingReceipt();
        SetupPurchaseOrder();

        ReceiptEntity? captured = null;
        _repository
            .Setup(x => x.UpdateAsync(It.IsAny<ReceiptEntity>()))
            .Callback<ReceiptEntity>(e => captured = e)
            .ReturnsAsync(true);

        var added = new List<ReceiptDetailEntity>();
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDetailEntity>()))
            .Callback<ReceiptDetailEntity>(added.Add)
            .ReturnsAsync(1);

        var dto = NewDto((100, 2), (101, 1));
        dto.Id = 5;
        dto.ReceiptNumber = "HACKED";
        dto.PurchaseOrderId = 99;
        dto.UpdatedBy = "editor";

        var result = await _service.UpdateAsync(dto);

        Assert.True(result);
        Assert.Equal("GRN-000005", captured!.ReceiptNumber);
        Assert.Equal(10, captured.PurchaseOrderId);
        Assert.Equal(7, captured.VendorId);
        Assert.Equal("editor", captured.UpdatedBy);
        Assert.Equal(2, added.Count);
        Assert.All(added, d => Assert.Equal(5, d.ReceiptId));
        _detailRepository.Verify(x => x.DeleteByReceiptIdAsync(5), Times.Once);
        Assert.Equal(1, _transaction.Calls);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotTouchDetails_WhenHeaderUpdateAffectsNoRows()
    {
        SetupExistingReceipt();
        SetupPurchaseOrder();
        _repository.Setup(x => x.UpdateAsync(It.IsAny<ReceiptEntity>())).ReturnsAsync(false);

        var dto = NewDto((100, 1));
        dto.Id = 5;

        Assert.False(await _service.UpdateAsync(dto));

        _detailRepository.Verify(x => x.DeleteByReceiptIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenThereAreNoDetails()
    {
        SetupExistingReceipt();

        var dto = NewDto();
        dto.Id = 5;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(dto));
    }

    // ------------------------------------------------------------
    // Delete / Post / Cancel
    // ------------------------------------------------------------
    [Fact]
    public async Task DeleteAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.DeleteAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.DeleteAsync(3, "tester"));

        _repository.Verify(x => x.DeleteAsync(3, "tester"), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.PostAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.PostAsync(3, "tester"));
    }

    [Fact]
    public async Task PostAsync_ShouldPropagateBusinessRuleException()
    {
        _repository
            .Setup(x => x.PostAsync(3, "tester"))
            .ThrowsAsync(new BusinessRuleException("Only Draft receipts can be posted."));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.PostAsync(3, "tester"));
    }

    [Fact]
    public async Task CancelAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.CancelAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.CancelAsync(3, "tester"));
    }

    [Fact]
    public async Task CancelAsync_ShouldPropagateNotFoundException()
    {
        _repository
            .Setup(x => x.CancelAsync(3, "tester"))
            .ThrowsAsync(new NotFoundException("Receipt not found."));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelAsync(3, "tester"));
    }
}
