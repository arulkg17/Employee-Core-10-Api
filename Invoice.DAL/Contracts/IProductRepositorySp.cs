using Invoice.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.DAL.Contracts;

public interface IProductRepositorySp
{
    Task<int> AddAsync(ProductEntity product);
    Task<IEnumerable<ProductEntity>> GetAllAsync();
    Task<ProductEntity?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(ProductEntity product);
    Task<bool> DeleteAsync(int id, byte[] rowVersion);
}