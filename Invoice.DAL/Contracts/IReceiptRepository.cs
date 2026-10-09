using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.DAL.Contracts;

public interface IReceiptRepository
{
    Task<int> AddAsync(ReceiptEntity receipt);

    Task<IEnumerable<ReceiptEntity>> GetAllAsync();

    Task<ReceiptEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(ReceiptEntity receipt);

    Task<bool> DeleteAsync(int id, string? updatedBy = null);

    Task<PagedResultDto<ReceiptEntity>> GetAllPagedAsync(
        string? receiptNumber,
        int? purchaseOrderId,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize);

    Task<bool> PostAsync(int id, string? updatedBy);

    Task<bool> CancelAsync(int id, string? updatedBy);
}
