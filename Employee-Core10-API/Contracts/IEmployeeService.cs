using Employee_Core10_API.DTOs;

namespace Employee_Core10_API.Contracts;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
}
