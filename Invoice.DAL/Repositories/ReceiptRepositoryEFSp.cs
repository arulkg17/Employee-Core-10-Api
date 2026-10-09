using System.Data.Common;
using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class ReceiptRepositoryEFSp : IReceiptRepository
{
    private readonly AppDbContext _dbContext;

    public ReceiptRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> AddAsync(ReceiptEntity receipt)
    {
        return SpExecutor.ScalarIntAsync(
            _dbContext,
            "dbo.sp_Receipt_Insert",
            SpExecutor.P(
                "@ReceiptNumber",
                string.IsNullOrWhiteSpace(receipt.ReceiptNumber) ? null : receipt.ReceiptNumber),
            SpExecutor.P("@ReceiptDate", receipt.ReceiptDate),
            SpExecutor.P("@PurchaseOrderId", receipt.PurchaseOrderId),
            SpExecutor.P("@Notes", receipt.Notes),
            SpExecutor.Dec("@SubTotal", receipt.SubTotal),
            SpExecutor.Dec("@TaxAmount", receipt.TaxAmount),
            SpExecutor.Dec("@TotalAmount", receipt.TotalAmount),
            SpExecutor.P("@CreatedBy", receipt.CreatedBy));
    }

    public async Task<IEnumerable<ReceiptEntity>> GetAllAsync()
    {
        return await _dbContext.Receipts
            .FromSqlRaw("EXEC dbo.sp_Receipt_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ReceiptEntity?> GetByIdAsync(int id)
    {
        var receipts = await _dbContext.Receipts
            .FromSqlRaw(
                "EXEC dbo.sp_Receipt_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return receipts.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(ReceiptEntity receipt)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_Receipt_Update",
            SpExecutor.P("@Id", receipt.Id),
            SpExecutor.P("@ReceiptDate", receipt.ReceiptDate),
            SpExecutor.P("@Notes", receipt.Notes),
            SpExecutor.Dec("@SubTotal", receipt.SubTotal),
            SpExecutor.Dec("@TaxAmount", receipt.TaxAmount),
            SpExecutor.Dec("@TotalAmount", receipt.TotalAmount),
            SpExecutor.P("@UpdatedBy", receipt.UpdatedBy));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, string? updatedBy = null)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_Receipt_Delete",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));

        return affectedRows > 0;
    }

    public async Task<PagedResultDto<ReceiptEntity>> GetAllPagedAsync(
        string? receiptNumber,
        int? purchaseOrderId,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var (rows, total) = await SpExecutor.PagedAsync(
            _dbContext,
            "dbo.sp_Receipt_GetPaged",
            Map,
            SpExecutor.P("@ReceiptNumber", receiptNumber),
            SpExecutor.P("@PurchaseOrderId", purchaseOrderId),
            SpExecutor.P("@VendorId", vendorId),
            SpExecutor.P("@Status", status),
            SpExecutor.P("@PageNumber", pageNumber),
            SpExecutor.P("@PageSize", pageSize));

        return new PagedResultDto<ReceiptEntity>
        {
            Data = rows,
            TotalRecords = total
        };
    }

    public Task<bool> PostAsync(int id, string? updatedBy)
    {
        return SpExecutor.ScalarBoolAsync(
            _dbContext,
            "dbo.sp_Receipt_Post",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));
    }

    public Task<bool> CancelAsync(int id, string? updatedBy)
    {
        return SpExecutor.ScalarBoolAsync(
            _dbContext,
            "dbo.sp_Receipt_Cancel",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));
    }

    private static ReceiptEntity Map(DbDataReader r) => new()
    {
        Id = r.Int("Id"),
        ReceiptNumber = r.Text("ReceiptNumber"),
        ReceiptDate = r.Stamp("ReceiptDate"),
        PurchaseOrderId = r.Int("PurchaseOrderId"),
        VendorId = r.Int("VendorId"),
        Status = r.Text("Status"),
        Notes = r.TextOrNull("Notes"),
        SubTotal = r.Num("SubTotal"),
        TaxAmount = r.Num("TaxAmount"),
        TotalAmount = r.Num("TotalAmount"),
        IsDeleted = r.Flag("IsDeleted"),
        CreatedBy = r.TextOrNull("CreatedBy"),
        CreatedDate = r.StampOrNull("CreatedDate"),
        UpdatedBy = r.TextOrNull("UpdatedBy"),
        UpdatedDate = r.StampOrNull("UpdatedDate")
    };
}
