
namespace Invoice.DTOs;

public class ItemmasterFilterDto
{
    public int? CategoryId { get; set; }

    public string? ItemBarCode { get; set; }

    public string? ItemCode { get; set; }

    public string? ItemName { get; set; }

    public string? Uom { get; set; }

    public bool? IsActive { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}