using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Timers;

namespace Invoice.CoreAPI.Test;

public class UserControllerTests
{
    private readonly Mock<IUserService> _serviceMock;
    private readonly Mock<ILogger<UserController>> _loggerMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _serviceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UserController>>();

        _controller = new UserController(
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
        var users = new List<UserDto>
        {
            new UserDto
            {
                Id = 1,
                UserName = "admin",
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "User",
                DisplayName = "Admin User",
                PhoneNumber = "9876543210",
                AddressLine1 = "Test Address",
                City = "Chennai",
                State = "Tamil Nadu",
                ZipCode = "600001",
                Country = "India",
                IsActive = true
            }
        };

        var response = new ApiResponse<IEnumerable<UserDto>>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = users
        };

        _serviceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

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
    public async Task GetById_ReturnsOk_WhenUserExists()
    {
        // Arrange
        var user = new UserDto
        {
            Id = 1,
            UserName = "admin",
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            DisplayName = "Admin User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true
        };

        var response = new ApiResponse<UserDto>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = user
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _serviceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var response = new ApiResponse<UserDto>
        {
            Success = false,
            Message = "User not found."
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Same(response, notFoundResult.Value);

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
    public async Task Create_ReturnsOk_WhenUserIsCreated()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            UserName = "testuser",
            Email = "testuser@test.com",
            Password = "Password@123",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            CreatedBy = "admin"
        };

        var user = new UserDto
        {
            Id = 10,
            UserName = dto.UserName,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName,
            PhoneNumber = dto.PhoneNumber,
            AddressLine1 = dto.AddressLine1,
            City = dto.City,
            State = dto.State,
            ZipCode = dto.ZipCode,
            Country = dto.Country,
            IsActive = true
        };

        var response = new ApiResponse<UserDto>
        {
            Success = true,
            Message = "User created successfully.",
            Data = user
        };

        _serviceMock
            .Setup(x => x.CreateAsync(dto))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _serviceMock.Verify(
            x => x.CreateAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenUserAlreadyExists()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            UserName = "admin",
            Email = "admin@test.com",
            Password = "Password@123",
            FirstName = "Admin",
            LastName = "User",
            DisplayName = "Admin User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            CreatedBy = "admin"
        };

        var response = new ApiResponse<UserDto>
        {
            Success = false,
            Message = "Username already exists."
        };

        _serviceMock
            .Setup(x => x.CreateAsync(dto))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var conflictResult =
            Assert.IsType<ConflictObjectResult>(result);

        Assert.Same(response, conflictResult.Value);

        _serviceMock.Verify(
            x => x.CreateAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task Create_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            UserName = "testuser",
            Email = "testuser@test.com",
            Password = "Password@123",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            CreatedBy = "admin"
        };

        _serviceMock
            .Setup(x => x.CreateAsync(dto))
            .ThrowsAsync(new Exception("Insert failed"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.CreateAsync(dto),
            Times.Once);
    }

    // ============================================================
    // Update
    // ============================================================

    [Fact]
    public async Task Update_ReturnsOk_WhenUserIsUpdated()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@test.com",
            FirstName = "Updated",
            LastName = "User",
            DisplayName = "Updated User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Updated Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            UpdatedBy = "admin"
        };

        var response = new ApiResponse<UserDto>
        {
            Success = true,
            Message = "User updated successfully.",
            Data = new UserDto
            {
                Id = 1,
                UserName = dto.UserName,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DisplayName = dto.DisplayName,
                PhoneNumber = dto.PhoneNumber,
                AddressLine1 = dto.AddressLine1,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Country = dto.Country,
                IsActive = true
            }
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(1, dto))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _serviceMock.Verify(
            x => x.UpdateAsync(1, dto),
            Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            UserName = "unknown",
            Email = "unknown@test.com",
            FirstName = "Unknown",
            LastName = "User",
            DisplayName = "Unknown User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            UpdatedBy = "admin"
        };

        var response = new ApiResponse<UserDto>
        {
            Success = false,
            Message = "User not found."
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(999, dto))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Update(999, dto);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Same(response, notFoundResult.Value);

        _serviceMock.Verify(
            x => x.UpdateAsync(999, dto),
            Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenUpdateFails()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            UserName = "testuser",
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            UpdatedBy = "admin"
        };

        var response = new ApiResponse<UserDto>
        {
            Success = false,
            Message = "Unable to update user."
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(1, dto))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Same(response, badRequestResult.Value);

        _serviceMock.Verify(
            x => x.UpdateAsync(1, dto),
            Times.Once);
    }

    [Fact]
    public async Task Update_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            UserName = "testuser",
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            UpdatedBy = "admin"
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(1, dto))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.UpdateAsync(1, dto),
            Times.Once);
    }

    // ============================================================
    // Delete
    // ============================================================

    [Fact]
    public async Task Delete_ReturnsOk_WhenUserIsDeleted()
    {
        // Arrange
        var response = new ApiResponse<bool>
        {
            Success = true,
            Message = "User deleted successfully.",
            Data = true
        };

        _serviceMock
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _serviceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var response = new ApiResponse<bool>
        {
            Success = false,
            Message = "User not found."
        };

        _serviceMock
            .Setup(x => x.DeleteAsync(999))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Same(response, notFoundResult.Value);

        _serviceMock.Verify(
            x => x.DeleteAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenDeleteFails()
    {
        // Arrange
        var response = new ApiResponse<bool>
        {
            Success = false,
            Message = "Unable to delete user.",
            Data = false
        };

        _serviceMock
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Same(response, badRequestResult.Value);

        _serviceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }

    // ============================================================
    // GetPaged
    // ============================================================

    [Fact]
    public async Task GetPaged_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var filter = new UserFilterDto();

        var pagedData = new PagedResultDto<UserDto>
        {
            Data = new List<UserDto>
            {
                new UserDto
                {
                    Id = 1,
                    UserName = "admin",
                    Email = "admin@test.com",
                    FirstName = "Admin",
                    LastName = "User",
                    DisplayName = "Admin User",
                    PhoneNumber = "9876543210",
                    AddressLine1 = "Test Address",
                    City = "Chennai",
                    State = "Tamil Nadu",
                    ZipCode = "600001",
                    Country = "India",
                    IsActive = true
                }
            },
            TotalRecords = 1
        };

        var response =
            new ApiResponse<PagedResultDto<UserDto>>
            {
                Success = true,
                Message = "Users retrieved successfully.",
                Data = pagedData
            };

        _serviceMock
            .Setup(x => x.GetPagedAsync(filter))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetPaged(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _serviceMock.Verify(
            x => x.GetPagedAsync(filter),
            Times.Once);
    }

    [Fact]
    public async Task GetPaged_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var filter = new UserFilterDto();

        _serviceMock
            .Setup(x => x.GetPagedAsync(filter))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPaged(filter);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.GetPagedAsync(filter),
            Times.Once);
    }

    // ============================================================
    // ValidateUser
    // ============================================================

    [Fact]
    public async Task ValidateUser_ReturnsOk_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            UserName = "admin",
            Password = "Password@123"
        };

        var user = new UserDto
        {
            Id = 1,
            UserName = "admin",
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            DisplayName = "Admin User",
            PhoneNumber = "9876543210",
            AddressLine1 = "Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true
        };

        _serviceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.ValidateUser(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _serviceMock.Verify(
            x => x.ValidateUserAsync(
                request.UserName,
                request.Password),
            Times.Once);
    }

    [Fact]
    public async Task ValidateUser_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            UserName = "admin",
            Password = "WrongPassword"
        };

        _serviceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.ValidateUser(request);

        // Assert
        var unauthorizedResult =
            Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.NotNull(unauthorizedResult.Value);

        _serviceMock.Verify(
            x => x.ValidateUserAsync(
                request.UserName,
                request.Password),
            Times.Once);
    }

    [Fact]
    public async Task ValidateUser_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var request = new UserLoginRequest
        {
            UserName = "admin",
            Password = "Password@123"
        };

        _serviceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ThrowsAsync(new Exception("Authentication error"));

        // Act
        var result = await _controller.ValidateUser(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusResult.StatusCode);

        _serviceMock.Verify(
            x => x.ValidateUserAsync(
                request.UserName,
                request.Password),
            Times.Once);
    }
}
