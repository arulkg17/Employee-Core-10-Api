using Asp.Versioning;
using Invoice.BAL.Contracts;
using Invoice.DTOs;
using Invoice.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _service;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ICategoryService service,
        ILogger<CategoryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ====================================================
    // GET: api/Category/GetAll
    // ====================================================

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<CategoryDto>>
            {
                Success = true,
                Message = "Categories retrieved successfully.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving categories.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error retrieving categories.",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ====================================================
    // GET: api/Category/GetById/{id}
    // ====================================================

    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Category not found."
                    });
            }

            return Ok(
                new ApiResponse<CategoryDto>
                {
                    Success = true,
                    Message = "Category retrieved successfully.",
                    Data = item
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving category with Id {CategoryId}.",
                id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error retrieving category.",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ====================================================
    // POST: api/Category/Create
    // ====================================================

    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] CategoryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Category data is required."
                    });
            }

            var id = await _service.AddAsync(dto);

            return Ok(
                new ApiResponse<int>
                {
                    Success = true,
                    Message = "Category created successfully.",
                    Data = id
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating category.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error creating category.",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ====================================================
    // PUT: api/Category/Update/{id}
    // ====================================================

    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CategoryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Category data is required."
                    });
            }

            dto.Id = id;

            var updated =
                await _service.UpdateAsync(dto);

            if (!updated)
            {
                return NotFound(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Category not found."
                    });
            }

            return Ok(
                new ApiResponse<string>
                {
                    Success = true,
                    Message = "Category updated successfully."
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating category with Id {CategoryId}.",
                id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error updating category.",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ====================================================
    // DELETE: api/Category/Delete/{id}
    // ====================================================

    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted =
                await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Category not found."
                    });
            }

            return Ok(
                new ApiResponse<string>
                {
                    Success = true,
                    Message = "Category deleted successfully."
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting category with Id {CategoryId}.",
                id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error deleting category.",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ====================================================
    // GET: api/Category/GetAllPaged
    // ====================================================

    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] string? Code,
        [FromQuery] string? Name,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1)
            {
                return BadRequest(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Page number must be greater than zero."
                    });
            }

            if (pageSize < 1)
            {
                return BadRequest(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Page size must be greater than zero."
                    });
            }

            var result =
                await _service.GetAllPagedAsync(
                    Code,
                    Name,
                    pageNumber,
                    pageSize);

            return Ok(
                new ApiResponse<IEnumerable<CategoryDto>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully.",
                    Data = result.Data,
                    TotalRecords = result.TotalRecords
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving paged categories.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error retrieving categories.",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }
    [HttpGet("TestException")]
    public IActionResult TestException()
    {
        throw new InvalidOperationException(
            "This is a test exception from Invoice CoreAPI.");
    }

}