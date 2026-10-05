using Invoice.Data.Entities;

namespace Invoice.DAL.Contracts;

public interface IPurchaseOrderDetailRepository
{
    Task<int> AddAsync(PurchaseOrderDetailEntity detail);

    Task<IEnumerable<PurchaseOrderDetailEntity>>
        GetByPurchaseOrderIdAsync(int purchaseOrderId);

    Task<PurchaseOrderDetailEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(PurchaseOrderDetailEntity detail);

    Task<bool> DeleteAsync(int id);

    Task<bool> DeleteByPurchaseOrderIdAsync(int purchaseOrderId);
}