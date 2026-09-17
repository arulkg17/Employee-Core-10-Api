
namespace Invoice.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; }
    public string CustomerName { get; set; }
    public string? ContactPerson { get; set; }
    public string? MobileNo { get; set; }
    public string? Email { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public string? GstNo { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}