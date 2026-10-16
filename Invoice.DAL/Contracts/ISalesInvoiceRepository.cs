using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.DAL.Contracts;

public interface ISalesInvoiceRepository
{
    Task<int> AddAsync(SalesInvoiceEntity salesInvoice);

    Task<IEnumerable<SalesInvoiceEntity>> GetAllAsync();

    Task<SalesInvoiceEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(SalesInvoiceEntity salesInvoice);

    Task<bool> DeleteAsync(int id, string? updatedBy = null);

    Task<PagedResultDto<SalesInvoiceEntity>> GetAllPagedAsync(
        string? invoiceNumber,
        int? customerId,
        string? status,
        int pageNumber,
        int pageSize);

    Task<bool> PostAsync(int id, string? updatedBy);

    Task<bool> CancelAsync(int id, string? updatedBy);
}
