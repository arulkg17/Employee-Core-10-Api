using Asp.Versioning;
using Invoice.BAL.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;

/// <summary>
/// Read-only count endpoints over IMasterCountService - the same rules Invoice.AI uses.
/// The MCP server calls these instead of downloading whole tables and counting itself.
/// isActive: true = active only, false = inactive only, omit = all (deleted rows never count).
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class MasterCountController : ApiControllerBase
{
    private readonly IMasterCountService _service;
    private readonly ILogger<MasterCountController> _logger;

    public MasterCountController(
        IMasterCountService service,
        ILogger<MasterCountController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/v1/MasterCount/Customers?isActive=true
    [HttpGet("Customers")]
    public Task<IActionResult> Customers([FromQuery] bool? isActive) =>
        RunAsync(async () =>
            Ok(Done("Customer count retrieved successfully", await _service.CountCustomersAsync(isActive))),
            _logger, "Error retrieving customer count");

    // GET: api/v1/MasterCount/CustomersByCity?isActive=true
    [HttpGet("CustomersByCity")]
    public Task<IActionResult> CustomersByCity([FromQuery] bool? isActive) =>
        RunAsync(async () =>
            Ok(Done("Customer counts by city retrieved successfully", await _service.CountCustomersGroupedByCityAsync(isActive))),
            _logger, "Error retrieving customer counts by city");

    // GET: api/v1/MasterCount/Vendors?isActive=true
    [HttpGet("Vendors")]
    public Task<IActionResult> Vendors([FromQuery] bool? isActive) =>
        RunAsync(async () =>
            Ok(Done("Vendor count retrieved successfully", await _service.CountVendorsAsync(isActive))),
            _logger, "Error retrieving vendor count");

    // GET: api/v1/MasterCount/Categories?isActive=true
    [HttpGet("Categories")]
    public Task<IActionResult> Categories([FromQuery] bool? isActive) =>
        RunAsync(async () =>
            Ok(Done("Category count retrieved successfully", await _service.CountCategoriesAsync(isActive))),
            _logger, "Error retrieving category count");

    // GET: api/v1/MasterCount/Items?categoryId=1&isActive=true
    [HttpGet("Items")]
    public Task<IActionResult> Items([FromQuery] int? categoryId, [FromQuery] bool? isActive) =>
        RunAsync(async () =>
            Ok(Done("Item count retrieved successfully", await _service.CountItemsAsync(categoryId, isActive))),
            _logger, "Error retrieving item count");

    // Users are deliberately NOT exposed here; Invoice.AI reads the user count through BAL directly.
}
