using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

public static class PoStatuses
{
    public static readonly string[] All = { "Draft", "Approved", "PartiallyReceived", "Received", "Cancelled" };
}

public class PurchaseOrderDetail
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ItemmasterId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Quantity already received through posted receipts (read only).</summary>
    public decimal ReceivedQuantity { get; set; }
}

/// <summary>What the API returns.</summary>
public class PurchaseOrder
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = "";
    public DateTime PoDate { get; set; }
    public int VendorId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseOrderDetail> Details { get; set; } = new();
}

/// <summary>What the Create / Update screen edits and sends.</summary>
public class PurchaseOrderRequest
{
    [Required(ErrorMessage = "PO number is required")]
    [StringLength(30, ErrorMessage = "Maximum 30 characters")]
    public string PoNumber { get; set; } = "";

    public DateTime PoDate { get; set; } = DateTime.Today;

    [Range(1, int.MaxValue, ErrorMessage = "Select a vendor")]
    public int VendorId { get; set; }

    [StringLength(500, ErrorMessage = "Maximum 500 characters")]
    public string? Notes { get; set; }

    public List<DocLine> Details { get; set; } = new();
}
