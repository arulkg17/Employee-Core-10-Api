using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class SalesInvoiceDetailRepositoryEFSp : ISalesInvoiceDetailRepository
{
    private readonly AppDbContext _dbContext;

    public SalesInvoiceDetailRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> AddAsync(SalesInvoiceDetailEntity detail)
    {
        return SpExecutor.ScalarIntAsync(
            _dbContext,
            "dbo.sp_SalesInvoiceDetail_Insert",
            SpExecutor.P("@SalesInvoiceId", detail.SalesInvoiceId),
            SpExecutor.P("@ItemmasterId", detail.ItemmasterId),
            SpExecutor.Dec("@Quantity", detail.Quantity),
            SpExecutor.Dec("@Rate", detail.Rate),
            SpExecutor.Dec("@DiscountAmount", detail.DiscountAmount),
            SpExecutor.Dec("@TaxPercent", detail.TaxPercent),
            SpExecutor.Dec("@TaxAmount", detail.TaxAmount),
            SpExecutor.Dec("@LineTotal", detail.LineTotal));
    }

    public async Task<IEnumerable<SalesInvoiceDetailEntity>> GetBySalesInvoiceIdAsync(
        int salesInvoiceId)
    {
        return await _dbContext.SalesInvoiceDetails
            .FromSqlRaw(
                "EXEC dbo.sp_SalesInvoiceDetail_GetBySalesInvoiceId @SalesInvoiceId",
                new SqlParameter("@SalesInvoiceId", salesInvoiceId))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SalesInvoiceDetailEntity?> GetByIdAsync(int id)
    {
        var details = await _dbContext.SalesInvoiceDetails
            .FromSqlRaw(
                "EXEC dbo.sp_SalesInvoiceDetail_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return details.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(SalesInvoiceDetailEntity detail)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_SalesInvoiceDetail_Update",
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
            "dbo.sp_SalesInvoiceDetail_Delete",
            SpExecutor.P("@Id", id));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteBySalesInvoiceIdAsync(int salesInvoiceId)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_SalesInvoiceDetail_DeleteBySalesInvoiceId",
            SpExecutor.P("@SalesInvoiceId", salesInvoiceId));

        return affectedRows > 0;
    }
}
