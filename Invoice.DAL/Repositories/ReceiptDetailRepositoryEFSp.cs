using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class ReceiptDetailRepositoryEFSp : IReceiptDetailRepository
{
    private readonly AppDbContext _dbContext;

    public ReceiptDetailRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> AddAsync(ReceiptDetailEntity detail)
    {
        return SpExecutor.ScalarIntAsync(
            _dbContext,
            "dbo.sp_ReceiptDetail_Insert",
            SpExecutor.P("@ReceiptId", detail.ReceiptId),
            SpExecutor.P("@PurchaseOrderDetailId", detail.PurchaseOrderDetailId),
            SpExecutor.P("@ItemmasterId", detail.ItemmasterId),
            SpExecutor.Dec("@ReceivedQuantity", detail.ReceivedQuantity),
            SpExecutor.Dec("@Rate", detail.Rate),
            SpExecutor.Dec("@DiscountAmount", detail.DiscountAmount),
            SpExecutor.Dec("@TaxPercent", detail.TaxPercent),
            SpExecutor.Dec("@TaxAmount", detail.TaxAmount),
            SpExecutor.Dec("@LineTotal", detail.LineTotal));
    }

    public async Task<IEnumerable<ReceiptDetailEntity>> GetByReceiptIdAsync(int receiptId)
    {
        return await _dbContext.ReceiptDetails
            .FromSqlRaw(
                "EXEC dbo.sp_ReceiptDetail_GetByReceiptId @ReceiptId",
                new SqlParameter("@ReceiptId", receiptId))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ReceiptDetailEntity?> GetByIdAsync(int id)
    {
        var details = await _dbContext.ReceiptDetails
            .FromSqlRaw(
                "EXEC dbo.sp_ReceiptDetail_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return details.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(ReceiptDetailEntity detail)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_ReceiptDetail_Update",
            SpExecutor.P("@Id", detail.Id),
            SpExecutor.Dec("@ReceivedQuantity", detail.ReceivedQuantity),
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
            "dbo.sp_ReceiptDetail_Delete",
            SpExecutor.P("@Id", id));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteByReceiptIdAsync(int receiptId)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_ReceiptDetail_DeleteByReceiptId",
            SpExecutor.P("@ReceiptId", receiptId));

        return affectedRows > 0;
    }
}
