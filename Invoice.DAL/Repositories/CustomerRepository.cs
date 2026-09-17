using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.EntityFrameworkCore;
namespace Invoice.DAL.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> AddAsync(CustomerEntity customer)
    {
        _dbContext.Customers.Add(customer);

        await _dbContext.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<IEnumerable<CustomerEntity>> GetAllAsync()
    {
        return await _dbContext.Customers
            .ToListAsync();
    }

    public async Task<CustomerEntity?> GetByIdAsync(int id)
    {
        return await _dbContext.Customers
            .FindAsync(id);
    }

    public async Task<bool> UpdateAsync(CustomerEntity customer)
    {
        _dbContext.Customers.Update(customer);

        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _dbContext.Customers
            .FindAsync(id);

        if (entity == null)
            return false;

        _dbContext.Customers.Remove(entity);

        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<PagedResultDto<CustomerEntity>> GetAllPagedAsync(
 string? CustomerCode,
 string? CustomerName,
 string? MobileNo,
 string? City,
 int PageNumber,
 int PageSize)
    {
        var query = _dbContext.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(CustomerCode))
        {
            query = query.Where(x =>
                x.CustomerCode.Contains(CustomerCode));
        }

        if (!string.IsNullOrWhiteSpace(CustomerName))
        {
            query = query.Where(x =>
                x.CustomerName.Contains(CustomerName));
        }

        if (!string.IsNullOrWhiteSpace(MobileNo))
        {
            query = query.Where(x =>
                x.MobileNo != null &&
                x.MobileNo.Contains(MobileNo));
        }

        if (!string.IsNullOrWhiteSpace(City))
        {
            query = query.Where(x =>
                x.City != null &&
                x.City.Contains(City));
        }

        var totalRecords = await query.CountAsync();

        var data = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return new PagedResultDto<CustomerEntity>
        {
            Data = data,
            TotalRecords = totalRecords
        };
    }

    public async Task<int> GetCustomerCountAsync(bool? activeOnly)
    {
        var query = _dbContext.Customers.AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == activeOnly.Value);
        }

        return await query.CountAsync();
    }
}