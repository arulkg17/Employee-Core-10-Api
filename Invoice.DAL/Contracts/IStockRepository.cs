using Invoice.Data.Entities;

namespace Invoice.DAL.Contracts;

public interface IStockRepository
{
    Task<IEnumerable<ItemStockEntity>> GetAllAsync();

    Task<ItemStockEntity?> GetByItemmasterIdAsync(int itemmasterId);
}
