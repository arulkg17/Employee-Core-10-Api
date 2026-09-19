using Asp.Versioning;
using Invoice.BAL.Contracts;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _service;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService service,
        ILogger<UserController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============================================================
    // GET: api/v1/User/GetAll
    // ============================================================
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var response = await _service.GetAllAsync();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Users");

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error retrieving Users",
                Error = ex.Message
            });
        }
    }

    // ============================================================
    // GET: api/v1/User/GetById/1
    // ============================================================
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var response = await _service.GetByIdAsync(id);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving User with Id {Id}",
                id);

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error retrieving User",
                Error = ex.Message
            });
        }
    }

    // ============================================================
    // POST: api/v1/User/Create
    // ============================================================
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] UserCreateDto dto)
    {
        try
        {
            var response = await _service.CreateAsync(dto);

            if (!response.Success)
            {
                return Conflict(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating User");

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error creating User",
                Error = ex.Message
            });
        }
    }

    // ============================================================
    // PUT: api/v1/User/Update/1
    // ============================================================
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UserUpdateDto dto)
    {
        try
        {
            var response = await _service.UpdateAsync(id, dto);

            if (!response.Success)
            {
                if (response.Message == "User not found.")
                {
                    return NotFound(response);
                }

                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating User with Id {Id}",
                id);

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error updating User",
                Error = ex.Message
            });
        }
    }

    // ============================================================
    // DELETE: api/v1/User/Delete/1
    // ============================================================
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var response = await _service.DeleteAsync(id);

            if (!response.Success)
            {
                if (response.Message == "User not found.")
                {
                    return NotFound(response);
                }

                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting User with Id {Id}",
                id);

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error deleting User",
                Error = ex.Message
            });
        }
    }

    // ============================================================
    // GET: api/v1/User/GetPaged
    // ============================================================
    [HttpGet("GetPaged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] UserFilterDto filter)
    {
        try
        {
            var response = await _service.GetPagedAsync(filter);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving paged Users");

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error retrieving Users",
                Error = ex.Message
            });
        }
    }

    // ============================================================
    // POST: api/v1/User/ValidateUser
    // ============================================================
    [AllowAnonymous]
    [HttpPost("ValidateUser")]
    public async Task<IActionResult> ValidateUser(
        [FromBody] UserLoginRequest request)
    {
        try
        {
            var user = await _service.ValidateUserAsync(
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

            return Ok(new
            {
                Success = true,
                Message = "User validated successfully.",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error validating User {UserName}",
                request.UserName);

            return StatusCode(500, new
            {
                Success = false,
                Message = "Error validating User",
                Error = ex.Message
            });
        }
    }
}