using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Invoice.CoreAPI.Test;

public class LoginControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<LoginController>> _loggerMock;

    public LoginControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<LoginController>>();
    }

    private static IConfiguration CreateConfiguration(
        string? jwtKey = null,
        string? issuer = null,
        string? audience = null,
        int expiryMinutes = 120)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = jwtKey ??
                "v2UJQxTrwUCqqJkehkxvSUZKQCX6gNmRWq7q1bWa3Jw=",

            ["Jwt:Issuer"] = issuer ?? "Invoice.Api",

            ["Jwt:Audience"] = audience ?? "Invoice.Api",

            ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString()
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private LoginController CreateController(
        IConfiguration? configuration = null)
    {
        configuration ??= CreateConfiguration();

        return new LoginController(
            _userServiceMock.Object,
            configuration,
            _loggerMock.Object);
    }


    // ============================================================
    // 1. VALID LOGIN
    // ============================================================

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        var user = new UserDto
        {
            Id = 1,
            UserName = "admin",
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            DisplayName = "Admin User",
            PhoneNumber = "1234567890",
            AddressLine1 = "Test Address",
            City = "Test City",
            State = "Test State",
            ZipCode = "12345",
            Country = "USA",
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        var controller = CreateController();

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var responseType = okResult.Value.GetType();

        var successProperty =
            responseType.GetProperty("Success");

        var messageProperty =
            responseType.GetProperty("Message");

        var dataProperty =
            responseType.GetProperty("Data");

        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);
        Assert.NotNull(dataProperty);

        Assert.True(
            (bool)successProperty.GetValue(okResult.Value)!);

        Assert.Equal(
            "Login successful.",
            messageProperty.GetValue(okResult.Value));

        var loginResponse =
            Assert.IsType<LoginResponseDto>(
                dataProperty.GetValue(okResult.Value));

        Assert.NotNull(loginResponse.Token);
        Assert.NotEmpty(loginResponse.Token);

        Assert.Equal(
            user.Id,
            loginResponse.User.Id);

        Assert.Equal(
            user.UserName,
            loginResponse.User.UserName);

        _userServiceMock.Verify(
            x => x.ValidateUserAsync(
                request.UserName,
                request.Password),
            Times.Once);
    }


    // ============================================================
    // 2. INVALID LOGIN
    // ============================================================

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "wrong-password"
        };

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync((UserDto?)null);

        var controller = CreateController();

        // Act
        var result = await controller.Login(request);

        // Assert
        var unauthorizedResult =
            Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.NotNull(unauthorizedResult.Value);

        var responseType =
            unauthorizedResult.Value.GetType();

        var successProperty =
            responseType.GetProperty("Success");

        var messageProperty =
            responseType.GetProperty("Message");

        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);

        Assert.False(
            (bool)successProperty.GetValue(
                unauthorizedResult.Value)!);

        Assert.Equal(
            "Invalid username or password.",
            messageProperty.GetValue(
                unauthorizedResult.Value));

        _userServiceMock.Verify(
            x => x.ValidateUserAsync(
                request.UserName,
                request.Password),
            Times.Once);
    }


    // ============================================================
    // 3. EMPTY USERNAME
    // ============================================================

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenUsernameIsEmpty()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "",
            Password = "password"
        };

        var controller = CreateController();

        // Act
        var result = await controller.Login(request);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badRequestResult.Value);

        _userServiceMock.Verify(
            x => x.ValidateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }


    // ============================================================
    // 4. EMPTY PASSWORD
    // ============================================================

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenPasswordIsEmpty()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = ""
        };

        var controller = CreateController();

        // Act
        var result = await controller.Login(request);

        // Assert
        var badRequestResult =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.NotNull(badRequestResult.Value);

        _userServiceMock.Verify(
            x => x.ValidateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }


    // ============================================================
    // 5. USER SERVICE EXCEPTION
    // ============================================================

    [Fact]
    public async Task Login_Returns500_WhenUserServiceThrowsException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ThrowsAsync(
                new Exception("Database connection failed."));

        var controller = CreateController();

        // Act
        var result = await controller.Login(request);

        // Assert
        var statusResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusResult.StatusCode);

        Assert.NotNull(statusResult.Value);
    }


    // ============================================================
    // 6. JWT KEY MISSING
    // ============================================================

    [Fact]
    public async Task Login_Returns500_WhenJwtKeyIsMissing()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        var user = CreateTestUser();

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        var configuration = CreateConfiguration(
            jwtKey: "");

        var controller = CreateController(configuration);

        // Act
        var result = await controller.Login(request);

        // Assert
        var statusResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusResult.StatusCode);
    }


    // ============================================================
    // 7. JWT ISSUER MISSING
    // ============================================================

    [Fact]
    public async Task Login_Returns500_WhenJwtIssuerIsMissing()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        var user = CreateTestUser();

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        var configuration = CreateConfiguration(
            issuer: "");

        var controller = CreateController(configuration);

        // Act
        var result = await controller.Login(request);

        // Assert
        var statusResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusResult.StatusCode);
    }


    // ============================================================
    // 8. JWT AUDIENCE MISSING
    // ============================================================

    [Fact]
    public async Task Login_Returns500_WhenJwtAudienceIsMissing()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        var user = CreateTestUser();

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        var configuration = CreateConfiguration(
            audience: "");

        var controller = CreateController(configuration);

        // Act
        var result = await controller.Login(request);

        // Assert
        var statusResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusResult.StatusCode);
    }


    // ============================================================
    // 9. INVALID JWT EXPIRATION
    // ============================================================

    [Fact]
    public async Task Login_Returns500_WhenJwtExpiryIsInvalid()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        var user = CreateTestUser();

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        var configuration = CreateConfiguration(
            expiryMinutes: 0);

        var controller = CreateController(configuration);

        // Act
        var result = await controller.Login(request);

        // Assert
        var statusResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusResult.StatusCode);
    }

// ============================================================
// 10. JWT CLAIMS
// ============================================================

[Fact]
public async Task Login_GeneratesJwtWithExpectedClaims()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            UserName = "admin",
            Password = "password"
        };

        var user = CreateTestUser();

        _userServiceMock
            .Setup(x => x.ValidateUserAsync(
                request.UserName,
                request.Password))
            .ReturnsAsync(user);

        var configuration = CreateConfiguration();

        var controller = CreateController(configuration);

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var responseType =
            okResult.Value!.GetType();

        var dataProperty =
            responseType.GetProperty("Data");

        var loginResponse =
            Assert.IsType<LoginResponseDto>(
                dataProperty!.GetValue(okResult.Value));

        Assert.NotEmpty(loginResponse.Token);

        // --------------------------------------------------------
        // Read generated JWT
        // --------------------------------------------------------
        var handler = new JwtSecurityTokenHandler();

        var token =
            handler.ReadJwtToken(loginResponse.Token);

        // --------------------------------------------------------
        // Validate standard JWT information
        // --------------------------------------------------------
        Assert.Equal(
            "Invoice.Api",
            token.Issuer);

        Assert.Contains(
            "Invoice.Api",
            token.Audiences);

        // --------------------------------------------------------
        // Validate JWT claims
        //
        // ClaimTypes.* are converted to JWT claim names when
        // the token is generated.
        // --------------------------------------------------------

        // ClaimTypes.NameIdentifier -> "nameid"
        Assert.Equal(
            user.Id.ToString(),
            token.Claims.First(
                x => x.Type == "nameid").Value);

        // ClaimTypes.Name -> "unique_name"
        Assert.Equal(
            user.UserName,
            token.Claims.First(
                x => x.Type == "unique_name").Value);

        // ClaimTypes.Email -> "email"
        Assert.Equal(
            user.Email,
            token.Claims.First(
                x => x.Type == "email").Value);

        // ClaimTypes.GivenName -> "given_name"
        Assert.Equal(
            user.FirstName,
            token.Claims.First(
                x => x.Type == "given_name").Value);

        // ClaimTypes.Surname -> "family_name"
        Assert.Equal(
            user.LastName,
            token.Claims.First(
                x => x.Type == "family_name").Value);

        // Custom claim remains unchanged
        Assert.Equal(
            user.DisplayName,
            token.Claims.First(
                x => x.Type == "displayName").Value);

        // --------------------------------------------------------
        // Validate expiration
        // --------------------------------------------------------
        Assert.True(
            token.ValidTo > DateTime.UtcNow);
    }


    // ============================================================
    // TEST USER HELPER
    // ============================================================

    private static UserDto CreateTestUser()
    {
        return new UserDto
        {
            Id = 1,
            UserName = "admin",
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            DisplayName = "Admin User",
            PhoneNumber = "1234567890",
            AddressLine1 = "Test Address",
            City = "Test City",
            State = "Test State",
            ZipCode = "12345",
            Country = "USA",
            IsActive = true
        };
    }
}