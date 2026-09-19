using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.CoreAPI.Test.Controllers;

public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _serviceMock;
    private readonly Mock<ILogger<CustomerController>> _loggerMock;
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _serviceMock = new Mock<ICustomerService>();
        _loggerMock = new Mock<ILogger<CustomerController>>();

        _controller = new CustomerController(
            _serviceMock.Object,
            _loggerMock.Object);
    }


    // ============================================================
    // GetAll
    // ============================================================

    [Fact]
    public async Task GetAll_ReturnsOk_WithCustomers()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new CustomerDto
            {
                Id = 1,
                CustomerCode = "C001",
                CustomerName = "ABC Customer",
                MobileNo = "1111111111",
                City = "Chennai",
                State = "Tamil Nadu",
                Country = "India",
                IsActive = true,
                IsDeleted = false
            },
            new CustomerDto
            {
                Id = 2,
                CustomerCode = "C002",
                CustomerName = "XYZ Customer",
                MobileNo = "2222222222",
                City = "Villupuram",
                State = "Tamil Nadu",
                Country = "India",
                IsActive = true,
                IsDeleted = false
            }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(customers);

        // Act
        var response = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<IEnumerable<CustomerDto>>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal(
            "Customers retrieved successfully",
            apiResponse.Message);

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
        Assert.Equal(
            "Error retrieving Customer",
            apiResponse.Message);

        Assert.Equal("500", apiResponse.Error?.Code);
        Assert.Equal(
            "Database error",
            apiResponse.Error?.Details);
    }


    // ============================================================
    // GetById
    // ============================================================

    [Fact]
    public async Task GetById_ReturnsOk_WhenCustomerExists()
    {
        // Arrange
        var customer = new CustomerDto
        {
            Id = 1,
            CustomerCode = "C001",
            CustomerName = "ABC Customer",
            MobileNo = "1111111111",
            City = "Chennai",
            State = "Tamil Nadu",
            Country = "India",
            IsActive = true,
            IsDeleted = false
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(customer);

        // Act
        var response = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<CustomerDto>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal(
            "Customer retrieved successfully",
            apiResponse.Message);

        Assert.NotNull(apiResponse.Data);
        Assert.Equal(1, apiResponse.Data.Id);
        Assert.Equal(
            "ABC Customer",
            apiResponse.Data.CustomerName);

        _serviceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }


    [Fact]
    public async Task GetById_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((CustomerDto?)null);

        // Act
        var response = await _controller.GetById(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(
                notFoundResult.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal(
            "Customer not found",
            apiResponse.Message);
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
        Assert.Equal(
            "Error retrieving Customer",
            apiResponse.Message);

        Assert.Equal(
            "Database error",
            apiResponse.Error?.Details);
    }


    // ============================================================
    // Create
    // ============================================================

    [Fact]
    public async Task Create_ReturnsOk_WhenCustomerIsCreated()
    {
        // Arrange
        var dto = new CustomerDto
        {
            CustomerCode = "C001",
            CustomerName = "ABC Customer",
            MobileNo = "1111111111",
            City = "Chennai",
            State = "Tamil Nadu",
            Country = "India",
            IsActive = true,
            IsDeleted = false
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
        Assert.Equal(
            "Customer created successfully",
            apiResponse.Message);

        Assert.Equal(10, apiResponse.Data);

        _serviceMock.Verify(
            x => x.AddAsync(dto),
            Times.Once);
    }


    [Fact]
    public async Task Create_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new CustomerDto
        {
            CustomerCode = "C001",
            CustomerName = "ABC Customer"
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
        Assert.Equal(
            "Error creating Customer",
            apiResponse.Message);

        Assert.Equal(
            "Insert failed",
            apiResponse.Error?.Details);
    }


    // ============================================================
    // Update
    // ============================================================

    [Fact]
    public async Task Update_ReturnsOk_WhenCustomerIsUpdated()
    {
        // Arrange
        var dto = new CustomerDto
        {
            Id = 999,
            CustomerCode = "C001",
            CustomerName = "Updated Customer",
            MobileNo = "1111111111",
            City = "Chennai",
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
        Assert.Equal(
            "Customer updated successfully",
            apiResponse.Message);

        // Route Id must overwrite DTO Id
        Assert.Equal(5, dto.Id);

        _serviceMock.Verify(
            x => x.UpdateAsync(dto),
            Times.Once);
    }


    [Fact]
    public async Task Update_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        var dto = new CustomerDto
        {
            Id = 999,
            CustomerCode = "C001",
            CustomerName = "ABC Customer",
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
        Assert.Equal(
            "Customer not found",
            apiResponse.Message);

        Assert.Equal(5, dto.Id);
    }


    [Fact]
    public async Task Update_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new CustomerDto
        {
            Id = 1,
            CustomerCode = "C001",
            CustomerName = "ABC Customer",
            IsActive = true
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
        Assert.Equal(
            "Error updating Customer",
            apiResponse.Message);

        Assert.Equal(
            "Update failed",
            apiResponse.Error?.Details);
    }


    // ============================================================
    // Delete
    // ============================================================

    [Fact]
    public async Task Delete_ReturnsOk_WhenCustomerIsDeleted()
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
        Assert.Equal(
            "Customer deleted successfully",
            apiResponse.Message);

        _serviceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }


    [Fact]
    public async Task Delete_ReturnsNotFound_WhenCustomerDoesNotExist()
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
        Assert.Equal(
            "Customer not found",
            apiResponse.Message);
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
        Assert.Equal(
            "Error deleting Customer",
            apiResponse.Message);

        Assert.Equal(
            "Delete failed",
            apiResponse.Error?.Details);
    }


    // ============================================================
    // GetAllPaged
    // ============================================================

    [Fact]
    public async Task GetAllPaged_ReturnsOk_WithPagedCustomers()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new CustomerDto
            {
                Id = 1,
                CustomerCode = "C001",
                CustomerName = "ABC Customer",
                City = "Chennai",
                IsActive = true
            },
            new CustomerDto
            {
                Id = 2,
                CustomerCode = "C002",
                CustomerName = "XYZ Customer",
                City = "Villupuram",
                IsActive = true
            }
        };

        var pagedResult = new PagedResultDto<CustomerDto>
        {
            Data = customers,
            TotalRecords = 2
        };

        _serviceMock
            .Setup(x => x.GetAllPagedAsync(
                "C",
                "Customer",
                "111",
                "Chennai",
                1,
                10))
            .ReturnsAsync(pagedResult);

        // Act
        var response = await _controller.GetAllPaged(
            "C",
            "Customer",
            "111",
            "Chennai",
            1,
            10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<PagedResultDto<CustomerDto>>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.Equal(
            "Customers retrieved successfully",
            apiResponse.Message);

        Assert.NotNull(apiResponse.Data);
        Assert.Equal(
            2,
            apiResponse.Data.TotalRecords);

        Assert.Equal(
            2,
            apiResponse.Data.Data.Count());

        _serviceMock.Verify(
            x => x.GetAllPagedAsync(
                "C",
                "Customer",
                "111",
                "Chennai",
                1,
                10),
            Times.Once);
    }


    [Fact]
    public async Task GetAllPaged_ReturnsOk_WithDefaultPagingValues()
    {
        // Arrange
        var pagedResult = new PagedResultDto<CustomerDto>
        {
            Data = new List<CustomerDto>(),
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
        var response = await _controller.GetAllPaged(
            null,
            null,
            null,
            null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);

        var apiResponse =
            Assert.IsType<ApiResponse<PagedResultDto<CustomerDto>>>(
                okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(0, apiResponse.Data.TotalRecords);

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
                "C",
                "Customer",
                "111",
                "Chennai",
                1,
                10))
            .ThrowsAsync(new Exception("Paging failed"));

        // Act
        var response = await _controller.GetAllPaged(
            "C",
            "Customer",
            "111",
            "Chennai",
            1,
            10);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);

        Assert.Equal(500, result.StatusCode);

        var apiResponse =
            Assert.IsType<ApiResponse<string>>(result.Value);

        Assert.False(apiResponse.Success);
        Assert.Equal(
            "Error retrieving Customer",
            apiResponse.Message);

        Assert.Equal(
            "Paging failed",
            apiResponse.Error?.Details);
    }
}