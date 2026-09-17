
namespace Invoice.DTOs;

public class ItemmasterDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string ItemBarCode { get; set; }

    public string ItemCode { get; set; }

    public string ItemName { get; set; }

    public string? Description { get; set; }

    public string Uom { get; set; }

    public decimal? Rate { get; set; }

    public decimal? MinimumStock { get; set; }

    public decimal? MaximumStock { get; set; }

    public bool? IsActive { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}