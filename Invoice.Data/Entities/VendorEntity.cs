using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;


[Table("Vendor")]
public class VendorEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string VendorCode { get; set; }

    [Required]
    [StringLength(100)]
    public string VendorName { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    public string? MobileNo { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Address1 { get; set; }

    [StringLength(200)]
    public string? Address2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [StringLength(50)]
    public string? GstNo { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}