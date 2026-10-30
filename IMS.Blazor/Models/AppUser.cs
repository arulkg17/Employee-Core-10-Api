using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

/// <summary>Named AppUser so it never clashes with System / Blazor names.</summary>
public class AppUser
{
    public int Id { get; set; }

    [Required(ErrorMessage = "User name is required")]
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    [StringLength(100, ErrorMessage = "Maximum 100 characters")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string FirstName { get; set; } = "";

    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string LastName { get; set; } = "";

    [StringLength(100, ErrorMessage = "Maximum 100 characters")]
    public string DisplayName { get; set; } = "";

    [Required(ErrorMessage = "Phone is required")]
    [StringLength(20, ErrorMessage = "Maximum 20 characters")]
    public string PhoneNumber { get; set; } = "";

    [StringLength(20, ErrorMessage = "Maximum 20 characters")]
    public string? AlternatePhone { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(150, ErrorMessage = "Maximum 150 characters")]
    public string AddressLine1 { get; set; } = "";

    [StringLength(150, ErrorMessage = "Maximum 150 characters")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "State is required")]
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string State { get; set; } = "";

    [Required(ErrorMessage = "Zip code is required")]
    [StringLength(15, ErrorMessage = "Maximum 15 characters")]
    public string ZipCode { get; set; } = "";

    [Required(ErrorMessage = "Country is required")]
    [StringLength(50, ErrorMessage = "Maximum 50 characters")]
    public string Country { get; set; } = "";

    public DateTime? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginDate { get; set; }
}
