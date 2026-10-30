using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

public class Customer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Code is required")]
    [StringLength(20, ErrorMessage = "Maximum 20 characters")]
    public string CustomerCode { get; set; } = "";

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Maximum 100 characters")]
    public string CustomerName { get; set; } = "";

    // ASSUMED: remove City / State if your Customer DTO does not have them
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string? City { get; set; }

    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string? State { get; set; }

    public bool IsActive { get; set; } = true;
}
