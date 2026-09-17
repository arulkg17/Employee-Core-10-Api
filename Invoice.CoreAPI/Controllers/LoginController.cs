using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Invoice.BAL.Contracts;
using Invoice.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Invoice.CoreAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        IUserService userService,
        IConfiguration configuration,
        ILogger<LoginController> logger)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Username and password are required."
                });
            }

            // ----------------------------------------------------
            // Validate username and password through BAL
            // ----------------------------------------------------
            var user = await _userService.ValidateUserAsync(
                request.UserName,
                request.Password);

            if (user == null)
            {
                return Unauthorized(new
                {
                    Success = false,
                    Message = "Invalid username or password."
                });
            }

            // ----------------------------------------------------
            // Read JWT configuration
            // ----------------------------------------------------
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var jwtExpiryMinutes =
                _configuration.GetValue<int>(
                    "Jwt:ExpiryMinutes");

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT Key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(jwtIssuer))
            {
                throw new InvalidOperationException(
                    "JWT Issuer is not configured.");
            }

            if (string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException(
                    "JWT Audience is not configured.");
            }

            if (jwtExpiryMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "JWT ExpiryMinutes must be greater than zero.");
            }

            // ----------------------------------------------------
            // Create expiration
            // ----------------------------------------------------
            var expiration =
                DateTime.UtcNow.AddMinutes(jwtExpiryMinutes);

            // ----------------------------------------------------
            // Create JWT claims
            // ----------------------------------------------------
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.UserName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.GivenName,
                    user.FirstName),

                new Claim(
                    ClaimTypes.Surname,
                    user.LastName),

                new Claim(
                    "displayName",
                    user.DisplayName)
            };

            // ----------------------------------------------------
            // Create signing key
            // ----------------------------------------------------
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            // ----------------------------------------------------
            // Create token descriptor
            // ----------------------------------------------------
            var tokenDescriptor =
                new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),

                    Expires = expiration,

                    Issuer = jwtIssuer,

                    Audience = jwtAudience,

                    SigningCredentials = credentials
                };

            // ----------------------------------------------------
            // Generate JWT
            // ----------------------------------------------------
            var tokenHandler =
                new JwtSecurityTokenHandler();

            var token =
                tokenHandler.CreateToken(
                    tokenDescriptor);

            var tokenString =
                tokenHandler.WriteToken(token);

            // ----------------------------------------------------
            // Login response
            // ----------------------------------------------------
            var response = new LoginResponseDto
            {
                Token = tokenString,
                Expiration = expiration,
                User = user
            };

            return Ok(new
            {
                Success = true,
                Message = "Login successful.",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred during login.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    Success = false,
                    Message = "An error occurred during login."
                });
        }
    }
}
