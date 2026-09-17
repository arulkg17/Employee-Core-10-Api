using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface ICategoryService
{
    Task<int> AddAsync(CategoryDto category);
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(CategoryDto category);
    Task<bool> DeleteAsync(int id);

    Task<PagedResultDto<CategoryDto>> GetAllPagedAsync(
        string? Code,
        string? Name,
        int pageNumber,
        int pageSize);
}
