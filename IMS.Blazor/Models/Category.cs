using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Code is required")]
    [StringLength(20, ErrorMessage = "Maximum 20 characters")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Maximum 100 characters")]
    public string Name { get; set; } = "";

    [StringLength(250, ErrorMessage = "Maximum 250 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
