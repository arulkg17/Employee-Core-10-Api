using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface ISalesInvoiceService
{
    Task<int> AddAsync(SalesInvoiceDto salesInvoice);

    Task<IEnumerable<SalesInvoiceDto>> GetAllAsync();

    Task<SalesInvoiceDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(SalesInvoiceDto salesInvoice);

    Task<bool> DeleteAsync(int id, string? updatedBy = null);

    Task<PagedResultDto<SalesInvoiceDto>> GetAllPagedAsync(
        string? invoiceNumber,
        int? customerId,
        string? status,
        int pageNumber,
        int pageSize);

    Task<bool> PostAsync(int id, string? updatedBy);

    Task<bool> CancelAsync(int id, string? updatedBy);
}
