using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.DAL.Contracts;

public interface IPurchaseOrderRepository
{
    Task<int> AddAsync(PurchaseOrderEntity purchaseOrder);

    Task<IEnumerable<PurchaseOrderEntity>> GetAllAsync();

    Task<PurchaseOrderEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(PurchaseOrderEntity purchaseOrder);

    Task<bool> DeleteAsync(int id);

    Task<PagedResultDto<PurchaseOrderEntity>> GetAllPagedAsync(
        string? PONumber,
        int? VendorId,
        string? Status,
        int pageNumber,
        int pageSize);

    // NEW: workflow
    Task<bool> ApproveAsync(int id, string? updatedBy);

    Task<bool> CancelAsync(int id, string? updatedBy);
}
