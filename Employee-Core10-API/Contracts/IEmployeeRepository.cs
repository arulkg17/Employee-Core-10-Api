using Employee_Core10_API.Entities;

namespace Employee_Core10_API.Contracts;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
}
