namespace Invoice.RAG.Models;

public sealed class AskRequest
{
    public string Question { get; set; } = "";
    public int? VendorId { get; set; }
    public string? PoNumber { get; set; }
    public int? AgreementId { get; set; }
    public int? TopK { get; set; }
}

public sealed record SourceRef(
    int Number, int AgreementId, string AgreementTitle, int Page, string? Section, double Score, string Evidence);

/// <summary>Retrieval only (no model answer): used to check and tune what the search finds.</summary>
public sealed class SearchRequest
{
    public string Query { get; set; } = "";
    public int? VendorId { get; set; }
    public int? AgreementId { get; set; }
    public int? TopK { get; set; }
}

public sealed record SearchHit(
    int AgreementId, int VendorId, string AgreementTitle, int Page, string? Section, double Score, string Text);

public sealed class AskResponse
{
    public string Answer { get; set; } = "";
    public List<SourceRef> Sources { get; set; } = [];
    public PoFacts? PurchaseOrder { get; set; }
    public PaymentSummary? PaymentSummary { get; set; }
    public long ElapsedMs { get; set; }
}

/// <summary>Facts about a purchase order, read from CoreAPI (the business system is the source of truth).</summary>
public sealed class PoFacts
{
    public string PoNumber { get; set; } = "";
    public int? Id { get; set; }
    public int? VendorId { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public string? Status { get; set; }
    public DateOnly? PoDate { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}

public sealed record VendorFacts(int Id, string? Code, string? Name);
