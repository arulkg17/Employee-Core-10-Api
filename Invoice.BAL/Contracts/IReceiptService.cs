using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface IReceiptService
{
    Task<int> AddAsync(ReceiptDto receipt);

    Task<IEnumerable<ReceiptDto>> GetAllAsync();

    Task<ReceiptDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(ReceiptDto receipt);

    Task<bool> DeleteAsync(int id, string? updatedBy = null);

    Task<PagedResultDto<ReceiptDto>> GetAllPagedAsync(
        string? receiptNumber,
        int? purchaseOrderId,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize);

    Task<bool> PostAsync(int id, string? updatedBy);

    Task<bool> CancelAsync(int id, string? updatedBy);
}
