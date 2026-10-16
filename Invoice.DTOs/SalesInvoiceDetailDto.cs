namespace Invoice.DTOs;

public class SalesInvoiceDetailDto
{
    public int Id { get; set; }

    public int SalesInvoiceId { get; set; }

    public int ItemmasterId { get; set; }

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    /// <summary>Output only: calculated by the server.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Output only: calculated by the server.</summary>
    public decimal LineTotal { get; set; }
}
