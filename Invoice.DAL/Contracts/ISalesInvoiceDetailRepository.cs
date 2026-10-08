using Invoice.Data.Entities;

namespace Invoice.DAL.Contracts;

public interface ISalesInvoiceDetailRepository
{
    Task<int> AddAsync(SalesInvoiceDetailEntity detail);

    Task<IEnumerable<SalesInvoiceDetailEntity>> GetBySalesInvoiceIdAsync(int salesInvoiceId);

    Task<SalesInvoiceDetailEntity?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(SalesInvoiceDetailEntity detail);

    Task<bool> DeleteAsync(int id);

    Task<bool> DeleteBySalesInvoiceIdAsync(int salesInvoiceId);
}
