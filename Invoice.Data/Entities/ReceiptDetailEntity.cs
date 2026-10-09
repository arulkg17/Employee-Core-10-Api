using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;

[Table("ReceiptDetail")]
public class ReceiptDetailEntity
{
    [Key]
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public int PurchaseOrderDetailId { get; set; }

    public int ItemmasterId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ReceivedQuantity { get; set; }

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
}
