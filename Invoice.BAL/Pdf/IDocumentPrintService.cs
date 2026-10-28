public interface IDocumentPrintService
{
    Task<byte[]?> GetPurchaseOrderPdfAsync(int id);
    Task<byte[]?> GetSalesInvoicePdfAsync(int id);
    Task<byte[]?> GetReceiptPdfAsync(int id);
}