using Invoice.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.DAL.Contracts;

public interface IProductRepository
{
    Task<IEnumerable<ProductEntity>> GetAllAsync();
    Task<ProductEntity?> GetByIdAsync(int id);
    Task<ProductEntity> CreateAsync(ProductEntity entity);
    Task<bool> UpdateAsync(ProductEntity entity);
    Task<bool> DeleteAsync(int id);
}