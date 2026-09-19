using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace Invoice.CoreAPI.Test.Controllers;

public class ItemmasterControllerTests
{
    private readonly Mock<IItemmasterService> _serviceMock;
    private readonly Mock<ILogger<ItemmasterController>> _loggerMock;
    private readonly ItemmasterController _controller;

    public ItemmasterControllerTests()
    {
        _serviceMock = new Mock<IItemmasterService>();
        _loggerMock = new Mock<ILogger<ItemmasterController>>();

        _controller = new ItemmasterController(
            _serviceMock.Object,
            _loggerMock.Object);
    }

    // ============================================================
    // GetAllPaged
    // ============================================================

    [Fact]
    public async Task GetAllPaged_ReturnsOk_WithPagedData()
    {
        // Arrange
        var search = new ItemmasterFilterDto();

        var items = new List<ItemmasterDto>
        {
            new ItemmasterDto
            {
                Id = 1,
                CategoryId = 1,
                ItemCode = "IT001",
                ItemBarCode = "BAR001",
                ItemName = "Rice",
                Uom = "KG",
                IsActive = true
            }
        };

        var result = new PagedResultDto<ItemmasterDto>
        {
            Data = items,
            TotalRecords = 1
        };

        _serviceMock
            .Setup(x => x.GetAllPagedAsync(search))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetAllPaged(search);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<PagedResultDto<ItemmasterDto>>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal("Items retrieved successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(1, apiResponse.Data.TotalRecords);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(search),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPaged_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var search = new ItemmasterFilterDto();

        _serviceMock
            .Setup(x => x.GetAllPagedAsync(search))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _controller.GetAllPaged(search);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Error retrieving items", apiResponse.Message);
        Assert.Equal("500", apiResponse.Error?.Code);
        Assert.Equal("Database error", apiResponse.Error?.Details);
    }


    // ============================================================
    // GetAll
    // ============================================================

    [Fact]
    public async Task GetAll_ReturnsOk_WithItems()
    {
        // Arrange
        var items = new List<ItemmasterDto>
        {
            new ItemmasterDto
            {
                Id = 1,
                CategoryId = 1,
                ItemCode = "IT001",
                ItemBarCode = "BAR001",
                ItemName = "Rice",
                Uom = "KG",
                IsActive = true
            },
            new ItemmasterDto
            {
                Id = 2,
                CategoryId = 1,
                ItemCode = "IT002",
                ItemBarCode = "BAR002",
                ItemName = "Wheat",
                Uom = "KG",
                IsActive = true
            }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(items);

        // Act
        var response = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<IEnumerable<ItemmasterDto>>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal("Items retrieved successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(2, apiResponse.Data.Count());

        _serviceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _controller.GetAll();

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Error retrieving Itemmaster", apiResponse.Message);
        Assert.Equal("500", apiResponse.Error?.Code);
        Assert.Equal("Database error", apiResponse.Error?.Details);
    }


    // ============================================================
    // GetById
    // ============================================================

    [Fact]
    public async Task GetById_ReturnsOk_WhenItemExists()
    {
        // Arrange
        var item = new ItemmasterDto
        {
            Id = 1,
            CategoryId = 1,
            ItemCode = "IT001",
            ItemBarCode = "BAR001",
            ItemName = "Rice",
            Uom = "KG",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(item);

        // Act
        var response = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<ItemmasterDto>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal("Item retrieved successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(1, apiResponse.Data.Id);
        Assert.Equal("Rice", apiResponse.Data.ItemName);

        _serviceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ItemmasterDto?)null);

        // Act
        var response = await _controller.GetById(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Item not found", apiResponse.Message);
    }

    [Fact]
    public async Task GetById_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _controller.GetById(1);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Error retrieving item", apiResponse.Message);
        Assert.Equal("Database error", apiResponse.Error?.Details);
    }


    // ============================================================
    // Create
    // ============================================================

    [Fact]
    public async Task Create_ReturnsOk_WhenItemIsCreated()
    {
        // Arrange
        var dto = new ItemmasterDto
        {
            CategoryId = 1,
            ItemCode = "IT001",
            ItemBarCode = "BAR001",
            ItemName = "Rice",
            Uom = "KG",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.AddAsync(dto))
            .ReturnsAsync(10);

        // Act
        var response = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<int>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal("Item created successfully", apiResponse.Message);
        Assert.Equal(10, apiResponse.Data);

        _serviceMock.Verify(
            x => x.AddAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task Create_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new ItemmasterDto
        {
            CategoryId = 1,
            ItemCode = "IT001",
            ItemBarCode = "BAR001",
            ItemName = "Rice",
            Uom = "KG"
        };

        _serviceMock
            .Setup(x => x.AddAsync(dto))
            .ThrowsAsync(new Exception("Insert failed"));

        // Act
        var response = await _controller.Create(dto);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Error creating item", apiResponse.Message);
        Assert.Equal("Insert failed", apiResponse.Error?.Details);
    }


    // ============================================================
    // Update
    // ============================================================

    [Fact]
    public async Task Update_ReturnsOk_WhenItemIsUpdated()
    {
        // Arrange
        var dto = new ItemmasterDto
        {
            Id = 999,
            CategoryId = 1,
            ItemCode = "IT001",
            ItemBarCode = "BAR001",
            ItemName = "Updated Rice",
            Uom = "KG",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(dto))
            .ReturnsAsync(true);

        // Act
        var response = await _controller.Update(5, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal("Item updated successfully", apiResponse.Message);

        // Controller must overwrite DTO Id with route Id
        Assert.Equal(5, dto.Id);

        _serviceMock.Verify(
            x => x.UpdateAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var dto = new ItemmasterDto
        {
            Id = 999,
            CategoryId = 1,
            ItemCode = "IT001",
            ItemBarCode = "BAR001",
            ItemName = "Rice",
            Uom = "KG",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(dto))
            .ReturnsAsync(false);

        // Act
        var response = await _controller.Update(5, dto);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Item not found", apiResponse.Message);

        // Verify route Id was assigned
        Assert.Equal(5, dto.Id);
    }

    [Fact]
    public async Task Update_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new ItemmasterDto
        {
            Id = 1,
            CategoryId = 1,
            ItemCode = "IT001",
            ItemBarCode = "BAR001",
            ItemName = "Rice",
            Uom = "KG"
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(dto))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var response = await _controller.Update(1, dto);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Error updating item", apiResponse.Message);
        Assert.Equal("Update failed", apiResponse.Error?.Details);
    }


    // ============================================================
    // Delete
    // ============================================================

    [Fact]
    public async Task Delete_ReturnsOk_WhenItemIsDeleted()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var response = await _controller.Delete(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal("Item deleted successfully", apiResponse.Message);

        _serviceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(999))
            .ReturnsAsync(false);

        // Act
        var response = await _controller.Delete(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Item not found", apiResponse.Message);
    }

    [Fact]
    public async Task Delete_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(1))
            .ThrowsAsync(new Exception("Delete failed"));

        // Act
        var response = await _controller.Delete(1);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal("Error deleting item", apiResponse.Message);
        Assert.Equal("Delete failed", apiResponse.Error?.Details);
    }


    // ============================================================
    // GetActiveItemCountByCategory
    // ============================================================

    [Fact]
    public async Task GetActiveItemCountByCategory_ReturnsOk_WithCount()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetActiveItemCountByCategoryAsync(1))
            .ReturnsAsync(3);

        // Act
        var response =
            await _controller.GetActiveItemCountByCategory(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<int>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal(
            "Active item count retrieved successfully",
            apiResponse.Message);

        Assert.Equal(3, apiResponse.Data);

        _serviceMock.Verify(
            x => x.GetActiveItemCountByCategoryAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveItemCountByCategory_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetActiveItemCountByCategoryAsync(1))
            .ThrowsAsync(new Exception("Count failed"));

        // Act
        var response =
            await _controller.GetActiveItemCountByCategory(1);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal(
            "Error retrieving active item count",
            apiResponse.Message);

        Assert.Equal(
            "Count failed",
            apiResponse.Error?.Details);
    }


    // ============================================================
    // TestException
    // ============================================================

    [Fact]
    public void TestException_ThrowsExpectedException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(
            () => _controller.TestException());

        Assert.Equal(
            "This is a test exception",
            exception.Message);
    }
}
