namespace Invoice.DTOs;

public class ReceiptDto
{
    public int Id { get; set; }

    /// <summary>Optional. Leave empty to auto-generate (GRN-000001).</summary>
    public string? ReceiptNumber { get; set; }

    public DateTime ReceiptDate { get; set; }

    public int PurchaseOrderId { get; set; }

    /// <summary>Output only: taken from the purchase order.</summary>
    public int VendorId { get; set; }

    /// <summary>Draft / Posted / Cancelled. Output only: changed with the Post and Cancel actions.</summary>
    public string Status { get; set; } = "Draft";

    public string? Notes { get; set; }

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsDeleted { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public List<ReceiptDetailDto> Details { get; set; } = new();
}
