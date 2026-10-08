using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.CoreAPI.Test.Controllers;

public class StockControllerTests
{
    private readonly Mock<IStockService> _serviceMock = new();
    private readonly Mock<ILogger<StockController>> _loggerMock = new();
    private readonly StockController _controller;

    public StockControllerTests()
    {
        _controller = new StockController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithStockRows()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ItemStockDto>
        {
            new() { ItemmasterId = 1, OnHandQuantity = 5 }
        });

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Single(Assert.IsType<ApiResponse<IEnumerable<ItemStockDto>>>(ok.Value).Data!);
    }

    [Fact]
    public async Task GetAll_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.GetAll()).StatusCode);
    }

    [Fact]
    public async Task GetByItem_ReturnsOk_WhenItemExists()
    {
        _serviceMock
            .Setup(x => x.GetByItemmasterIdAsync(1))
            .ReturnsAsync(new ItemStockDto { ItemmasterId = 1, OnHandQuantity = 6 });

        var result = await _controller.GetByItem(1);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(6m, Assert.IsType<ApiResponse<ItemStockDto>>(ok.Value).Data!.OnHandQuantity);
    }

    [Fact]
    public async Task GetByItem_ReturnsNotFound_WhenItemDoesNotExist()
    {
        _serviceMock.Setup(x => x.GetByItemmasterIdAsync(1)).ReturnsAsync((ItemStockDto?)null);

        Assert.IsType<NotFoundObjectResult>(await _controller.GetByItem(1));
    }

    [Fact]
    public async Task GetByItem_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetByItemmasterIdAsync(1)).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.GetByItem(1)).StatusCode);
    }
}
