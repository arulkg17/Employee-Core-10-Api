using Microsoft.EntityFrameworkCore;

namespace Invoice.Data.Entities;

/// <summary>Row of dbo.vw_ItemStock (read only, loaded with FromSqlRaw).</summary>
[Keyless]
public class ItemStockEntity
{
    public int ItemmasterId { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public decimal ReceivedQuantity { get; set; }

    public decimal SoldQuantity { get; set; }

    public decimal OnHandQuantity { get; set; }
}
