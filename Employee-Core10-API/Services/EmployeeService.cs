using Employee_Core10_API.Contracts;
using Employee_Core10_API.DTOs;

namespace Employee_Core10_API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository; 

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    { 
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            Name = e.Name,
            Department = e.Department,
            Salary = e.Salary,
            CreatedDate = e.CreatedDate
        });

    }
}
