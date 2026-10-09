using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
    public Task<int> AddAsync(PurchaseOrderEntity purchaseOrder)
    {
        return SpExecutor.ScalarIntAsync(
            _dbContext,
            "dbo.sp_PurchaseOrder_Insert",
            SpExecutor.P("@PONumber", purchaseOrder.PONumber),
            SpExecutor.P("@PODate", purchaseOrder.PODate),
            SpExecutor.P("@VendorId", purchaseOrder.VendorId),
            SpExecutor.P("@Status", purchaseOrder.Status),
            SpExecutor.P("@Notes", purchaseOrder.Notes),
            SpExecutor.Dec("@SubTotal", purchaseOrder.SubTotal),
            SpExecutor.Dec("@TaxAmount", purchaseOrder.TaxAmount),
            SpExecutor.Dec("@TotalAmount", purchaseOrder.TotalAmount),
            SpExecutor.P("@CreatedBy", purchaseOrder.CreatedBy));
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<PurchaseOrderEntity?> GetByIdAsync(int id)
    {
        var purchaseOrders = await _dbContext.PurchaseOrders
            .FromSqlRaw(
                "EXEC dbo.sp_PurchaseOrder_GetById @Id",
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
            .FromSqlRaw("EXEC dbo.sp_PurchaseOrder_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    // ============================================================
    // UPDATE  (SP throws when the PO is not Draft)
    // ============================================================
    public async Task<bool> UpdateAsync(PurchaseOrderEntity purchaseOrder)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_PurchaseOrder_Update",
            SpExecutor.P("@Id", purchaseOrder.Id),
            SpExecutor.P("@PONumber", purchaseOrder.PONumber),
            SpExecutor.P("@PODate", purchaseOrder.PODate),
            SpExecutor.P("@VendorId", purchaseOrder.VendorId),
            SpExecutor.P("@Status", purchaseOrder.Status),
            SpExecutor.P("@Notes", purchaseOrder.Notes),
            SpExecutor.Dec("@SubTotal", purchaseOrder.SubTotal),
            SpExecutor.Dec("@TaxAmount", purchaseOrder.TaxAmount),
            SpExecutor.Dec("@TotalAmount", purchaseOrder.TotalAmount),
            SpExecutor.P("@UpdatedBy", purchaseOrder.UpdatedBy));

        return affectedRows > 0;
    }

    // ============================================================
    // DELETE  (soft delete; SP allows Draft / Cancelled only)
    // ============================================================
    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_PurchaseOrder_Delete",
            SpExecutor.P("@Id", id));

        return affectedRows > 0;
    }

    // ============================================================
    // PAGED
    // ============================================================
    public async Task<PagedResultDto<PurchaseOrderEntity>> GetAllPagedAsync(
        string? PONumber,
        int? VendorId,
        string? Status,
        int pageNumber,
        int pageSize)
    {
        var (rows, total) = await SpExecutor.PagedAsync(
            _dbContext,
            "dbo.sp_PurchaseOrder_GetPaged",
            reader => new PurchaseOrderEntity
            {
                Id = reader.Int("Id"),
                PONumber = reader.Text("PONumber"),
                PODate = reader.Stamp("PODate"),
                VendorId = reader.Int("VendorId"),
                Status = reader.Text("Status"),
                Notes = reader.TextOrNull("Notes"),
                SubTotal = reader.Num("SubTotal"),
                TaxAmount = reader.Num("TaxAmount"),
                TotalAmount = reader.Num("TotalAmount"),
                IsDeleted = reader.Flag("IsDeleted"),
                CreatedBy = reader.TextOrNull("CreatedBy"),
                CreatedDate = reader.StampOrNull("CreatedDate"),
                UpdatedBy = reader.TextOrNull("UpdatedBy"),
                UpdatedDate = reader.StampOrNull("UpdatedDate")
            },
            SpExecutor.P("@PONumber", PONumber),
            SpExecutor.P("@VendorId", VendorId),
            SpExecutor.P("@Status", Status),
            SpExecutor.P("@PageNumber", pageNumber),
            SpExecutor.P("@PageSize", pageSize));

        return new PagedResultDto<PurchaseOrderEntity>
        {
            Data = rows,
            TotalRecords = total
        };
    }

    // ============================================================
    // WORKFLOW
    // ============================================================
    public Task<bool> ApproveAsync(int id, string? updatedBy)
    {
        return SpExecutor.ScalarBoolAsync(
            _dbContext,
            "dbo.sp_PurchaseOrder_Approve",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));
    }

    public Task<bool> CancelAsync(int id, string? updatedBy)
    {
        return SpExecutor.ScalarBoolAsync(
            _dbContext,
            "dbo.sp_PurchaseOrder_Cancel",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));
    }
}
