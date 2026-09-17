using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface IItemmasterService
{
    Task<int> AddAsync(ItemmasterDto dto);

    Task<IEnumerable<ItemmasterDto>> GetAllAsync();

    Task<ItemmasterDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(ItemmasterDto dto);

    Task<bool> DeleteAsync(int id);

    Task<PagedResultDto<ItemmasterDto>> GetAllPagedAsync(
        ItemmasterFilterDto search);

    Task<int> GetActiveItemCountByCategoryAsync(int categoryId);
}