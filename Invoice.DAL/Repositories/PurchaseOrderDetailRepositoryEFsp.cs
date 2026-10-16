using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class PurchaseOrderDetailRepositoryEFSp : IPurchaseOrderDetailRepository
{
    private readonly AppDbContext _dbContext;

    public PurchaseOrderDetailRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> AddAsync(PurchaseOrderDetailEntity detail)
    {
        return SpExecutor.ScalarIntAsync(
            _dbContext,
            "dbo.sp_PurchaseOrderDetail_Insert",
            SpExecutor.P("@PurchaseOrderId", detail.PurchaseOrderId),
            SpExecutor.P("@ItemmasterId", detail.ItemmasterId),
            SpExecutor.Dec("@Quantity", detail.Quantity),
            SpExecutor.Dec("@Rate", detail.Rate),
            SpExecutor.Dec("@DiscountAmount", detail.DiscountAmount),
            SpExecutor.Dec("@TaxPercent", detail.TaxPercent),
            SpExecutor.Dec("@TaxAmount", detail.TaxAmount),
            SpExecutor.Dec("@LineTotal", detail.LineTotal));
    }

    public async Task<IEnumerable<PurchaseOrderDetailEntity>> GetByPurchaseOrderIdAsync(
        int purchaseOrderId)
    {
        return await _dbContext.PurchaseOrderDetails
            .FromSqlRaw(
                "EXEC dbo.sp_PurchaseOrderDetail_GetByPurchaseOrderId @PurchaseOrderId",
                new SqlParameter("@PurchaseOrderId", purchaseOrderId))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PurchaseOrderDetailEntity?> GetByIdAsync(int id)
    {
        var details = await _dbContext.PurchaseOrderDetails
            .FromSqlRaw(
                "EXEC dbo.sp_PurchaseOrderDetail_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return details.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(PurchaseOrderDetailEntity detail)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_PurchaseOrderDetail_Update",
            SpExecutor.P("@Id", detail.Id),
            SpExecutor.P("@ItemmasterId", detail.ItemmasterId),
            SpExecutor.Dec("@Quantity", detail.Quantity),
            SpExecutor.Dec("@Rate", detail.Rate),
            SpExecutor.Dec("@DiscountAmount", detail.DiscountAmount),
            SpExecutor.Dec("@TaxPercent", detail.TaxPercent),
            SpExecutor.Dec("@TaxAmount", detail.TaxAmount),
            SpExecutor.Dec("@LineTotal", detail.LineTotal));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_PurchaseOrderDetail_Delete",
            SpExecutor.P("@Id", id));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteByPurchaseOrderIdAsync(int purchaseOrderId)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_PurchaseOrderDetail_DeleteByPurchaseOrderId",
            SpExecutor.P("@PurchaseOrderId", purchaseOrderId));

        return affectedRows > 0;
    }
}
