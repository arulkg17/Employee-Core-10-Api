using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;

[Table("Itemmaster")]
public class ItemmasterEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required int CategoryId { get; set; }

    [Required]
    [StringLength(25)]
    public required string ItemBarCode { get; set; }

    [Required]
    [StringLength(10)]
    public required string ItemCode { get; set; }

    [Required]
    [StringLength(100)]
    public required string ItemName { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    [Required]
    [StringLength(3)]
    public required string Uom { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Rate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumStock { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumStock { get; set; }

    public bool? IsActive { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
