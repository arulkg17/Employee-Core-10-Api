using Invoice.Data.Entities;
using Invoice.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.DAL.Contracts;

public interface IVendorRepository
{
    Task<int> AddAsync(VendorEntity vendor);
    Task<IEnumerable<VendorEntity>> GetAllAsync();
    Task<VendorEntity?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(VendorEntity vendor);
    Task<bool> DeleteAsync(int id);
    Task<PagedResultDto<VendorEntity>> GetAllPagedAsync(
        string? VendorCode,
        string? VendorName,
        string? MobileNo,
        string? City,
        int pageNumber,
        int pageSize);

    Task<int> GetVendorCountAsync(bool? activeOnly);
}