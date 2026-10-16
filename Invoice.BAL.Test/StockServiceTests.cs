using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Moq;

namespace Invoice.BAL.Test;

public class StockServiceTests
{
    private readonly Mock<IStockRepository> _repository = new();
    private readonly StockServiceEFSp _service;

    public StockServiceTests()
    {
        _service = new StockServiceEFSp(_repository.Object, TestMapper.Create());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedRows()
    {
        _repository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ItemStockEntity>
        {
            new() { ItemmasterId = 1, ItemCode = "A", OnHandQuantity = 5 },
            new() { ItemmasterId = 2, ItemCode = "B", OnHandQuantity = 0 }
        });

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].ItemCode);
        Assert.Equal(5m, result[0].OnHandQuantity);
    }

    [Fact]
    public async Task GetByItemmasterIdAsync_ShouldReturnMappedRow()
    {
        _repository
            .Setup(x => x.GetByItemmasterIdAsync(1))
            .ReturnsAsync(new ItemStockEntity { ItemmasterId = 1, ReceivedQuantity = 10, SoldQuantity = 4, OnHandQuantity = 6 });

        var result = await _service.GetByItemmasterIdAsync(1);

        Assert.Equal(6m, result!.OnHandQuantity);
        Assert.Equal(10m, result.ReceivedQuantity);
        Assert.Equal(4m, result.SoldQuantity);
    }

    [Fact]
    public async Task GetByItemmasterIdAsync_ShouldReturnNull_WhenItemDoesNotExist()
    {
        _repository.Setup(x => x.GetByItemmasterIdAsync(1)).ReturnsAsync((ItemStockEntity?)null);

        Assert.Null(await _service.GetByItemmasterIdAsync(1));
    }
}
