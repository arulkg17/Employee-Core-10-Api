namespace Invoice.DTOs;

public class ItemStockDto
{
    public int ItemmasterId { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public decimal ReceivedQuantity { get; set; }

    public decimal SoldQuantity { get; set; }

    public decimal OnHandQuantity { get; set; }
}
