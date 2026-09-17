using Invoice.Data.Entities;

namespace Invoice.DAL.Contracts;

public interface IProductRepository
{
    Task<IEnumerable<ProductEntity>> GetAllAsync();
    Task<ProductEntity?> GetByIdAsync(int id);
    Task<int> CreateAsync(ProductEntity entity);
    Task<bool> UpdateAsync(ProductEntity entity);
    Task<bool> DeleteAsync(int id, byte[] rowVersion);
}