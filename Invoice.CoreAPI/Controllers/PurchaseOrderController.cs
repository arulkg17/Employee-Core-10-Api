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
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _service;
    private readonly ILogger<PurchaseOrderController> _logger;

    public PurchaseOrderController(
        IPurchaseOrderService service,
        ILogger<PurchaseOrderController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============================================================
    // GET: api/v1/PurchaseOrder/GetAll
    // ============================================================
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data =
                await _service.GetAllAsync();

            return Ok(
                new ApiResponse<IEnumerable<PurchaseOrderDto>>
                {
                    Success = true,
                    Message =
                        "Purchase Orders retrieved successfully",
                    Data = data
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Purchase Orders");

            return StatusCode(
                500,
                new ApiResponse<string>
                {
                    Success = false,
                    Message =
                        "Error retrieving Purchase Orders",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ============================================================
    // GET: api/v1/PurchaseOrder/GetById/1
    // ============================================================
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var data =
                await _service.GetByIdAsync(id);

            if (data == null)
            {
                return NotFound(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message =
                            "Purchase Order not found"
                    });
            }

            return Ok(
                new ApiResponse<PurchaseOrderDto>
                {
                    Success = true,
                    Message =
                        "Purchase Order retrieved successfully",
                    Data = data
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Purchase Order with Id {Id}",
                id);

            return StatusCode(
                500,
                new ApiResponse<string>
                {
                    Success = false,
                    Message =
                        "Error retrieving Purchase Order",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ============================================================
    // POST: api/v1/PurchaseOrder/Create
    // ============================================================
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] PurchaseOrderDto dto)
    {
        try
        {
            var id =
                await _service.AddAsync(dto);

            return Ok(
                new ApiResponse<int>
                {
                    Success = true,
                    Message =
                        "Purchase Order created successfully",
                    Data = id
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating Purchase Order");

            return StatusCode(
                500,
                new ApiResponse<string>
                {
                    Success = false,
                    Message =
                        "Error creating Purchase Order",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ============================================================
    // PUT: api/v1/PurchaseOrder/Update/1
    // ============================================================
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PurchaseOrderDto dto)
    {
        try
        {
            dto.Id = id;

            var updated =
                await _service.UpdateAsync(dto);

            if (!updated)
            {
                return NotFound(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message =
                            "Purchase Order not found"
                    });
            }

            return Ok(
                new ApiResponse<string>
                {
                    Success = true,
                    Message =
                        "Purchase Order updated successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating Purchase Order with Id {Id}",
                id);

            return StatusCode(
                500,
                new ApiResponse<string>
                {
                    Success = false,
                    Message =
                        "Error updating Purchase Order",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ============================================================
    // DELETE: api/v1/PurchaseOrder/Delete/1
    // ============================================================
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
                        Message =
                            "Purchase Order not found"
                    });
            }

            return Ok(
                new ApiResponse<string>
                {
                    Success = true,
                    Message =
                        "Purchase Order deleted successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting Purchase Order with Id {Id}",
                id);

            return StatusCode(
                500,
                new ApiResponse<string>
                {
                    Success = false,
                    Message =
                        "Error deleting Purchase Order",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }

    // ============================================================
    // GET: api/v1/PurchaseOrder/GetAllPaged
    // ============================================================
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? PONumber,
        int? VendorId,
        string? Status,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            var result =
                await _service.GetAllPagedAsync(
                    PONumber,
                    VendorId,
                    Status,
                    pageNumber,
                    pageSize);

            return Ok(
                new ApiResponse<PagedResultDto<PurchaseOrderDto>>
                {
                    Success = true,
                    Message =
                        "Purchase Orders retrieved successfully",
                    Data = result
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving paged Purchase Orders");

            return StatusCode(
                500,
                new ApiResponse<string>
                {
                    Success = false,
                    Message =
                        "Error retrieving Purchase Orders",
                    Error = new ApiError
                    {
                        Code = "500",
                        Details = ex.Message
                    }
                });
        }
    }
}