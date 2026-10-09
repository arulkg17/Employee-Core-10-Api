namespace Invoice.DTOs;

public class ReceiptDetailDto
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    /// <summary>The purchase order line being received (required).</summary>
    public int PurchaseOrderDetailId { get; set; }

    /// <summary>Output only: taken from the purchase order line.</summary>
    public int ItemmasterId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    /// <summary>Output only: rate / discount / tax come from the purchase order line.</summary>
    public decimal Rate { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }
}
