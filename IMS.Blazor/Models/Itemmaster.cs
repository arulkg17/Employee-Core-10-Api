using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

public class Itemmaster : IValidatableObject
{
    public static readonly string[] UomList = { "KGS", "NOS", "LTR", "DOZ" };

    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Category is required")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Bar code is required")]
    [StringLength(25, ErrorMessage = "Maximum 25 characters")]
    public string ItemBarCode { get; set; } = "";

    [Required(ErrorMessage = "Code is required")]
    [StringLength(10, ErrorMessage = "Maximum 10 characters")]
    public string ItemCode { get; set; } = "";

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Maximum 100 characters")]
    public string ItemName { get; set; } = "";

    [StringLength(250, ErrorMessage = "Maximum 250 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "UOM is required")]
    public string Uom { get; set; } = "";

    [Range(0, double.MaxValue, ErrorMessage = "Cannot be negative")]
    public decimal? Rate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Cannot be negative")]
    public decimal MinimumStock { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Cannot be negative")]
    public decimal MaximumStock { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaximumStock < MinimumStock)
        {
            yield return new ValidationResult(
                "Maximum cannot be below minimum",
                new[] { nameof(MaximumStock) });
        }
    }
}
