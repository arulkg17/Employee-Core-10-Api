using Asp.Versioning;
using Invoice.BAL.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class StockController : ApiControllerBase
{
    private readonly IStockService _service;
    private readonly ILogger<StockController> _logger;

    public StockController(IStockService service, ILogger<StockController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/v1/Stock/GetAll
    [HttpGet("GetAll")]
    public Task<IActionResult> GetAll() =>
        RunAsync(async () =>
        {
            var data = await _service.GetAllAsync();
            return Ok(Done("Stock retrieved successfully", data));
        }, _logger, "Error retrieving Stock");

    // GET: api/v1/Stock/GetByItem/1
    [HttpGet("GetByItem/{itemmasterId:int}")]
    public Task<IActionResult> GetByItem(int itemmasterId) =>
        RunAsync(async () =>
        {
            var data = await _service.GetByItemmasterIdAsync(itemmasterId);

            if (data == null)
                return NotFound(Fail("Item not found", "404", "Item not found"));

            return Ok(Done("Stock retrieved successfully", data));
        }, _logger, "Error retrieving Stock");
}
