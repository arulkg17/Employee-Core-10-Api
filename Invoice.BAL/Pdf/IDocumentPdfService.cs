using Invoice.DTOs;

public interface IDocumentPdfService
{
    byte[] GeneratePurchaseOrder(PurchaseOrderDto po, string vendorName,
                                 IReadOnlyDictionary<int, string> itemNames);

    byte[] GenerateSalesInvoice(SalesInvoiceDto inv, string customerName,
                                IReadOnlyDictionary<int, string> itemNames);

    byte[] GenerateReceipt(ReceiptDto rc, string vendorName, string poNumber,
                           IReadOnlyDictionary<int, string> itemNames);
}