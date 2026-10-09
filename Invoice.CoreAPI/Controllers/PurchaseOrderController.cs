using Asp.Versioning;
using Invoice.BAL.Contracts;
using Invoice.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class PurchaseOrderController : ApiControllerBase
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

    // GET: api/v1/PurchaseOrder/GetAll
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll() =>
        RunAsync(async () =>
        {
            var data = await _service.GetAllAsync();
            return Ok(Done("Purchase Orders retrieved successfully", data));
        }, _logger, "Error retrieving Purchase Orders");

    // GET: api/v1/PurchaseOrder/GetById/1
    [HttpGet("GetById/{id:int}")]
    public Task<IActionResult> GetById(int id) =>
        RunAsync(async () =>
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(Fail("Purchase Order not found", "404", "Purchase Order not found"));

            return Ok(Done("Purchase Order retrieved successfully", data));
        }, _logger, "Error retrieving Purchase Order");

    // POST: api/v1/PurchaseOrder/Create
    [HttpPost("Create")]
    public Task<IActionResult> Create([FromBody] PurchaseOrderDto dto) =>
        RunAsync(async () =>
        {
            dto.CreatedBy = CurrentUser;

            var id = await _service.AddAsync(dto);

            return Ok(Done("Purchase Order created successfully", id));
        }, _logger, "Error creating Purchase Order");

    // PUT: api/v1/PurchaseOrder/Update/1
    [HttpPut("Update/{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] PurchaseOrderDto dto) =>
        RunAsync(async () =>
        {
            dto.Id = id;
            dto.UpdatedBy = CurrentUser;

            var updated = await _service.UpdateAsync(dto);

            if (!updated)
                return NotFound(Fail("Purchase Order not found", "404", "Purchase Order not found"));

            return Ok(Done("Purchase Order updated successfully"));
        }, _logger, "Error updating Purchase Order");

    // DELETE: api/v1/PurchaseOrder/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public Task<IActionResult> Delete(int id) =>
        RunAsync(async () =>
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound(Fail("Purchase Order not found", "404", "Purchase Order not found"));

            return Ok(Done("Purchase Order deleted successfully"));
        }, _logger, "Error deleting Purchase Order");

    // GET: api/v1/PurchaseOrder/GetAllPaged
    [HttpGet("GetAllPaged")]
    public Task<IActionResult> GetAllPaged(
        string? PONumber,
        int? VendorId,
        string? Status,
        int pageNumber = 1,
        int pageSize = 10) =>
        RunAsync(async () =>
        {
            var result = await _service.GetAllPagedAsync(
                PONumber, VendorId, Status, pageNumber, pageSize);

            return Ok(Done("Purchase Orders retrieved successfully", result));
        }, _logger, "Error retrieving paged Purchase Orders");

    // POST: api/v1/PurchaseOrder/Approve/1   (Draft -> Approved)
    [HttpPost("Approve/{id:int}")]
    public Task<IActionResult> Approve(int id) =>
        RunAsync(async () =>
        {
            await _service.ApproveAsync(id, CurrentUser);
            return Ok(Done("Purchase Order approved successfully"));
        }, _logger, "Error approving Purchase Order");

    // POST: api/v1/PurchaseOrder/Cancel/1
    [HttpPost("Cancel/{id:int}")]
    public Task<IActionResult> Cancel(int id) =>
        RunAsync(async () =>
        {
            await _service.CancelAsync(id, CurrentUser);
            return Ok(Done("Purchase Order cancelled successfully"));
        }, _logger, "Error cancelling Purchase Order");
}
