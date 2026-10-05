using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Invoice.DAL.Repositories;

public class PurchaseOrderRepositoryEFSp : IPurchaseOrderRepository
{
    private readonly AppDbContext _dbContext;

    public PurchaseOrderRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ============================================================
    // INSERT
    // ============================================================
    public async Task<int> AddAsync(PurchaseOrderEntity purchaseOrder)
    {
        var connection = _dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "sp_PurchaseOrder_Insert";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@PONumber", purchaseOrder.PONumber));
        command.Parameters.Add(new SqlParameter("@PODate", purchaseOrder.PODate));
        command.Parameters.Add(new SqlParameter("@VendorId", purchaseOrder.VendorId));
        command.Parameters.Add(new SqlParameter("@Status", purchaseOrder.Status));

        command.Parameters.Add(new SqlParameter(
            "@Notes",
            (object?)purchaseOrder.Notes ?? DBNull.Value));

        command.Parameters.Add(new SqlParameter("@SubTotal", purchaseOrder.SubTotal));
        command.Parameters.Add(new SqlParameter("@TaxAmount", purchaseOrder.TaxAmount));
        command.Parameters.Add(new SqlParameter("@TotalAmount", purchaseOrder.TotalAmount));

        command.Parameters.Add(new SqlParameter(
            "@CreatedBy",
            (object?)purchaseOrder.CreatedBy ?? DBNull.Value));

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<PurchaseOrderEntity?> GetByIdAsync(int id)
    {
        var purchaseOrders = await _dbContext.PurchaseOrders
            .FromSqlRaw(
                "EXEC sp_PurchaseOrder_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return purchaseOrders.FirstOrDefault();
    }

    // ============================================================
    // GET ALL
    // ============================================================
    public async Task<IEnumerable<PurchaseOrderEntity>> GetAllAsync()
    {
        return await _dbContext.PurchaseOrders
            .FromSqlRaw("EXEC sp_PurchaseOrder_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    // ============================================================
    // UPDATE
    // ============================================================
    public async Task<bool> UpdateAsync(
        PurchaseOrderEntity purchaseOrder)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            @"EXEC sp_PurchaseOrder_Update
                @Id,
                @PONumber,
                @PODate,
                @VendorId,
                @Status,
                @Notes,
                @SubTotal,
                @TaxAmount,
                @TotalAmount,
                @UpdatedBy",

            new SqlParameter("@Id", purchaseOrder.Id),
            new SqlParameter("@PONumber", purchaseOrder.PONumber),
            new SqlParameter("@PODate", purchaseOrder.PODate),
            new SqlParameter("@VendorId", purchaseOrder.VendorId),
            new SqlParameter("@Status", purchaseOrder.Status),
            new SqlParameter(
                "@Notes",
                (object?)purchaseOrder.Notes ?? DBNull.Value),
            new SqlParameter("@SubTotal", purchaseOrder.SubTotal),
            new SqlParameter("@TaxAmount", purchaseOrder.TaxAmount),
            new SqlParameter("@TotalAmount", purchaseOrder.TotalAmount),
            new SqlParameter(
                "@UpdatedBy",
                (object?)purchaseOrder.UpdatedBy ?? DBNull.Value));

        return affectedRows > 0;
    }

    // ============================================================
    // DELETE
    // ============================================================
    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows =
            await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_PurchaseOrder_Delete @Id",
                new SqlParameter("@Id", id));

        return affectedRows > 0;
    }

    // ============================================================
    // PAGED
    // ============================================================
    public async Task<PagedResultDto<PurchaseOrderEntity>>
        GetAllPagedAsync(
            string? PONumber,
            int? VendorId,
            string? Status,
            int PageNumber,
            int PageSize)
    {
        var connection = _dbContext.Database.GetDbConnection();

        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = "sp_PurchaseOrder_GetPaged";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@PONumber",
                (object?)PONumber ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@VendorId",
                (object?)VendorId ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter(
                "@Status",
                (object?)Status ?? DBNull.Value));

        command.Parameters.Add(
            new SqlParameter("@PageNumber", PageNumber));

        command.Parameters.Add(
            new SqlParameter("@PageSize", PageSize));

        using var reader =
            await command.ExecuteReaderAsync();

        var purchaseOrders =
            new List<PurchaseOrderEntity>();

        while (await reader.ReadAsync())
        {
            purchaseOrders.Add(
                new PurchaseOrderEntity
                {
                    Id = reader.GetInt32(0),
                    PONumber = reader.GetString(1),
                    PODate = reader.GetDateTime(2),
                    VendorId = reader.GetInt32(3),
                    Status = reader.GetString(4),

                    Notes = reader.IsDBNull(5)
                        ? null
                        : reader.GetString(5),

                    SubTotal = reader.GetDecimal(6),
                    TaxAmount = reader.GetDecimal(7),
                    TotalAmount = reader.GetDecimal(8),

                    IsDeleted = reader.GetBoolean(9),

                    CreatedBy = reader.IsDBNull(10)
                        ? null
                        : reader.GetString(10),

                    CreatedDate = reader.IsDBNull(11)
                        ? null
                        : reader.GetDateTime(11),

                    UpdatedBy = reader.IsDBNull(12)
                        ? null
                        : reader.GetString(12),

                    UpdatedDate = reader.IsDBNull(13)
                        ? null
                        : reader.GetDateTime(13)
                });
        }

        await reader.NextResultAsync();

        int totalRecords = 0;

        if (await reader.ReadAsync())
        {
            totalRecords = reader.GetInt32(0);
        }

        return new PagedResultDto<PurchaseOrderEntity>
        {
            Data = purchaseOrders,
            TotalRecords = totalRecords
        };
    }
}