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
public class ReceiptController : ApiControllerBase
{
    private readonly IReceiptService _service;
    private readonly ILogger<ReceiptController> _logger;

    public ReceiptController(IReceiptService service, ILogger<ReceiptController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/v1/Receipt/GetAll
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll() =>
        RunAsync(async () =>
        {
            var data = await _service.GetAllAsync();
            return Ok(Done("Receipts retrieved successfully", data));
        }, _logger, "Error retrieving Receipts");

    // GET: api/v1/Receipt/GetById/1
    [HttpGet("GetById/{id:int}")]
    public Task<IActionResult> GetById(int id) =>
        RunAsync(async () =>
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(Fail("Receipt not found", "404", "Receipt not found"));

            return Ok(Done("Receipt retrieved successfully", data));
        }, _logger, "Error retrieving Receipt");

    // POST: api/v1/Receipt/Create
    [HttpPost("Create")]
    public Task<IActionResult> Create([FromBody] ReceiptDto dto) =>
        RunAsync(async () =>
        {
            dto.CreatedBy = CurrentUser;

            var id = await _service.AddAsync(dto);

            return Ok(Done("Receipt created successfully", id));
        }, _logger, "Error creating Receipt");

    // PUT: api/v1/Receipt/Update/1
    [HttpPut("Update/{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] ReceiptDto dto) =>
        RunAsync(async () =>
        {
            dto.Id = id;
            dto.UpdatedBy = CurrentUser;

            var updated = await _service.UpdateAsync(dto);

            if (!updated)
                return NotFound(Fail("Receipt not found", "404", "Receipt not found"));

            return Ok(Done("Receipt updated successfully"));
        }, _logger, "Error updating Receipt");

    // DELETE: api/v1/Receipt/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public Task<IActionResult> Delete(int id) =>
        RunAsync(async () =>
        {
            var deleted = await _service.DeleteAsync(id, CurrentUser);

            if (!deleted)
                return NotFound(Fail("Receipt not found", "404", "Receipt not found"));

            return Ok(Done("Receipt deleted successfully"));
        }, _logger, "Error deleting Receipt");

    // GET: api/v1/Receipt/GetAllPaged
    [HttpGet("GetAllPaged")]
    public Task<IActionResult> GetAllPaged(
        string? receiptNumber,
        int? purchaseOrderId,
        int? vendorId,
        string? status,
        int pageNumber = 1,
        int pageSize = 10) =>
        RunAsync(async () =>
        {
            var result = await _service.GetAllPagedAsync(
                receiptNumber, purchaseOrderId, vendorId, status, pageNumber, pageSize);

            return Ok(Done("Receipts retrieved successfully", result));
        }, _logger, "Error retrieving paged Receipts");

    // POST: api/v1/Receipt/Post/1   (Draft -> Posted, updates PO received quantities and stock)
    [HttpPost("Post/{id:int}")]
    public Task<IActionResult> PostReceipt(int id) =>
        RunAsync(async () =>
        {
            await _service.PostAsync(id, CurrentUser);
            return Ok(Done("Receipt posted successfully"));
        }, _logger, "Error posting Receipt");

    // POST: api/v1/Receipt/Cancel/1
    [HttpPost("Cancel/{id:int}")]
    public Task<IActionResult> Cancel(int id) =>
        RunAsync(async () =>
        {
            await _service.CancelAsync(id, CurrentUser);
            return Ok(Done("Receipt cancelled successfully"));
        }, _logger, "Error cancelling Receipt");
}
