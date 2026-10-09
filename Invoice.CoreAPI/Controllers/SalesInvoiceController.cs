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
public class SalesInvoiceController : ApiControllerBase
{
    private readonly ISalesInvoiceService _service;
    private readonly ILogger<SalesInvoiceController> _logger;

    public SalesInvoiceController(
        ISalesInvoiceService service,
        ILogger<SalesInvoiceController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/v1/SalesInvoice/GetAll
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll() =>
        RunAsync(async () =>
        {
            var data = await _service.GetAllAsync();
            return Ok(Done("Sales invoices retrieved successfully", data));
        }, _logger, "Error retrieving Sales Invoices");

    // GET: api/v1/SalesInvoice/GetById/1
    [HttpGet("GetById/{id:int}")]
    public Task<IActionResult> GetById(int id) =>
        RunAsync(async () =>
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(Fail("Sales invoice not found", "404", "Sales invoice not found"));

            return Ok(Done("Sales invoice retrieved successfully", data));
        }, _logger, "Error retrieving Sales Invoice");

    // POST: api/v1/SalesInvoice/Create
    [HttpPost("Create")]
    public Task<IActionResult> Create([FromBody] SalesInvoiceDto dto) =>
        RunAsync(async () =>
        {
            dto.CreatedBy = CurrentUser;

            var id = await _service.AddAsync(dto);

            return Ok(Done("Sales invoice created successfully", id));
        }, _logger, "Error creating Sales Invoice");

    // PUT: api/v1/SalesInvoice/Update/1
    [HttpPut("Update/{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] SalesInvoiceDto dto) =>
        RunAsync(async () =>
        {
            dto.Id = id;
            dto.UpdatedBy = CurrentUser;

            var updated = await _service.UpdateAsync(dto);

            if (!updated)
                return NotFound(Fail("Sales invoice not found", "404", "Sales invoice not found"));

            return Ok(Done("Sales invoice updated successfully"));
        }, _logger, "Error updating Sales Invoice");

    // DELETE: api/v1/SalesInvoice/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public Task<IActionResult> Delete(int id) =>
        RunAsync(async () =>
        {
            var deleted = await _service.DeleteAsync(id, CurrentUser);

            if (!deleted)
                return NotFound(Fail("Sales invoice not found", "404", "Sales invoice not found"));

            return Ok(Done("Sales invoice deleted successfully"));
        }, _logger, "Error deleting Sales Invoice");

    // GET: api/v1/SalesInvoice/GetAllPaged
    [HttpGet("GetAllPaged")]
    public Task<IActionResult> GetAllPaged(
        string? invoiceNumber,
        int? customerId,
        string? status,
        int pageNumber = 1,
        int pageSize = 10) =>
        RunAsync(async () =>
        {
            var result = await _service.GetAllPagedAsync(
                invoiceNumber, customerId, status, pageNumber, pageSize);

            return Ok(Done("Sales invoices retrieved successfully", result));
        }, _logger, "Error retrieving paged Sales Invoices");

    // POST: api/v1/SalesInvoice/Post/1   (Draft -> Posted, checks and reduces stock)
    [HttpPost("Post/{id:int}")]
    public Task<IActionResult> PostInvoice(int id) =>
        RunAsync(async () =>
        {
            await _service.PostAsync(id, CurrentUser);
            return Ok(Done("Sales invoice posted successfully"));
        }, _logger, "Error posting Sales Invoice");

    // POST: api/v1/SalesInvoice/Cancel/1   (stock is returned)
    [HttpPost("Cancel/{id:int}")]
    public Task<IActionResult> Cancel(int id) =>
        RunAsync(async () =>
        {
            await _service.CancelAsync(id, CurrentUser);
            return Ok(Done("Sales invoice cancelled successfully"));
        }, _logger, "Error cancelling Sales Invoice");
}
