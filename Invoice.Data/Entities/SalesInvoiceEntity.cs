using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;

[Table("SalesInvoice")]
public class SalesInvoiceEntity
{
    [Key]
    public int Id { get; set; }

    [StringLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    public int CustomerId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public bool IsDeleted { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
    [ForeignKey(nameof(SalesInvoiceDetailEntity.SalesInvoiceId))]

    public List<SalesInvoiceDetailEntity> Details { get; set; } = new();
}
