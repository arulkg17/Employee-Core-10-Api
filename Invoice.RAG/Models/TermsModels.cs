namespace Invoice.RAG.Models;

/// <summary>Payment-related terms read from an agreement. Every field is optional: null = not stated.</summary>
public sealed class AgreementTerms
{
    public int? PaymentTermsDays { get; set; }
    public string? PaymentTermsText { get; set; }
    /// <summary>invoice_date | goods_receipt_date | po_date | other</summary>
    public string? DueDateBasis { get; set; }
    public decimal? EarlyPaymentDiscountPercent { get; set; }
    public int? EarlyPaymentDiscountDays { get; set; }
    public int? GracePeriodDays { get; set; }
    public decimal? LatePenaltyPercent { get; set; }
    /// <summary>day | week | month | flat</summary>
    public string? LatePenaltyPeriod { get; set; }
    public decimal? LatePenaltyCapPercent { get; set; }
    public string? DeliveryPenaltyText { get; set; }
    public int? TerminationNoticeDays { get; set; }
    public string? AgreementStartDate { get; set; }
    public string? AgreementEndDate { get; set; }
    public List<TermEvidence> Evidence { get; set; } = [];
}

public sealed class TermEvidence
{
    public string Field { get; set; } = "";
    public int? Page { get; set; }
    public string? Quote { get; set; }
}

public sealed record StoredTermsDto(
    int AgreementId, AgreementTerms Terms, DateTime ExtractedUtc, bool Reviewed, string? ReviewedBy);
