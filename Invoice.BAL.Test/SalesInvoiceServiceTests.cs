using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.BAL.Test;

public class SalesInvoiceServiceTests
{
    private readonly Mock<ISalesInvoiceRepository> _repository = new();
    private readonly Mock<ISalesInvoiceDetailRepository> _detailRepository = new();
    private readonly FakeTransactionRunner _transaction = new();
    private readonly SalesInvoiceServiceEFSp _service;

    public SalesInvoiceServiceTests()
    {
        _service = new SalesInvoiceServiceEFSp(
            _repository.Object,
            _detailRepository.Object,
            _transaction,
            TestMapper.Create());
    }

    private static SalesInvoiceDto NewDto(params (int Item, decimal Qty, decimal Rate, decimal Discount, decimal Tax)[] lines) => new()
    {
        InvoiceDate = new DateTime(2026, 10, 6),
        DueDate = new DateTime(2026, 11, 5),
        CustomerId = 3,
        CreatedBy = "tester",
        Details = lines.Select(l => new SalesInvoiceDetailDto
        {
            ItemmasterId = l.Item,
            Quantity = l.Qty,
            Rate = l.Rate,
            DiscountAmount = l.Discount,
            TaxPercent = l.Tax
        }).ToList()
    };

    private static SalesInvoiceDto OneLine() => NewDto((1, 2, 150, 0, 10));

    // ------------------------------------------------------------
    // AddAsync
    // ------------------------------------------------------------
    [Fact]
    public async Task AddAsync_ShouldSaveHeaderAndDetails_AndReturnId()
    {
        _repository.Setup(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>())).ReturnsAsync(42);

        var saved = new List<SalesInvoiceDetailEntity>();
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<SalesInvoiceDetailEntity>()))
            .Callback<SalesInvoiceDetailEntity>(saved.Add)
            .ReturnsAsync(1);

        var id = await _service.AddAsync(NewDto((1, 2, 150, 0, 10), (2, 1, 250, 0, 10)));

        Assert.Equal(42, id);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, d => Assert.Equal(42, d.SalesInvoiceId));
    }

    [Fact]
    public async Task AddAsync_ShouldRunInsideOneTransaction()
    {
        _repository.Setup(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>())).ReturnsAsync(1);

        await _service.AddAsync(OneLine());

        Assert.Equal(1, _transaction.Calls);
    }

    [Fact]
    public async Task AddAsync_ShouldForceDraftStatus_AndKeepCreatedBy()
    {
        SalesInvoiceEntity? captured = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>()))
            .Callback<SalesInvoiceEntity>(e => captured = e)
            .ReturnsAsync(1);

        var dto = OneLine();
        dto.Status = "Posted";

        await _service.AddAsync(dto);

        Assert.Equal("Draft", captured!.Status);
        Assert.Equal("tester", captured.CreatedBy);
    }

    [Fact]
    public async Task AddAsync_ShouldCalculateLineAndHeaderTotals_IgnoringClientValues()
    {
        SalesInvoiceEntity? header = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>()))
            .Callback<SalesInvoiceEntity>(e => header = e)
            .ReturnsAsync(1);

        var lines = new List<SalesInvoiceDetailEntity>();
        _detailRepository
            .Setup(x => x.AddAsync(It.IsAny<SalesInvoiceDetailEntity>()))
            .Callback<SalesInvoiceDetailEntity>(lines.Add)
            .ReturnsAsync(1);

        var dto = NewDto((1, 5, 200, 10, 10), (2, 2, 50, 0, 0));
        dto.SubTotal = 1;                     // ignored
        dto.TotalAmount = 1;                  // ignored
        dto.Details[0].LineTotal = 1;         // ignored

        await _service.AddAsync(dto);

        // line 1: 1000 - 10 = 990, tax 99, total 1089 ; line 2: 100, tax 0, total 100
        Assert.Equal(99m, lines[0].TaxAmount);
        Assert.Equal(1089m, lines[0].LineTotal);
        Assert.Equal(100m, lines[1].LineTotal);
        Assert.Equal(1090m, header!.SubTotal);
        Assert.Equal(99m, header.TaxAmount);
        Assert.Equal(1189m, header.TotalAmount);
    }

    [Fact]
    public async Task AddAsync_ShouldAllowTheSameItemOnTwoLines()
    {
        _repository.Setup(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>())).ReturnsAsync(1);

        await _service.AddAsync(NewDto((1, 1, 100, 0, 0), (1, 2, 100, 0, 0)));

        _detailRepository.Verify(x => x.AddAsync(It.IsAny<SalesInvoiceDetailEntity>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenThereAreNoDetails()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto()));

        _repository.Verify(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenCustomerIsMissing()
    {
        var dto = OneLine();
        dto.CustomerId = 0;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenDueDateIsBeforeInvoiceDate()
    {
        var dto = OneLine();
        dto.DueDate = dto.InvoiceDate.AddDays(-1);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_ShouldAcceptMissingDueDate()
    {
        _repository.Setup(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>())).ReturnsAsync(1);

        var dto = OneLine();
        dto.DueDate = null;

        Assert.Equal(1, await _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowBusinessRule_WhenItemIsMissing()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AddAsync(NewDto((0, 1, 10, 0, 0))));
    }

    [Theory]
    [InlineData(0, 10, 0, 0)]       // quantity
    [InlineData(-2, 10, 0, 0)]      // quantity
    [InlineData(1, -5, 0, 0)]       // rate
    [InlineData(1, 10, -1, 0)]      // discount
    [InlineData(1, 10, 11, 0)]      // discount above line amount
    [InlineData(1, 10, 0, 150)]     // tax percent
    public async Task AddAsync_ShouldThrowBusinessRule_ForInvalidLine(
        decimal qty, decimal rate, decimal discount, decimal tax)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AddAsync(NewDto((1, qty, rate, discount, tax))));

        _repository.Verify(x => x.AddAsync(It.IsAny<SalesInvoiceEntity>()), Times.Never);
    }

    // ------------------------------------------------------------
    // Read
    // ------------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenInvoiceDoesNotExist()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((SalesInvoiceEntity?)null);

        Assert.Null(await _service.GetByIdAsync(1));

        _detailRepository.Verify(x => x.GetBySalesInvoiceIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnInvoiceWithDetails()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new SalesInvoiceEntity { Id = 1, InvoiceNumber = "INV-1" });
        _detailRepository
            .Setup(x => x.GetBySalesInvoiceIdAsync(1))
            .ReturnsAsync(new List<SalesInvoiceDetailEntity> { new() { Id = 9, SalesInvoiceId = 1, Quantity = 2 } });

        var result = await _service.GetByIdAsync(1);

        Assert.Equal("INV-1", result!.InvoiceNumber);
        Assert.Single(result.Details);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadDetailsForEveryInvoice()
    {
        _repository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<SalesInvoiceEntity>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        });

        _detailRepository
            .Setup(x => x.GetBySalesInvoiceIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SalesInvoiceDetailEntity> { new() { Id = 1 } });

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Single(r.Details));
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnMappedPageWithTotal()
    {
        _repository
            .Setup(x => x.GetAllPagedAsync("INV", 3, "Posted", 1, 10))
            .ReturnsAsync(new PagedResultDto<SalesInvoiceEntity>
            {
                Data = new List<SalesInvoiceEntity> { new() { Id = 1 }, new() { Id = 2 } },
                TotalRecords = 25
            });

        _detailRepository
            .Setup(x => x.GetBySalesInvoiceIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SalesInvoiceDetailEntity>());

        var result = await _service.GetAllPagedAsync("INV", 3, "Posted", 1, 10);

        Assert.Equal(25, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());
    }

    // ------------------------------------------------------------
    // UpdateAsync
    // ------------------------------------------------------------
    private void SetupExisting(string status = "Draft")
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new SalesInvoiceEntity
        {
            Id = 5,
            InvoiceNumber = "INV-000005",
            Status = status
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenInvoiceDoesNotExist()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync((SalesInvoiceEntity?)null);

        var dto = OneLine();
        dto.Id = 5;

        Assert.False(await _service.UpdateAsync(dto));
    }

    [Theory]
    [InlineData("Posted")]
    [InlineData("Cancelled")]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenInvoiceIsNotDraft(string status)
    {
        SetupExisting(status);

        var dto = OneLine();
        dto.Id = 5;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(dto));

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SalesInvoiceEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceDetails_AndKeepInvoiceNumber()
    {
        SetupExisting();

        SalesInvoiceEntity? captured = null;
        _repository
            .Setup(x => x.UpdateAsync(It.IsAny<SalesInvoiceEntity>()))
            .Callback<SalesInvoiceEntity>(e => captured = e)
            .ReturnsAsync(true);

        var dto = NewDto((1, 2, 150, 0, 10), (2, 1, 250, 0, 10));
        dto.Id = 5;
        dto.InvoiceNumber = "HACKED";
        dto.UpdatedBy = "editor";

        var result = await _service.UpdateAsync(dto);

        Assert.True(result);
        Assert.Equal("INV-000005", captured!.InvoiceNumber);
        Assert.Equal("editor", captured.UpdatedBy);
        Assert.Equal(550m, captured.SubTotal);
        Assert.Equal(55m, captured.TaxAmount);
        _detailRepository.Verify(x => x.DeleteBySalesInvoiceIdAsync(5), Times.Once);
        _detailRepository.Verify(
            x => x.AddAsync(It.Is<SalesInvoiceDetailEntity>(d => d.SalesInvoiceId == 5)),
            Times.Exactly(2));
        Assert.Equal(1, _transaction.Calls);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotTouchDetails_WhenHeaderUpdateAffectsNoRows()
    {
        SetupExisting();
        _repository.Setup(x => x.UpdateAsync(It.IsAny<SalesInvoiceEntity>())).ReturnsAsync(false);

        var dto = OneLine();
        dto.Id = 5;

        Assert.False(await _service.UpdateAsync(dto));

        _detailRepository.Verify(x => x.DeleteBySalesInvoiceIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowBusinessRule_WhenNewDetailsAreInvalid()
    {
        SetupExisting();

        var dto = NewDto((1, 0, 10, 0, 0));
        dto.Id = 5;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(dto));

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SalesInvoiceEntity>()), Times.Never);
    }

    // ------------------------------------------------------------
    // Delete / Post / Cancel
    // ------------------------------------------------------------
    [Fact]
    public async Task DeleteAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.DeleteAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.DeleteAsync(3, "tester"));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNothingWasDeleted()
    {
        _repository.Setup(x => x.DeleteAsync(3, "tester")).ReturnsAsync(false);

        Assert.False(await _service.DeleteAsync(3, "tester"));
    }

    [Fact]
    public async Task PostAsync_ShouldDelegateToRepository()
    {
        _repository.Setup(x => x.PostAsync(3, "tester")).ReturnsAsync(true);

        Assert.True(await _service.PostAsync(3, "tester"));

        _repository.Verify(x => x.PostAsync(3, "tester"), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldPropagateInsufficientStockError()
    {
        _repository
            .Setup(x => x.PostAsync(3, "tester"))
            .ThrowsAsync(new BusinessRuleException("Insufficient stock for item A01 (available 1, required 5)."));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.PostAsync(3, "tester"));

        Assert.Contains("Insufficient stock", ex.Message);
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
            .ThrowsAsync(new NotFoundException("Sales invoice not found."));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelAsync(3, "tester"));
    }
}
