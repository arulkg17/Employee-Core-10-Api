using Invoice.Data.Entities;
using Invoice.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.DAL.Contracts;

public interface ICustomerRepository
{
    Task<int> AddAsync(CustomerEntity customer);
    Task<IEnumerable<CustomerEntity>> GetAllAsync();
    Task<CustomerEntity?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(CustomerEntity customer);
    Task<bool> DeleteAsync(int id);
    Task<PagedResultDto<CustomerEntity>> GetAllPagedAsync(
    string? CustomerCode,
    string? CustomerName,
    string? MobileNo,
    string? City,
    int PageNumber,
    int PageSize
    );

    Task<int> GetCustomerCountAsync(bool? activeOnly);
}