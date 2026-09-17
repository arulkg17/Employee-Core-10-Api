using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace Invoice.DAL.Repositories;

public class ItemmasterRepositoryEFSp : IItemmasterRepository
{
    private readonly AppDbContext _dbContext;

    public ItemmasterRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> AddAsync(ItemmasterEntity item)
    {
        var result = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC dbo.sp_Itemmaster_Insert
                @CategoryId,
                @ItemBarCode,
                @ItemCode,
                @ItemName,
                @Description,
                @Uom,
                @Rate,
                @MinimumStock,
                @MaximumStock,
                @IsActive",
            new SqlParameter("@CategoryId", item.CategoryId),
            new SqlParameter("@ItemBarCode", item.ItemBarCode),
            new SqlParameter("@ItemCode", item.ItemCode),
            new SqlParameter("@ItemName", item.ItemName),
            new SqlParameter(
                "@Description",
                (object?)item.Description ?? DBNull.Value),
            new SqlParameter("@Uom", item.Uom),
            new SqlParameter(
                "@Rate",
                (object?)item.Rate ?? DBNull.Value),
            new SqlParameter(
                "@MinimumStock",
                (object?)item.MinimumStock ?? DBNull.Value),
            new SqlParameter(
                "@MaximumStock",
                (object?)item.MaximumStock ?? DBNull.Value),
            new SqlParameter(
                "@IsActive",
                (object?)item.IsActive ?? DBNull.Value)
        );

        return result;
    }

    public async Task<bool> UpdateAsync(ItemmasterEntity item)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC dbo.sp_Itemmaster_Update
                @Id,
                @CategoryId,
                @ItemBarCode,
                @ItemCode,
                @ItemName,
                @Description,
                @Uom,
                @Rate,
                @MinimumStock,
                @MaximumStock,
                @IsActive",
            new SqlParameter("@Id", item.Id),
            new SqlParameter("@CategoryId", item.CategoryId),
            new SqlParameter("@ItemBarCode", item.ItemBarCode),
            new SqlParameter("@ItemCode", item.ItemCode),
            new SqlParameter("@ItemName", item.ItemName),
            new SqlParameter(
                "@Description",
                (object?)item.Description ?? DBNull.Value),
            new SqlParameter("@Uom", item.Uom),
            new SqlParameter(
                "@Rate",
                (object?)item.Rate ?? DBNull.Value),
            new SqlParameter(
                "@MinimumStock",
                (object?)item.MinimumStock ?? DBNull.Value),
            new SqlParameter(
                "@MaximumStock",
                (object?)item.MaximumStock ?? DBNull.Value),
            new SqlParameter(
                "@IsActive",
                (object?)item.IsActive ?? DBNull.Value)
        );

        return affectedRows > 0;
    }

    public async Task<ItemmasterEntity?> GetByIdAsync(int id)
    {
        var items = await _dbContext.Itemmasters
            .FromSqlRaw(
                "EXEC dbo.sp_Itemmaster_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return items.FirstOrDefault();
    }

    public async Task<IEnumerable<ItemmasterEntity>> GetAllAsync()
    {
        return await _dbContext.Itemmasters
            .FromSqlRaw("EXEC dbo.sp_Itemmaster_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_Itemmaster_Delete @Id",
            new SqlParameter("@Id", id));

        return affectedRows > 0;
    }

    public async Task<PagedResultDto<ItemmasterEntity>> GetAllPagedAsync(
        ItemmasterFilterDto search)
    {
        using var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Itemmaster_GetPaged";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@CategoryId",
                (object?)search.CategoryId ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@ItemBarCode",
                (object?)search.ItemBarCode ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@ItemCode",
                (object?)search.ItemCode ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@ItemName",
                (object?)search.ItemName ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@Uom",
                (object?)search.Uom ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@IsActive",
                (object?)search.IsActive ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@PageNumber",
                search.PageNumber));

        command.Parameters.Add(
            new SqlParameter(
                "@PageSize",
                search.PageSize));

        using var reader = await command.ExecuteReaderAsync();

        var items = new List<ItemmasterEntity>();

        // Result Set 1 - Itemmaster records
        while (await reader.ReadAsync())
        {
            items.Add(new ItemmasterEntity
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),

                CategoryId = reader.GetInt32(
                    reader.GetOrdinal("CategoryId")),

                ItemBarCode = reader.GetString(
                    reader.GetOrdinal("ItemBarCode")),

                ItemCode = reader.GetString(
                    reader.GetOrdinal("ItemCode")),

                ItemName = reader.GetString(
                    reader.GetOrdinal("ItemName")),

                Description = reader.IsDBNull(
                    reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("Description")),

                Uom = reader.GetString(
                    reader.GetOrdinal("Uom")),

                Rate = reader.GetDecimal(
                    reader.GetOrdinal("Rate")),

                MinimumStock = reader.GetDecimal(
                    reader.GetOrdinal("MinimumStock")),

                MaximumStock = reader.GetDecimal(
                    reader.GetOrdinal("MaximumStock")),

                IsActive = reader.GetBoolean(
                    reader.GetOrdinal("IsActive"))
            });
        }

        // Result Set 2 - TotalRecords
        await reader.NextResultAsync();

        var totalRecords = 0;

        if (await reader.ReadAsync())
        {
            totalRecords = reader.GetInt32(
                reader.GetOrdinal("TotalRecords"));
        }

        return new PagedResultDto<ItemmasterEntity>
        {
            Data = items,
            TotalRecords = totalRecords
        };
    }
    public async Task<int> GetActiveItemCountByCategoryAsync(int categoryId)
    {
        using var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Itemmaster_GetActiveCountByCategory";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter("@CategoryId", categoryId));

        var result = await command.ExecuteScalarAsync();

        return result == null || result == DBNull.Value
            ? 0
            : Convert.ToInt32(result);
    }


}