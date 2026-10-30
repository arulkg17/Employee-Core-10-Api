using System.ComponentModel.DataAnnotations;

namespace IMS.Blazor.Models;

/// <summary>Receipt and Sales Invoice share one workflow.</summary>
public static class DocStatuses
{
    public static readonly string[] All = { "Draft", "Posted", "Cancelled" };
}

public class ReceiptDetail
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int PurchaseOrderDetailId { get; set; }
    public int ItemmasterId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class Receipt
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public int PurchaseOrderId { get; set; }
    public int VendorId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ReceiptDetail> Details { get; set; } = new();
}

/// <summary>Only the PO line and the quantity are sent; rate and tax come from the PO line.</summary>
public class ReceiptLineRequest
{
    public int PurchaseOrderDetailId { get; set; }
    public decimal ReceivedQuantity { get; set; }
}

public class ReceiptRequest
{
    /// <summary>Leave empty to auto-generate (GRN-000001).</summary>
    [StringLength(30, ErrorMessage = "Maximum 30 characters")]
    public string? ReceiptNumber { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    [Range(1, int.MaxValue, ErrorMessage = "Select a purchase order")]
    public int PurchaseOrderId { get; set; }

    [StringLength(500, ErrorMessage = "Maximum 500 characters")]
    public string? Notes { get; set; }

    public List<ReceiptLineRequest> Details { get; set; } = new();
}
