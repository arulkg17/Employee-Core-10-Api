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
public class ItemmasterController : ControllerBase
{
    private readonly IItemmasterService _service;
    private readonly ILogger<ItemmasterController> _logger;

    public ItemmasterController(
        IItemmasterService service,
        ILogger<ItemmasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/v1/Itemmaster/GetAllPaged
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] ItemmasterFilterDto search)
    {
        try
        {
            var result = await _service.GetAllPagedAsync(search);

            return Ok(new ApiResponse<PagedResultDto<ItemmasterDto>>
            {
                Success = true,
                Message = "Items retrieved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving paged Itemmaster records");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving items",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // GET: api/v1/Itemmaster/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<ItemmasterDto>>
            {
                Success = true,
                Message = "Items retrieved successfully",
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Itemmaster records");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Itemmaster",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // GET: api/v1/Itemmaster/GetById/1
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Item not found"
                });
            }

            return Ok(new ApiResponse<ItemmasterDto>
            {
                Success = true,
                Message = "Item retrieved successfully",
                Data = item
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Itemmaster with Id {ItemId}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving item",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // POST: api/v1/Itemmaster/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] ItemmasterDto dto)
    {
        try
        {
            var id = await _service.AddAsync(dto);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = "Item created successfully",
                Data = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating Itemmaster");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error creating item",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // PUT: api/v1/Itemmaster/Update/1
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ItemmasterDto dto)
    {
        try
        {
            dto.Id = id;

            var updated = await _service.UpdateAsync(dto);

            if (!updated)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Item not found"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Item updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating Itemmaster with Id {ItemId}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error updating item",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // DELETE: api/v1/Itemmaster/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Item not found"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Item deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting Itemmaster with Id {ItemId}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error deleting item",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // GET: api/v1/Itemmaster/GetActiveItemCountByCategory/1
    [HttpGet("GetActiveItemCountByCategory/{categoryId:int}")]
    public async Task<IActionResult> GetActiveItemCountByCategory(
        int categoryId)
    {
        try
        {
            var count =
                await _service.GetActiveItemCountByCategoryAsync(categoryId);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = "Active item count retrieved successfully",
                Data = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving active item count for CategoryId {CategoryId}",
                categoryId);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving active item count",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // GET: api/v1/Itemmaster/TestException
    [HttpGet("TestException")]
    public IActionResult TestException()
    {
        throw new Exception("This is a test exception");
    }
}
