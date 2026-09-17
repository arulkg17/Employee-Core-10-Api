using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice.Data.Entities;

[Table("Category")]
public class CategoryEntity
{
    [Key]
    public int Id { get; set; }
    [Required]
    [StringLength(10)]
    public string Code { get; set; }
    [Required]
    [StringLength(25)]

    public string Name { get; set; }

    [StringLength(25)]
    public string? Description { get; set; }
    [Required]
    public bool? IsActive { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }


}
