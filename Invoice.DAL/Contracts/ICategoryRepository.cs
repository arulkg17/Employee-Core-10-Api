
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model.AI;

namespace Invoice.DAL.Contracts;

public interface ICategoryRepository
{
    Task<int> AddAsync(CategoryEntity category);
    Task<IEnumerable<CategoryEntity>> GetAllAsync();
    Task<CategoryEntity?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(CategoryEntity category);
    Task<bool> DeleteAsync(int id);
    Task<PagedResultDto<CategoryEntity>> GetAllPagedAsync(
    string? Code,
    string? Name,
    int pageNumber,
    int pageSize);
    Task<CategoryEntity?> GetByNameAsync(string name);
    Task<CategoryItemCountResult?> GetCategoryItemCountAsync(
    string categoryName,
    bool categoryActiveOnly,
    bool? itemActiveOnly);

}