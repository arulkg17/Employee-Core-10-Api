using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.BAL.Test;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _repository = new();
    private readonly Mock<IPurchaseOrderDetailRepository> _detailRepository = new();
    private readonly FakeTransactionRunner _transaction = new();
    private readonly PurchaseOrderServiceEFSp _service;

    public PurchaseOrderServiceTests()
    {
        _service = new PurchaseOrderServiceEFSp(
            _repository.Object,
            _detailRepository.Object,
            _transaction,
            TestMapper.Create());
    }

    private static PurchaseOrderDto NewDto(params (int Item, decimal Qty, decimal Rate, decimal Discount, decimal Tax)[] lines) => new()
    {
        PONumber = "PO-1",
        PODate = new DateTime(2026, 10, 6),
        VendorId = 1,
        Status = "Received",          // must be ignored
        Details = lines.Select(l => new PurchaseOrderDetailDto
        {
            ItemmasterId = l.Item,
            Quantity = l.Qty,
            Rate = l.Rate,
            DiscountAmount = l.Discount,
            TaxPercent = l.Tax,
            ReceivedQuantity = 7      // must be ignored
        }).ToList()
    };

    [Fact]
    public async Task AddAsync_ShouldForceDraft_ResetReceivedQuantity_AndCalculateTotals()
    {
        PurchaseOrderEntity? header = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<PurchaseOrderEntity>()))
            .Callback<PurchaseOrderEntity>(e => header = e)
            .ReturnsAsync(8);

        var lines = new List<PurchaseOrderDetailEntity>();
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<PurchaseOrderDetailEntity>()))
            .Callback<PurchaseOrderDetailEntity>(lines.Add)
            .ReturnsAsync(1);

        // same data as PurchaseOrderRequest.json
        var id = await _service.AddAsync(NewDto((1, 10, 100, 0, 10), (2, 5, 200, 10, 10)));

        Assert.Equal(8, id);
        Assert.Equal("Draft", header!.Status);
        Assert.Equal(1990m, header.SubTotal);
        Assert.Equal(199m, header.TaxAmount);
        Assert.Equal(2189m, header.TotalAmount);
        Assert.All(lines, l => Assert.Equal(8, l.PurchaseOrderId));
        Assert.All(lines, l => Assert.Equal(0m, l.ReceivedQuantity));
        Assert.Equal(1, _transaction.Calls);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenThereAreNoDetails()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto()));

        _repository.Verify(x => x.AddAsync(It.IsAny<PurchaseOrderEntity>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenALineIsInvalid()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AddAsync(NewDto((1, -1, 100, 0, 10))));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenPoDoesNotExist()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((PurchaseOrderEntity?)null);

        Assert.Null(await _service.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPoWithDetailsAndReceivedQuantity()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new PurchaseOrderEntity { Id = 1, PONumber = "PO-1" });
        _detailRepository
            .Setup(x => x.GetByPurchaseOrderIdAsync(1))
            .ReturnsAsync(new List<PurchaseOrderDetailEntity> { new() { Id = 2, Quantity = 10, ReceivedQuantity = 4 } });

        var result = await _service.GetByIdAsync(1);

        Assert.Single(result!.Details);
        Assert.Equal(4m, result.Details[0].ReceivedQuantity);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenPoDoesNotExist()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync((PurchaseOrderEntity?)null);

        var dto = NewDto((1, 1, 10, 0, 0));
        dto.Id = 5;

        Assert.False(await _service.UpdateAsync(dto));
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("PartiallyReceived")]
    [InlineData("Received")]
    [InlineData("Cancelled")]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenPoIsNotDraft(string status)
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new PurchaseOrderEntity { Id = 5, Status = status });

        var dto = NewDto((1, 1, 10, 0, 0));
        dto.Id = 5;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(dto));

        _repository.Verify(x => x.UpdateAsync(It.IsAny<PurchaseOrderEntity>()), Times.Never);
        _detailRepository.Verify(x => x.DeleteByPurchaseOrderIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceDetailsInsideATransaction()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new PurchaseOrderEntity { Id = 5, Status = "Draft" });
        _repository.Setup(x => x.UpdateAsync(It.IsAny<PurchaseOrderEntity>())).ReturnsAsync(true);

        var dto = NewDto((1, 1, 10, 0, 0), (2, 1, 10, 0, 0));
        dto.Id = 5;

        Assert.True(await _service.UpdateAsync(dto));

        _detailRepository.Verify(x => x.DeleteByPurchaseOrderIdAsync(5), Times.Once);
        _detailRepository.Verify(
            x => x.AddAsync(It.Is<PurchaseOrderDetailEntity>(d => d.PurchaseOrderId == 5)),
            Times.Exactly(2));
        Assert.Equal(1, _transaction.Calls);
    }

    [Fact]
    public async Task DeleteAsync_ShouldOnlySoftDeleteTheHeader()
    {
        _repository.Setup(x => x.DeleteAsync(3)).ReturnsAsync(true);

        Assert.True(await _service.DeleteAsync(3));

        _detailRepository.Verify(x => x.DeleteByPurchaseOrderIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.ApproveAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.ApproveAsync(3, "tester"));
    }

    [Fact]
    public async Task ApproveAsync_ShouldPropagateBusinessRuleException()
    {
        _repository
            .Setup(x => x.ApproveAsync(3, "tester"))
            .ThrowsAsync(new BusinessRuleException("Only Draft purchase orders can be approved."));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ApproveAsync(3, "tester"));
    }

    [Fact]
    public async Task CancelAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.CancelAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.CancelAsync(3, "tester"));
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldLoadDetailsForEachPo()
    {
        _repository
            .Setup(x => x.GetAllPagedAsync(null, null, "Approved", 1, 10))
            .ReturnsAsync(new PagedResultDto<PurchaseOrderEntity>
            {
                Data = new List<PurchaseOrderEntity> { new() { Id = 1 }, new() { Id = 2 } },
                TotalRecords = 2
            });

        _detailRepository
            .Setup(x => x.GetByPurchaseOrderIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<PurchaseOrderDetailEntity> { new() { Id = 1 } });

        var result = await _service.GetAllPagedAsync(null, null, "Approved", 1, 10);

        Assert.Equal(2, result.TotalRecords);
        Assert.All(result.Data, p => Assert.Single(p.Details));
    }
}
