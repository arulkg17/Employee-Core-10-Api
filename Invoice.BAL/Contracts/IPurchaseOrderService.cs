using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

public interface IPurchaseOrderService
{
    Task<int> AddAsync(PurchaseOrderDto purchaseOrder);

    Task<IEnumerable<PurchaseOrderDto>> GetAllAsync();

    Task<PurchaseOrderDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(PurchaseOrderDto purchaseOrder);

    Task<bool> DeleteAsync(int id);

    Task<PagedResultDto<PurchaseOrderDto>> GetAllPagedAsync(
        string? PONumber,
        int? VendorId,
        string? Status,
        int pageNumber,
        int pageSize);

    // NEW
    Task<bool> ApproveAsync(int id, string? updatedBy);

    Task<bool> CancelAsync(int id, string? updatedBy);
}
