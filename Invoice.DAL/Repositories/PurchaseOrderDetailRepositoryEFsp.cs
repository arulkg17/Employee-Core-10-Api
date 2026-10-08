using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class PurchaseOrderDetailRepositoryEFSp
    : IPurchaseOrderDetailRepository
{
    private readonly AppDbContext _dbContext;

    public PurchaseOrderDetailRepositoryEFSp(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ============================================================
    // INSERT
    // ============================================================
    public async Task<int> AddAsync(
        PurchaseOrderDetailEntity detail)
    {
        var connection =
            _dbContext.Database.GetDbConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            "sp_PurchaseOrderDetail_Insert";

        command.CommandType =
            System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@PurchaseOrderId",
                detail.PurchaseOrderId));

        command.Parameters.Add(
            new SqlParameter(
                "@ItemmasterId",
                detail.ItemmasterId));

        command.Parameters.Add(
            new SqlParameter(
                "@Quantity",
                detail.Quantity));

        command.Parameters.Add(
            new SqlParameter(
                "@Rate",
                detail.Rate));

        command.Parameters.Add(
            new SqlParameter(
                "@DiscountAmount",
                detail.DiscountAmount));

        command.Parameters.Add(
            new SqlParameter(
                "@TaxPercent",
                detail.TaxPercent));

        command.Parameters.Add(
            new SqlParameter(
                "@TaxAmount",
                detail.TaxAmount));

        command.Parameters.Add(
            new SqlParameter(
                "@LineTotal",
                detail.LineTotal));

        if (connection.State !=
            System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    // ============================================================
    // GET BY PURCHASE ORDER
    // ============================================================
    public async Task<IEnumerable<PurchaseOrderDetailEntity>>
        GetByPurchaseOrderIdAsync(int purchaseOrderId)
    {
        return await _dbContext.PurchaseOrderDetails
            .FromSqlRaw(
                @"EXEC sp_PurchaseOrderDetail_GetByPurchaseOrderId
                    @PurchaseOrderId",
                new SqlParameter(
                    "@PurchaseOrderId",
                    purchaseOrderId))
            .AsNoTracking()
            .ToListAsync();
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<PurchaseOrderDetailEntity?>
        GetByIdAsync(int id)
    {
        var details =
            await _dbContext.PurchaseOrderDetails
                .FromSqlRaw(
                    @"EXEC sp_PurchaseOrderDetail_GetById @Id",
                    new SqlParameter("@Id", id))
                .AsNoTracking()
                .ToListAsync();

        return details.FirstOrDefault();
    }

    // ============================================================
    // UPDATE
    // ============================================================
    public async Task<bool> UpdateAsync(
        PurchaseOrderDetailEntity detail)
    {
        var affectedRows =
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"EXEC sp_PurchaseOrderDetail_Update
                    @Id,
                    @ItemmasterId,
                    @Quantity,
                    @Rate,
                    @DiscountAmount,
                    @TaxPercent,
                    @TaxAmount,
                    @LineTotal",

                new SqlParameter("@Id", detail.Id),

                new SqlParameter(
                    "@ItemmasterId",
                    detail.ItemmasterId),

                new SqlParameter(
                    "@Quantity",
                    detail.Quantity),

                new SqlParameter(
                    "@Rate",
                    detail.Rate),

                new SqlParameter(
                    "@DiscountAmount",
                    detail.DiscountAmount),

                new SqlParameter(
                    "@TaxPercent",
                    detail.TaxPercent),

                new SqlParameter(
                    "@TaxAmount",
                    detail.TaxAmount),

                new SqlParameter(
                    "@LineTotal",
                    detail.LineTotal));

        return affectedRows > 0;
    }

    // ============================================================
    // DELETE
    // ============================================================
    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows =
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"EXEC sp_PurchaseOrderDetail_Delete @Id",
                new SqlParameter("@Id", id));

        return affectedRows > 0;
    }

    // ============================================================
    // DELETE ALL DETAILS FOR PO
    // ============================================================
    public async Task<bool> DeleteByPurchaseOrderIdAsync(
        int purchaseOrderId)
    {
        var affectedRows =
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"EXEC sp_PurchaseOrderDetail_DeleteByPurchaseOrderId
                    @PurchaseOrderId",
                new SqlParameter(
                    "@PurchaseOrderId",
                    purchaseOrderId));

        return affectedRows > 0;
    }
}