using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface IStockService
{
    Task<IEnumerable<ItemStockDto>> GetAllAsync();

    Task<ItemStockDto?> GetByItemmasterIdAsync(int itemmasterId);
}
