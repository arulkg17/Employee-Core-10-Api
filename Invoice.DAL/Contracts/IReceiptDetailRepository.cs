using Invoice.Data.Entities;

namespace Invoice.DAL.Contracts;

public interface IReceiptDetailRepository
{
    Task<int> AddAsync(ReceiptDetailEntity detail);

    Task<IEnumerable<ReceiptDetailEntity>> GetByReceiptIdAsync(int receiptId);

    Task<ReceiptDetailEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(ReceiptDetailEntity detail);

    Task<bool> DeleteAsync(int id);

    Task<bool> DeleteByReceiptIdAsync(int receiptId);
}
