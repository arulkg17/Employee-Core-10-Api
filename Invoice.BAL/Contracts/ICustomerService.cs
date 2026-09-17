using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface ICustomerService
{
    Task<int> AddAsync(CustomerDto customer);
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(CustomerDto customer);
    Task<bool> DeleteAsync(int id);
    Task<PagedResultDto<CustomerDto>> GetAllPagedAsync(
        string? CustomerCode,
        string? CustomerName,
        string? MobileNo,
        string? City,
        int PageNumber,
        int PageSize
        );
}