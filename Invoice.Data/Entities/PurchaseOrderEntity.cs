using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;

[Table("PurchaseOrder")]
public class PurchaseOrderEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string PONumber { get; set; } = string.Empty;

    [Required]
    public DateTime PODate { get; set; }

    [Required]
    public int VendorId { get; set; }

    [Required]
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

    public List<PurchaseOrderDetailEntity> Details { get; set; }
        = new();
}