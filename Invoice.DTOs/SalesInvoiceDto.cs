namespace Invoice.DTOs;

public class SalesInvoiceDto
{
    public int Id { get; set; }

    /// <summary>Optional. Leave empty to auto-generate (INV-000001).</summary>
    public string? InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    public int CustomerId { get; set; }

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

    public List<SalesInvoiceDetailDto> Details { get; set; } = new();
}
