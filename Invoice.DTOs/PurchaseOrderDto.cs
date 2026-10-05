namespace Invoice.DTOs;

public class PurchaseOrderDto
{
    public int Id { get; set; }

    public string PONumber { get; set; } = string.Empty;

    public DateTime PODate { get; set; }

    public int VendorId { get; set; }

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

    public List<PurchaseOrderDetailDto> Details { get; set; }
        = new();
}