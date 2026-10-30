using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

public class SalesInvoiceDetail
{
    public int Id { get; set; }
    public int SalesInvoiceId { get; set; }
    public int ItemmasterId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SalesInvoiceDetail> Details { get; set; } = new();
}

public class SalesInvoiceRequest : IValidatableObject
{
    /// <summary>Leave empty to auto-generate (INV-000001).</summary>
    [StringLength(30, ErrorMessage = "Maximum 30 characters")]
    public string? InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public DateTime? DueDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a customer")]
    public int CustomerId { get; set; }

    [StringLength(500, ErrorMessage = "Maximum 500 characters")]
    public string? Notes { get; set; }

    public List<DocLine> Details { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate.HasValue && DueDate.Value.Date < InvoiceDate.Date)
        {
            yield return new ValidationResult(
                "Due date cannot be before the invoice date",
                new[] { nameof(DueDate) });
        }
    }
}
