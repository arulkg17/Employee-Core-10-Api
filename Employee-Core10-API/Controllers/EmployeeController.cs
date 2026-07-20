using Employee_Core10_API.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Core10_API.Controllers;

[ApiController]
[Route("api/[Controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    { 
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var employeeResult = await _employeeService.GetAllAsync();
        return Ok(employeeResult);
    }
}
