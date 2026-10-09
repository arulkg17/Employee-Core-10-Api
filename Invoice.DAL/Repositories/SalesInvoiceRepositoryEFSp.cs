using System.Data.Common;
using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class SalesInvoiceRepositoryEFSp : ISalesInvoiceRepository
{
    private readonly AppDbContext _dbContext;

    public SalesInvoiceRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> AddAsync(SalesInvoiceEntity salesInvoice)
    {
        return SpExecutor.ScalarIntAsync(
            _dbContext,
            "dbo.sp_SalesInvoice_Insert",
            SpExecutor.P(
                "@InvoiceNumber",
                string.IsNullOrWhiteSpace(salesInvoice.InvoiceNumber) ? null : salesInvoice.InvoiceNumber),
            SpExecutor.P("@InvoiceDate", salesInvoice.InvoiceDate),
            SpExecutor.P("@DueDate", salesInvoice.DueDate),
            SpExecutor.P("@CustomerId", salesInvoice.CustomerId),
            SpExecutor.P("@Notes", salesInvoice.Notes),
            SpExecutor.Dec("@SubTotal", salesInvoice.SubTotal),
            SpExecutor.Dec("@TaxAmount", salesInvoice.TaxAmount),
            SpExecutor.Dec("@TotalAmount", salesInvoice.TotalAmount),
            SpExecutor.P("@CreatedBy", salesInvoice.CreatedBy));
    }

    public async Task<IEnumerable<SalesInvoiceEntity>> GetAllAsync()
    {
        return await _dbContext.SalesInvoices
            .FromSqlRaw("EXEC dbo.sp_SalesInvoice_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SalesInvoiceEntity?> GetByIdAsync(int id)
    {
        var invoices = await _dbContext.SalesInvoices
            .FromSqlRaw(
                "EXEC dbo.sp_SalesInvoice_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return invoices.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(SalesInvoiceEntity salesInvoice)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_SalesInvoice_Update",
            SpExecutor.P("@Id", salesInvoice.Id),
            SpExecutor.P("@InvoiceDate", salesInvoice.InvoiceDate),
            SpExecutor.P("@DueDate", salesInvoice.DueDate),
            SpExecutor.P("@CustomerId", salesInvoice.CustomerId),
            SpExecutor.P("@Notes", salesInvoice.Notes),
            SpExecutor.Dec("@SubTotal", salesInvoice.SubTotal),
            SpExecutor.Dec("@TaxAmount", salesInvoice.TaxAmount),
            SpExecutor.Dec("@TotalAmount", salesInvoice.TotalAmount),
            SpExecutor.P("@UpdatedBy", salesInvoice.UpdatedBy));

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, string? updatedBy = null)
    {
        var affectedRows = await SpExecutor.NonQueryAsync(
            _dbContext,
            "dbo.sp_SalesInvoice_Delete",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));

        return affectedRows > 0;
    }

    public async Task<PagedResultDto<SalesInvoiceEntity>> GetAllPagedAsync(
        string? invoiceNumber,
        int? customerId,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var (rows, total) = await SpExecutor.PagedAsync(
            _dbContext,
            "dbo.sp_SalesInvoice_GetPaged",
            Map,
            SpExecutor.P("@InvoiceNumber", invoiceNumber),
            SpExecutor.P("@CustomerId", customerId),
            SpExecutor.P("@Status", status),
            SpExecutor.P("@PageNumber", pageNumber),
            SpExecutor.P("@PageSize", pageSize));

        return new PagedResultDto<SalesInvoiceEntity>
        {
            Data = rows,
            TotalRecords = total
        };
    }

    public Task<bool> PostAsync(int id, string? updatedBy)
    {
        return SpExecutor.ScalarBoolAsync(
            _dbContext,
            "dbo.sp_SalesInvoice_Post",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));
    }

    public Task<bool> CancelAsync(int id, string? updatedBy)
    {
        return SpExecutor.ScalarBoolAsync(
            _dbContext,
            "dbo.sp_SalesInvoice_Cancel",
            SpExecutor.P("@Id", id),
            SpExecutor.P("@UpdatedBy", updatedBy));
    }

    private static SalesInvoiceEntity Map(DbDataReader r) => new()
    {
        Id = r.Int("Id"),
        InvoiceNumber = r.Text("InvoiceNumber"),
        InvoiceDate = r.Stamp("InvoiceDate"),
        DueDate = r.StampOrNull("DueDate"),
        CustomerId = r.Int("CustomerId"),
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
