using Invoice.Data.Entities;
using Invoice.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.DAL.Contracts;

public interface IItemmasterRepository
{
    Task<int> AddAsync(ItemmasterEntity itemmaster);

    Task<IEnumerable<ItemmasterEntity>> GetAllAsync();

    Task<ItemmasterEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(ItemmasterEntity itemmaster);

    Task<bool> DeleteAsync(int id);

    Task<PagedResultDto<ItemmasterEntity>> GetAllPagedAsync(
        ItemmasterFilterDto search);
    Task<int> GetActiveItemCountByCategoryAsync(int categoryId);

}