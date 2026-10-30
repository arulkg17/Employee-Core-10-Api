namespace IMS.Blazor.Models;

/// <summary>One row of the stock view: posted receipts minus posted sales invoices.</summary>
public class ItemStock
{
    public int ItemmasterId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal ReceivedQuantity { get; set; }
    public decimal SoldQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
}
