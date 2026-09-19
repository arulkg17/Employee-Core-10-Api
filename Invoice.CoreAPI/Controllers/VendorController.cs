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
public class VendorController : ControllerBase
{
    private readonly IVendorService _service;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        IVendorService service,
        ILogger<VendorController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============================================================
    // GET: api/v1/Vendor/GetAll
    // ============================================================
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<VendorDto>>
            {
                Success = true,
                Message = "Vendors retrieved successfully",
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Vendors");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Vendor",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // ============================================================
    // GET: api/v1/Vendor/GetById/1
    // ============================================================
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var vendor = await _service.GetByIdAsync(id);

            if (vendor == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Vendor not found"
                });
            }

            return Ok(new ApiResponse<VendorDto>
            {
                Success = true,
                Message = "Vendor retrieved successfully",
                Data = vendor
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Vendor with Id {Id}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Vendor",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // ============================================================
    // POST: api/v1/Vendor/Create
    // ============================================================
    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] VendorDto dto)
    {
        try
        {
            var id = await _service.AddAsync(dto);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = "Vendor created successfully",
                Data = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating Vendor");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error creating Vendor",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // ============================================================
    // PUT: api/v1/Vendor/Update/1
    // ============================================================
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] VendorDto dto)
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
                    Message = "Vendor not found"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Vendor updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating Vendor with Id {Id}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error updating Vendor",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // ============================================================
    // DELETE: api/v1/Vendor/Delete/1
    // ============================================================
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
                    Message = "Vendor not found"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Vendor deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting Vendor with Id {Id}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error deleting Vendor",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }

    // ============================================================
    // GET: api/v1/Vendor/GetAllPaged
    // ============================================================
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? VendorCode,
        string? VendorName,
        string? MobileNo,
        string? City,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            var result = await _service.GetAllPagedAsync(
                VendorCode,
                VendorName,
                MobileNo,
                City,
                pageNumber,
                pageSize);

            return Ok(new ApiResponse<PagedResultDto<VendorDto>>
            {
                Success = true,
                Message = "Vendors retrieved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving paged Vendors");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Vendor",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }
}
