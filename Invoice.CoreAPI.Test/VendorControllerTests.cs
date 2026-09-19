using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.CoreAPI.Test.Controllers;

public class VendorControllerTests
{
    private readonly Mock<IVendorService> _serviceMock;
    private readonly Mock<ILogger<VendorController>> _loggerMock;
    private readonly VendorController _controller;

    public VendorControllerTests()
    {
        _serviceMock = new Mock<IVendorService>();
        _loggerMock = new Mock<ILogger<VendorController>>();

        _controller = new VendorController(
            _serviceMock.Object,
            _loggerMock.Object);
    }

    // ============================================================
    // GetAll
    // ============================================================

    [Fact]
    public async Task GetAll_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var vendors = new List<VendorDto>
        {
            new VendorDto
            {
                Id = 1,
                VendorCode = "V001",
                VendorName = "ABC Supplier"
            },
            new VendorDto
            {
                Id = 2,
                VendorCode = "V002",
                VendorName = "XYZ Supplier"
            }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(vendors);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

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
        var result = await _controller.GetAll();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    // ============================================================
    // GetById
    // ============================================================

    [Fact]
    public async Task GetById_ReturnsOk_WhenVendorExists()
    {
        // Arrange
        var vendor = new VendorDto
        {
            Id = 1,
            VendorCode = "V001",
            VendorName = "ABC Supplier"
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(vendor);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _serviceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenVendorDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((VendorDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.NotNull(notFoundResult.Value);

        _serviceMock.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task GetById_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    // ============================================================
    // Create
    // ============================================================

    [Fact]
    public async Task Create_ReturnsOk_WhenVendorIsCreated()
    {
        // Arrange
        var vendor = new VendorDto
        {
            VendorCode = "V001",
            VendorName = "ABC Supplier"
        };

        _serviceMock
            .Setup(x => x.AddAsync(vendor))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.Create(vendor);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _serviceMock.Verify(
            x => x.AddAsync(vendor),
            Times.Once);
    }

    [Fact]
    public async Task Create_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var vendor = new VendorDto
        {
            VendorCode = "V001",
            VendorName = "ABC Supplier"
        };

        _serviceMock
            .Setup(x => x.AddAsync(vendor))
            .ThrowsAsync(new Exception("Insert failed"));

        // Act
        var result = await _controller.Create(vendor);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.AddAsync(vendor),
            Times.Once);
    }

    // ============================================================
    // Update
    // ============================================================

    [Fact]
    public async Task Update_ReturnsOk_WhenVendorIsUpdated()
    {
        // Arrange
        var vendor = new VendorDto
        {
            Id = 1,
            VendorCode = "V001",
            VendorName = "ABC Supplier Updated"
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(vendor))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(1, vendor);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        Assert.Equal(1, vendor.Id);

        _serviceMock.Verify(
            x => x.UpdateAsync(vendor),
            Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenVendorDoesNotExist()
    {
        // Arrange
        var vendor = new VendorDto
        {
            Id = 999,
            VendorCode = "V999",
            VendorName = "Unknown Vendor"
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(vendor))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(999, vendor);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.NotNull(notFoundResult.Value);

        _serviceMock.Verify(
            x => x.UpdateAsync(vendor),
            Times.Once);
    }

    [Fact]
    public async Task Update_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var vendor = new VendorDto
        {
            Id = 1,
            VendorCode = "V001",
            VendorName = "ABC Supplier"
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(vendor))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await _controller.Update(1, vendor);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.UpdateAsync(vendor),
            Times.Once);
    }

    // ============================================================
    // Delete
    // ============================================================

    [Fact]
    public async Task Delete_ReturnsOk_WhenVendorIsDeleted()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _serviceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenVendorDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.NotNull(notFoundResult.Value);

        _serviceMock.Verify(
            x => x.DeleteAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task Delete_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.DeleteAsync(1))
            .ThrowsAsync(new Exception("Delete failed"));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }

    // ============================================================
    // GetAllPaged
    // ============================================================

    [Fact]
    public async Task GetAllPaged_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var vendors = new List<VendorDto>
        {
            new VendorDto
            {
                Id = 1,
                VendorCode = "V001",
                VendorName = "ABC Supplier",
                City = "Chennai"
            },
            new VendorDto
            {
                Id = 2,
                VendorCode = "V002",
                VendorName = "XYZ Supplier",
                City = "Chennai"
            }
        };

        var pagedResult = new PagedResultDto<VendorDto>
        {
            Data = vendors,
            TotalRecords = 2
        };

        _serviceMock
            .Setup(x => x.GetAllPagedAsync(
                "V",
                "Supplier",
                "9876543210",
                "Chennai",
                1,
                10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAllPaged(
            "V",
            "Supplier",
            "9876543210",
            "Chennai",
            1,
            10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                "V",
                "Supplier",
                "9876543210",
                "Chennai",
                1,
                10),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPaged_UsesDefaultPaging_WhenPagingIsNotProvided()
    {
        // Arrange
        var pagedResult = new PagedResultDto<VendorDto>
        {
            Data = new List<VendorDto>(),
            TotalRecords = 0
        };

        _serviceMock
            .Setup(x => x.GetAllPagedAsync(
                null,
                null,
                null,
                null,
                1,
                10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAllPaged(
            null,
            null,
            null,
            null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                null,
                null,
                null,
                null,
                1,
                10),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPaged_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetAllPagedAsync(
                "V",
                "Supplier",
                "9876543210",
                "Chennai",
                1,
                10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllPaged(
            "V",
            "Supplier",
            "9876543210",
            "Chennai",
            1,
            10);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                "V",
                "Supplier",
                "9876543210",
                "Chennai",
                1,
                10),
            Times.Once);
    }
}
