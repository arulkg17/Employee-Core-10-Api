namespace Invoice.DTOs;

public class PurchaseOrderDetailDto
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public int ItemmasterId { get; set; }

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    // Output only: set by the server, ignored on create/update
    public decimal ReceivedQuantity { get; set; }
}
