using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;

[Table("PurchaseOrderDetail")]
public class PurchaseOrderDetailEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseOrderId { get; set; }

    [Required]
    public int ItemmasterId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    // NEW: quantity already received through posted receipts (maintained by sp_Receipt_Post / sp_Receipt_Cancel)
    [Column(TypeName = "decimal(18,2)")]
    public decimal ReceivedQuantity { get; set; }
}
