using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

namespace Invoice.CoreAPI.Test;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _serviceMock;
    private readonly Mock<ILogger<CategoryController>> _loggerMock;
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _serviceMock = new Mock<ICategoryService>();
        _loggerMock = new Mock<ILogger<CategoryController>>();

        _controller = new CategoryController(
            _serviceMock.Object,
            _loggerMock.Object);
    }

    // =========================================================
    // GetAll
    // =========================================================

    [Fact]
    public async Task GetAll_ReturnsOk_WhenServiceReturnsCategories()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new CategoryDto
            {
                Id = 1,
                Code = "RI001",
                Name = "Rice",
                Description = "All types of rice",
                IsActive = true
            },
            new CategoryDto
            {
                Id = 2,
                Code = "VE001",
                Name = "Vegitables",
                Description = "All types of vegitables",
                IsActive = true
            }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<IEnumerable<CategoryDto>>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "Categories retrieved successfully.",
            response.Message);

        Assert.NotNull(response.Data);

        var data = response.Data.ToList();

        Assert.Equal(2, data.Count);
        Assert.Equal("RI001", data[0].Code);
        Assert.Equal("Rice", data[0].Name);

        _serviceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenServiceReturnsEmptyList()
    {
        // Arrange
        var categories = Enumerable.Empty<CategoryDto>();

        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<IEnumerable<CategoryDto>>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);

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
            .ThrowsAsync(
                new Exception("Database connection failed."));

        // Act
        var result = await _controller.GetAll();

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<string>>(
                objectResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Error retrieving categories.",
            response.Message);

        Assert.NotNull(response.Error);
        Assert.Equal("500", response.Error.Code);
    }

    // =========================================================
    // GetById
    // =========================================================

    [Fact]
    public async Task GetById_ReturnsOk_WhenCategoryExists()
    {
        // Arrange
        var category = new CategoryDto
        {
            Id = 1,
            Code = "RI001",
            Name = "Rice",
            Description = "All types of rice",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<CategoryDto>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "Category retrieved successfully.",
            response.Message);

        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data.Id);
        Assert.Equal("RI001", response.Data.Code);
        Assert.Equal("Rice", response.Data.Name);

        _serviceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(9999))
            .ReturnsAsync((CategoryDto?)null);

        // Act
        var result = await _controller.GetById(9999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Category not found.",
            response.Message);

        _serviceMock.Verify(
            x => x.GetByIdAsync(9999),
            Times.Once);
    }

    [Fact]
    public async Task GetById_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ThrowsAsync(
                new Exception("Database error."));

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<string>>(
                objectResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Error retrieving category.",
            response.Message);

        Assert.NotNull(response.Error);
        Assert.Equal("500", response.Error.Code);
    }

    // =========================================================
    // Create
    // =========================================================

    [Fact]
    public async Task Create_ReturnsOk_WhenCategoryIsCreated()
    {
        // Arrange
        var dto = new CategoryDto
        {
            Code = "AP001",
            Name = "Apples",
            Description = "Apple products",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.AddAsync(dto))
            .ReturnsAsync(2004);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<int>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "Category created successfully.",
            response.Message);

        Assert.Equal(2004, response.Data);

        _serviceMock.Verify(
            x => x.AddAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCategoryIsNull()
    {
        // Act
        var result = await _controller.Create(null!);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Category data is required.",
            response.Message);

        _serviceMock.Verify(
            x => x.AddAsync(It.IsAny<CategoryDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new CategoryDto
        {
            Code = "AP001",
            Name = "Apples",
            Description = "Apple products",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.AddAsync(dto))
            .ThrowsAsync(
                new Exception("Insert failed."));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<string>>(
                objectResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Error creating category.",
            response.Message);
    }

    // =========================================================
    // Update
    // =========================================================

    [Fact]
    public async Task Update_ReturnsOk_WhenCategoryIsUpdated()
    {
        // Arrange
        var dto = new CategoryDto
        {
            Code = "AP001",
            Name = "Green Apples",
            Description = "All types of green apples",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(It.Is<CategoryDto>(
                x => x.Id == 2004 &&
                     x.Code == "AP001" &&
                     x.Name == "Green Apples")))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(2004, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "Category updated successfully.",
            response.Message);

        Assert.Equal(2004, dto.Id);

        _serviceMock.Verify(
            x => x.UpdateAsync(It.Is<CategoryDto>(
                c => c.Id == 2004 &&
                     c.Code == "AP001" &&
                     c.Name == "Green Apples")),
            Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenCategoryIsNull()
    {
        // Act
        var result = await _controller.Update(2004, null!);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Category data is required.",
            response.Message);

        _serviceMock.Verify(
            x => x.UpdateAsync(It.IsAny<CategoryDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var dto = new CategoryDto
        {
            Code = "XX001",
            Name = "Unknown",
            Description = "Unknown category",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<CategoryDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(9999, dto);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Category not found.",
            response.Message);

        Assert.Equal(9999, dto.Id);
    }

    [Fact]
    public async Task Update_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new CategoryDto
        {
            Code = "AP001",
            Name = "Apples",
            Description = "Apple products",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<CategoryDto>()))
            .ThrowsAsync(
                new Exception("Update failed."));

        // Act
        var result = await _controller.Update(2004, dto);

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<string>>(
                objectResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Error updating category.",
            response.Message);
    }

    // =========================================================
    // Delete
    // =========================================================

    [Fact]
    public async Task Delete_ReturnsOk_WhenCategoryIsDeleted()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(2004))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(2004);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "Category deleted successfully.",
            response.Message);

        _serviceMock.Verify(
            x => x.DeleteAsync(2004),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(9999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(9999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Category not found.",
            response.Message);

        _serviceMock.Verify(
            x => x.DeleteAsync(9999),
            Times.Once);
    }

    [Fact]
    public async Task Delete_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(2004))
            .ThrowsAsync(
                new Exception("Delete failed."));

        // Act
        var result = await _controller.Delete(2004);

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<string>>(
                objectResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Error deleting category.",
            response.Message);
    }

    // =========================================================
    // GetAllPaged
    // =========================================================

    [Fact]
    public async Task GetAllPaged_ReturnsOk_WhenRequestIsValid()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new CategoryDto
            {
                Id = 1,
                Code = "RI001",
                Name = "Rice",
                Description = "All types of rice",
                IsActive = true
            },
            new CategoryDto
            {
                Id = 2,
                Code = "VE001",
                Name = "Vegitables",
                Description = "All types of vegitables",
                IsActive = true
            }
        };

        var pagedResult = new PagedResultDto<CategoryDto>
        {
            Data = categories,
            TotalRecords = 9
        };

        _serviceMock
            .Setup(x => x.GetAllPagedAsync(
                "RI",
                null,
                1,
                5))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAllPaged(
            "RI",
            null,
            1,
            5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<IEnumerable<CategoryDto>>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "Categories retrieved successfully.",
            response.Message);

        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count());

        Assert.Equal(9, response.TotalRecords);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                "RI",
                null,
                1,
                5),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPaged_ReturnsBadRequest_WhenPageNumberIsLessThanOne()
    {
        // Act
        var result = await _controller.GetAllPaged(
            null,
            null,
            0,
            10);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Page number must be greater than zero.",
            response.Message);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllPaged_ReturnsBadRequest_WhenPageSizeIsLessThanOne()
    {
        // Act
        var result = await _controller.GetAllPaged(
            null,
            null,
            1,
            0);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<string>>(
                badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Page size must be greater than zero.",
            response.Message);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllPaged_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetAllPagedAsync(
                "RI",
                null,
                1,
                10))
            .ThrowsAsync(
                new Exception("Paging query failed."));

        // Act
        var result = await _controller.GetAllPaged(
            "RI",
            null,
            1,
            10);

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            objectResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<string>>(
                objectResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Error retrieving categories.",
            response.Message);
    }
}
