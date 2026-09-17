using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Moq;

namespace Invoice.BAL.Test.Services;

public class UserServiceSpDapTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly UserServiceSpDap _service;

    public UserServiceSpDapTests()
    {
        _repositoryMock = new Mock<IUserRepository>();

        _service = new UserServiceSpDap(
            _repositoryMock.Object);
    }

    private static UserEntity CreateUser(
        int id = 1,
        string userName = "testuser",
        string email = "test@test.com",
        bool isActive = true,
        bool isDeleted = false)
    {
        return new UserEntity
        {
            Id = id,
            UserName = userName,
            Email = email,
            PasswordHash = string.Empty,
            FirstName = "Test",
            MiddleName = "M",
            LastName = "User",
            DisplayName = "Test User",
            PhoneNumber = "9999999999",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedBy = "TEST",
            CreatedDate = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSuccessResponse()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<UserEntity>
            {
                CreateUser(1, "user1", "user1@test.com"),
                CreateUser(2, "user2", "user2@test.com")
            });

        var result = await _service.GetAllAsync();

        Assert.True(result.Success);
        Assert.Equal(
            "Users retrieved successfully.",
            result.Message);

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenFound()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(CreateUser());

        var result = await _service.GetByIdAsync(1);

        Assert.True(result.Success);
        Assert.Equal(
            "Users retrieved successfully.",
            result.Message);

        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data!.Id);
        Assert.Equal("testuser", result.Data.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((UserEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenUsernameExists()
    {
        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("testuser"))
            .ReturnsAsync(CreateUser());

        var dto = new UserCreateDto
        {
            UserName = "testuser",
            Email = "new@test.com",
            Password = "Password123!"
        };

        var result = await _service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(
            "Username already exists.",
            result.Message);

        _repositoryMock.Verify(
            x => x.GetByEmailAsync(It.IsAny<string>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.InsertAsync(It.IsAny<UserEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenEmailExists()
    {
        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("newuser"))
            .ReturnsAsync((UserEntity?)null);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync("existing@test.com"))
            .ReturnsAsync(
                CreateUser(email: "existing@test.com"));

        var dto = new UserCreateDto
        {
            UserName = "newuser",
            Email = "existing@test.com",
            Password = "Password123!"
        };

        var result = await _service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(
            "Email already exists.",
            result.Message);

        _repositoryMock.Verify(
            x => x.InsertAsync(It.IsAny<UserEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WhenUsernameAndEmailAreUnique()
    {
        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("newuser"))
            .ReturnsAsync((UserEntity?)null);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync("new@test.com"))
            .ReturnsAsync((UserEntity?)null);

        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync(100);

        var dto = new UserCreateDto
        {
            UserName = "newuser",
            Email = "new@test.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            DisplayName = "New User",
            PhoneNumber = "9999999999",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            CreatedBy = "TEST"
        };

        var result = await _service.CreateAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(
            "User created successfully.",
            result.Message);

        Assert.NotNull(result.Data);
        Assert.Equal(100, result.Data!.Id);
        Assert.Equal("newuser", result.Data.UserName);

        _repositoryMock.Verify(
            x => x.InsertAsync(
                It.Is<UserEntity>(u =>
                    u.UserName == "newuser" &&
                    u.Email == "new@test.com" &&
                    u.IsDeleted == false &&
                    !string.IsNullOrWhiteSpace(u.PasswordHash))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((UserEntity?)null);

        var dto = new UserUpdateDto
        {
            UserName = "updated",
            Email = "updated@test.com"
        };

        var result = await _service.UpdateAsync(999, dto);

        Assert.False(result.Success);
        Assert.Equal(
            "User not found.",
            result.Message);

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<int>(),
                It.IsAny<UserEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser_WhenUserExists()
    {
        var existing = CreateUser();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(x => x.UpdateAsync(
                1,
                It.IsAny<UserEntity>()))
            .ReturnsAsync(true);

        var dto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@test.com",
            FirstName = "Updated",
            LastName = "User",
            DisplayName = "Updated User",
            PhoneNumber = "8888888888",
            City = "Bangalore",
            State = "Karnataka",
            ZipCode = "560001",
            Country = "India",
            IsActive = true,
            UpdatedBy = "TEST"
        };

        var result = await _service.UpdateAsync(1, dto);

        Assert.True(result.Success);
        Assert.Equal(
            "User updated successfully.",
            result.Message);

        Assert.NotNull(result.Data);
        Assert.Equal("updateduser", result.Data!.UserName);
        Assert.Equal("updated@test.com", result.Data.Email);

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                1,
                It.Is<UserEntity>(u =>
                    u.UserName == "updateduser" &&
                    u.Email == "updated@test.com")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenRepositoryUpdateFails()
    {
        var existing = CreateUser();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(x => x.UpdateAsync(
                1,
                It.IsAny<UserEntity>()))
            .ReturnsAsync(false);

        var dto = new UserUpdateDto
        {
            UserName = "updateduser",
            Email = "updated@test.com"
        };

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.Success);
        Assert.Equal(
            "Unable to update user.",
            result.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((UserEntity?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.Success);
        Assert.Equal(
            "User not found.",
            result.Message);

        _repositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenDeleteSucceeds()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(CreateUser());

        _repositoryMock
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal(
            "User deleted successfully.",
            result.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenRepositoryDeleteFails()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(CreateUser());

        _repositoryMock
            .Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(false);

        var result = await _service.DeleteAsync(1);

        Assert.False(result.Success);
        Assert.False(result.Data);
        Assert.Equal(
            "Unable to delete user.",
            result.Message);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedUsers()
    {
        var filter = new UserFilterDto
        {
            UserName = "test",
            PageNumber = 1,
            PageSize = 10
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(filter))
            .ReturnsAsync(
                new PagedResultDto<UserEntity>
                {
                    Data = new List<UserEntity>
                    {
                        CreateUser(1),
                        CreateUser(2)
                    },
                    TotalRecords = 25
                });

        var result = await _service.GetPagedAsync(filter);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        Assert.Equal(
            25,
            result.Data!.TotalRecords);

        Assert.Equal(
            2,
            result.Data.Data.Count());

        _repositoryMock.Verify(
            x => x.GetPagedAsync(filter),
            Times.Once);
    }

    [Fact]
    public async Task ValidateUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("testuser"))
            .ReturnsAsync((UserEntity?)null);

        var result =
            await _service.ValidateUserAsync(
                "testuser",
                "Password123!");

        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.UpdateLastLoginAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateUserAsync_ShouldReturnNull_WhenUserIsInactive()
    {
        var user = CreateUser(isActive: false);

        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("testuser"))
            .ReturnsAsync(user);

        var result =
            await _service.ValidateUserAsync(
                "testuser",
                "Password123!");

        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.UpdateLastLoginAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateUserAsync_ShouldReturnNull_WhenUserIsDeleted()
    {
        var user = CreateUser(
            isActive: true,
            isDeleted: true);

        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("testuser"))
            .ReturnsAsync(user);

        var result =
            await _service.ValidateUserAsync(
                "testuser",
                "Password123!");

        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.UpdateLastLoginAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateUserAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        var user = CreateUser();

        var hasher =
            new Microsoft.AspNetCore.Identity.PasswordHasher<UserEntity>();

        user.PasswordHash =
            hasher.HashPassword(
                user,
                "CorrectPassword123!");

        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("testuser"))
            .ReturnsAsync(user);

        var result =
            await _service.ValidateUserAsync(
                "testuser",
                "WrongPassword123!");

        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.UpdateLastLoginAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateUserAsync_ShouldReturnUserAndUpdateLastLogin_WhenCredentialsAreValid()
    {
        var user = CreateUser();

        var hasher =
            new Microsoft.AspNetCore.Identity.PasswordHasher<UserEntity>();

        user.PasswordHash =
            hasher.HashPassword(
                user,
                "CorrectPassword123!");

        _repositoryMock
            .Setup(x => x.GetByUserNameAsync("testuser"))
            .ReturnsAsync(user);

        _repositoryMock
            .Setup(x => x.UpdateLastLoginAsync(1))
            .ReturnsAsync(true);

        var result =
            await _service.ValidateUserAsync(
                "testuser",
                "CorrectPassword123!");

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("testuser", result.UserName);

        _repositoryMock.Verify(
            x => x.UpdateLastLoginAsync(1),
            Times.Once);
    }
}

