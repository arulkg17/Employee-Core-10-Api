using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
namespace Invoice.DAL.Repositories;

public class VendorRepositoryEFSp : IVendorRepository
{
    private readonly AppDbContext _dbContext;
    public VendorRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<int> AddAsync(VendorEntity vendor)
    {
        var result = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC sp_Vendor_Insert
        @VendorCode,
        @VendorName,
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

            new SqlParameter("@VendorCode", vendor.VendorCode),
            new SqlParameter("@VendorName", vendor.VendorName),
            new SqlParameter("@ContactPerson", (object?)vendor.ContactPerson ?? DBNull.Value),
            new SqlParameter("@MobileNo", (object?)vendor.MobileNo ?? DBNull.Value),
            new SqlParameter("@Email", (object?)vendor.Email ?? DBNull.Value),
            new SqlParameter("@Address1", (object?)vendor.Address1 ?? DBNull.Value),
            new SqlParameter("@Address2", (object?)vendor.Address2 ?? DBNull.Value),
            new SqlParameter("@City", (object?)vendor.City ?? DBNull.Value),
            new SqlParameter("@State", (object?)vendor.State ?? DBNull.Value),
            new SqlParameter("@Country", (object?)vendor.Country ?? DBNull.Value),
            new SqlParameter("@ZipCode", (object?)vendor.ZipCode ?? DBNull.Value),
            new SqlParameter("@GstNo", (object?)vendor.GstNo ?? DBNull.Value));

        return result;
    }
    public async Task<bool> UpdateAsync(VendorEntity vendor)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC sp_Vendor_Update
        @Id,
        @VendorCode,
        @VendorName,
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

            new SqlParameter("@Id", vendor.Id),
            new SqlParameter("@VendorCode", vendor.VendorCode),
            new SqlParameter("@VendorName", vendor.VendorName),
            new SqlParameter("@ContactPerson", (object?)vendor.ContactPerson ?? DBNull.Value),
            new SqlParameter("@MobileNo", (object?)vendor.MobileNo ?? DBNull.Value),
            new SqlParameter("@Email", (object?)vendor.Email ?? DBNull.Value),
            new SqlParameter("@Address1", (object?)vendor.Address1 ?? DBNull.Value),
            new SqlParameter("@Address2", (object?)vendor.Address2 ?? DBNull.Value),
            new SqlParameter("@City", (object?)vendor.City ?? DBNull.Value),
            new SqlParameter("@State", (object?)vendor.State ?? DBNull.Value),
            new SqlParameter("@Country", (object?)vendor.Country ?? DBNull.Value),
            new SqlParameter("@ZipCode", (object?)vendor.ZipCode ?? DBNull.Value),
            new SqlParameter("@GstNo", (object?)vendor.GstNo ?? DBNull.Value),
            new SqlParameter("@IsActive", (object?)vendor.IsActive ?? DBNull.Value),
            new SqlParameter("@IsDeleted", (object?)vendor.IsDeleted ?? DBNull.Value));

        return affectedRows > 0;
    }
    public async Task<VendorEntity?> GetByIdAsync(int id)
    {
        var vendors = await _dbContext.Vendors
            .FromSqlRaw("EXEC sp_Vendor_GetById @Id",
            new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return vendors.FirstOrDefault();
    }
    public async Task<IEnumerable<VendorEntity>> GetAllAsync()
    {
        return await _dbContext.Vendors
            .FromSqlRaw("EXEC sp_Vendor_GetAll")
            .ToListAsync();
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC sp_Vendor_Delete @Id",
            new SqlParameter("@Id", id));

        return affectedRows > 0;
    }
    public async Task<PagedResultDto<VendorEntity>> GetAllPagedAsync(
    string? VendorCode,
    string? VendorName,
    string? MobileNo,
    string? City,
    int PageNumber,
    int PageSize)
    {
        using var connection = _dbContext.Database.GetDbConnection();

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = "sp_Vendor_GetPaged";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@VendorCode", (object?)VendorCode ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@VendorName", (object?)VendorName ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@MobileNo", (object?)MobileNo ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@City", (object?)City ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@PageNumber", PageNumber));
        command.Parameters.Add(new SqlParameter("@PageSize", PageSize));

        using var reader = await command.ExecuteReaderAsync();

        var vendors = new List<VendorEntity>();

        while (await reader.ReadAsync())
        {
            vendors.Add(new VendorEntity
            {
                Id = reader.GetInt32(0),
                VendorCode = reader.GetString(1),
                VendorName = reader.GetString(2),
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
                IsActive = reader.GetBoolean(13),
                IsDeleted = reader.GetBoolean(14),
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

        return new PagedResultDto<VendorEntity>
        {
            Data = vendors,
            TotalRecords = totalRecords
        };
    }
    public async Task<int> GetVendorCountAsync(bool? activeOnly)
    {
        var vendors = await GetAllAsync();

        var query = vendors.AsEnumerable();

        if (activeOnly.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == activeOnly.Value);
        }

        return query.Count();
    }
}