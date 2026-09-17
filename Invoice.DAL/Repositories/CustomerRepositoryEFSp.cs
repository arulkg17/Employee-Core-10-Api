using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
namespace Invoice.DAL.Repositories;

public class CustomerRepositoryEFSp : ICustomerRepository
{
    private readonly AppDbContext _dbContext;
    public CustomerRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<int> AddAsync(CustomerEntity customer)
    {
        var result = await _dbContext.Database.ExecuteSqlRawAsync(

            @"EXEC sp_Customer_Insert
            @CustomerCode,
            @CustomerName,
            @ContactPerson,
            @MobileNo,
            @Email,
            @Address1,
            @Address2,
            @City,
            @State,
            @Country,
            @ZipCode,
            @GstNo",
            new SqlParameter("@CustomerCode", customer.CustomerCode),
            new SqlParameter("@CustomerName", customer.CustomerName),
            new SqlParameter("@ContactPerson", (object?)customer.ContactPerson ?? DBNull.Value),
            new SqlParameter("@MobileNo", (object?)customer.MobileNo ?? DBNull.Value),
            new SqlParameter("@Email", (object?)customer.Email ?? DBNull.Value),
            new SqlParameter("@Address1", (object?)customer.Address1 ?? DBNull.Value),
            new SqlParameter("@Address2", (object?)customer.Address2 ?? DBNull.Value),
            new SqlParameter("@City", (object?)customer.City ?? DBNull.Value),
            new SqlParameter("@State", (object?)customer.State ?? DBNull.Value),
            new SqlParameter("@Country", (object?)customer.Country ?? DBNull.Value),
            new SqlParameter("@ZipCode", (object?)customer.ZipCode ?? DBNull.Value),
            new SqlParameter("@GstNo", (object?)customer.GstNo ?? DBNull.Value)
            );
        return result;
    }
    public async Task<bool> UpdateAsync(CustomerEntity customer)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC sp_Customer_Update
            @Id,
            @CustomerCode,
            @CustomerName,
            @ContactPerson,
            @MobileNo,
            @Email,
            @Address1,
            @Address2,
            @City,
            @State,
            @Country,
            @ZipCode,
            @GstNo,
            @IsActive,
            @IsDeleted",
            new SqlParameter("@Id", customer.Id),
            new SqlParameter("@CustomerCode", customer.CustomerCode),
            new SqlParameter("@CustomerName", customer.CustomerName),
            new SqlParameter("@ContactPerson", (object?)customer.ContactPerson ?? DBNull.Value),
            new SqlParameter("@MobileNo", (object?)customer.MobileNo ?? DBNull.Value),
            new SqlParameter("@Email", (object?)customer.Email ?? DBNull.Value),
            new SqlParameter("@Address1", (object?)customer.Address1 ?? DBNull.Value),
            new SqlParameter("@Address2", (object?)customer.Address2 ?? DBNull.Value),
            new SqlParameter("@City", (object?)customer.City ?? DBNull.Value),
            new SqlParameter("@State", (object?)customer.State ?? DBNull.Value),
            new SqlParameter("@Country", (object?)customer.Country ?? DBNull.Value),
            new SqlParameter("@ZipCode", (object?)customer.ZipCode ?? DBNull.Value),
            new SqlParameter("@GstNo", (object?)customer.GstNo ?? DBNull.Value),
            new SqlParameter("@IsActive", (object?)customer.IsActive ?? DBNull.Value),
            new SqlParameter("@IsDeleted", (object?)customer.IsDeleted ?? DBNull.Value)
            );
        return affectedRows > 0;
    }
    public async Task<CustomerEntity?> GetByIdAsync(int id)
    {
        var customers = await _dbContext.Customers.FromSqlRaw(
            @"EXEC sp_Customer_GetById
            @Id",
            new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();
        return customers.FirstOrDefault();
    }
    public async Task<IEnumerable<CustomerEntity>> GetAllAsync()
    {
        return await _dbContext.Customers.FromSqlRaw(
            @"EXEC sp_Customer_GetAll")
            .ToListAsync();
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC sp_Customer_Delete @Id",
            new SqlParameter("@Id", id));
        return affectedRows > 0;
    }
    public async Task<PagedResultDto<CustomerEntity>> GetAllPagedAsync(
        string? CustomerCode,
        string? CustomerName,
        string? MobileNo,
        string? City,
        int PageNumber,
        int PageSize)
    {
        using (var connection = _dbContext.Database.GetDbConnection())
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "sp_Customer_GetPaged";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@CustomerCode", (object?)CustomerCode ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@CustomerName", (object?)CustomerName ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@MobileNo", (object?)MobileNo ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@City", (object?)City ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@PageNumber", PageNumber));
            command.Parameters.Add(new SqlParameter("@PageSize", PageSize));

            using var reader = await command.ExecuteReaderAsync();
            var customers = new List<CustomerEntity>();
            while (await reader.ReadAsync())
            {
                customers.Add(new CustomerEntity
                {
                    Id = reader.GetInt32(0),
                    CustomerCode = reader.GetString(1),
                    CustomerName = reader.GetString(2),
                    ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                    MobileNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Address1 = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Address2 = reader.IsDBNull(7) ? null : reader.GetString(7),
                    City = reader.IsDBNull(8) ? null : reader.GetString(8),
                    State = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Country = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ZipCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                    GstNo = reader.IsDBNull(12) ? null : reader.GetString(12),
                    IsActive = reader.IsDBNull(13) ? false : reader.GetBoolean(13),
                    IsDeleted = reader.IsDBNull(14) ? false : reader.GetBoolean(14),
                    CreatedBy = reader.IsDBNull(15) ? null : reader.GetString(15),
                    CreatedDate = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
                    UpdatedBy = reader.IsDBNull(17) ? null : reader.GetString(17),
                    UpdatedDate = reader.IsDBNull(18) ? null : reader.GetDateTime(18)
                });
            }
            await reader.NextResultAsync();
            int totalRecords = 0;
            if (await reader.ReadAsync())
            {
                totalRecords = reader.GetInt32(0);
            }
            return new PagedResultDto<CustomerEntity>
            {
                Data = customers,
                TotalRecords = totalRecords
            };
        }
    }
    public async Task<int> GetCustomerCountAsync(bool? activeOnly)
    {
        var query = _dbContext.Customers
            .Where(x => x.IsDeleted != true);

        if (activeOnly.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == activeOnly.Value);
        }

        return await query.CountAsync();
    }
}