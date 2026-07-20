using Employee_Core10_API.Contracts;
using Employee_Core10_API.Data;
using Employee_Core10_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Employee_Core10_API.Repositories;

public class EmployeeRepository : IEmployeeRepository   
{
    private readonly AppDBContext __context;

    public EmployeeRepository(AppDBContext context)
    { 
        __context = context;
    }
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        try
        {

        
        return await __context.Employee
                    .OrderBy(e=>e.Id)
                    .ToListAsync();
        }
        catch (Exception )
        {

            throw ;
        }
    }
}
