using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

namespace Invoice.CoreAPI.Test;

public class MasterCountControllerTests
{
    private readonly Mock<IMasterCountService> _serviceMock;
    private readonly Mock<ILogger<MasterCountController>> _loggerMock;
    private readonly MasterCountController _controller;

    public MasterCountControllerTests()
    {
        _serviceMock = new Mock<IMasterCountService>();
        _loggerMock = new Mock<ILogger<MasterCountController>>();

        _controller = new MasterCountController(
            _serviceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Customers_ReturnsOk_WhenServiceReturnsCount()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.CountCustomersAsync(true))
            .ReturnsAsync(42);

        // Act
        var result = await _controller.Customers(true);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);

        var response = Assert.IsType<ApiResponse<int>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Customer count retrieved successfully", response.Message);
        Assert.Equal(42, response.Data);

        _serviceMock.Verify(x => x.CountCustomersAsync(true), Times.Once);
    }

    [Fact]
    public async Task Customers_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.CountCustomersAsync(It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Customers(null);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Error retrieving customer count", response.Message);
        Assert.NotNull(response.Error);
        Assert.Equal("500", response.Error.Code);

        _serviceMock.Verify(x => x.CountCustomersAsync(null), Times.Once);
    }

    [Fact]
    public async Task CustomersByCity_ReturnsOk_WhenServiceReturnsCounts()
    {
        // Arrange
        var counts = new List<CityCountDto>
        {
            new CityCountDto("Chennai", 10),
            new CityCountDto("Bangalore", 5)
        };

        _serviceMock
            .Setup(x => x.CountCustomersGroupedByCityAsync(null))
            .ReturnsAsync(counts);

        // Act
        var result = await _controller.CustomersByCity(null);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<CityCountDto>>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Customer counts by city retrieved successfully", response.Message);
        Assert.NotNull(response.Data);

        var list = response.Data.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("Chennai", list[0].City);
        Assert.Equal(10, list[0].Count);

        _serviceMock.Verify(x => x.CountCustomersGroupedByCityAsync(null), Times.Once);
    }

    [Fact]
    public async Task Vendors_ReturnsOk_WhenServiceReturnsCount()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.CountVendorsAsync(true))
            .ReturnsAsync(7);

        // Act
        var result = await _controller.Vendors(true);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<int>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Vendor count retrieved successfully", response.Message);
        Assert.Equal(7, response.Data);

        _serviceMock.Verify(x => x.CountVendorsAsync(true), Times.Once);
    }

    [Fact]
    public async Task Categories_ReturnsOk_WhenServiceReturnsCount()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.CountCategoriesAsync(null))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.Categories(null);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<int>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Category count retrieved successfully", response.Message);
        Assert.Equal(3, response.Data);

        _serviceMock.Verify(x => x.CountCategoriesAsync(null), Times.Once);
    }

    [Fact]
    public async Task Items_ReturnsOk_WhenServiceReturnsCount()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.CountItemsAsync(1, true))
            .ReturnsAsync(15);

        // Act
        var result = await _controller.Items(1, true);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<int>>(ok.Value);

        Assert.True(response.Success);
        Assert.Equal("Item count retrieved successfully", response.Message);
        Assert.Equal(15, response.Data);

        _serviceMock.Verify(x => x.CountItemsAsync(1, true), Times.Once);
    }
}
