using Asp.Versioning;
using Invoice.BAL.Contracts;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Invoice.CoreAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _service;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        ICustomerService service,
        ILogger<CustomerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============================================================
    // GET: api/v1/Customer/GetAll
    // ============================================================

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<CustomerDto>>
            {
                Success = true,
                Message = "Customers retrieved successfully",
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Customers");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Customer",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }


    // ============================================================
    // GET: api/v1/Customer/GetById/1
    // ============================================================

    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var customer = await _service.GetByIdAsync(id);

            if (customer == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Customer not found"
                });
            }

            return Ok(new ApiResponse<CustomerDto>
            {
                Success = true,
                Message = "Customer retrieved successfully",
                Data = customer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Customer with Id {Id}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Customer",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }


    // ============================================================
    // POST: api/v1/Customer/Create
    // ============================================================

    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] CustomerDto dto)
    {
        try
        {
            var id = await _service.AddAsync(dto);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = "Customer created successfully",
                Data = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating Customer");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error creating Customer",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }


    // ============================================================
    // PUT: api/v1/Customer/Update/1
    // ============================================================

    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CustomerDto dto)
    {
        try
        {
            // Route Id is authoritative
            dto.Id = id;

            var updated = await _service.UpdateAsync(dto);

            if (!updated)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Customer not found"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Customer updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating Customer with Id {Id}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error updating Customer",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }


    // ============================================================
    // DELETE: api/v1/Customer/Delete/1
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
                    Message = "Customer not found"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Customer deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting Customer with Id {Id}",
                id);

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error deleting Customer",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }


    // ============================================================
    // GET:
    // api/v1/Customer/GetAllPaged
    //
    // Example:
    // ?CustomerCode=C001
    // &CustomerName=ABC
    // &MobileNo=123
    // &City=Chennai
    // &pageNumber=1
    // &pageSize=10
    // ============================================================

    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? CustomerCode,
        string? CustomerName,
        string? MobileNo,
        string? City,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            var result = await _service.GetAllPagedAsync(
                CustomerCode,
                CustomerName,
                MobileNo,
                City,
                pageNumber,
                pageSize);

            return Ok(new ApiResponse<PagedResultDto<CustomerDto>>
            {
                Success = true,
                Message = "Customers retrieved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving paged Customers");

            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Error retrieving Customer",
                Error = new ApiError
                {
                    Code = "500",
                    Details = ex.Message
                }
            });
        }
    }
}