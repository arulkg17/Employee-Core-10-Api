using System.Globalization;
using System.Text;

namespace Invoice.RAG.Models;

/// <summary>Due date, discount and penalty worked out by C# code (never by the model) from PO facts + agreement terms.</summary>
public sealed class PaymentSummary
{
    public string PoNumber { get; set; } = "";
    public int AgreementId { get; set; }
    public bool TermsReviewed { get; set; }
    public DateOnly? BasisDate { get; set; }
    public string? BasisDescription { get; set; }
    public int? PaymentTermsDays { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PenaltyStartsOn { get; set; }
    public DateOnly? EarlyDiscountDeadline { get; set; }
    public decimal? EarlyDiscountAmount { get; set; }
    public DateOnly AsOf { get; set; }
    public int DaysPastDue { get; set; }
    public int PenaltyDays { get; set; }
    public decimal? PenaltyEstimate { get; set; }
    public decimal? PoAmount { get; set; }
    public List<string> Assumptions { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    /// <summary>Plain lines for the prompt, so the model only has to word them.</summary>
    public string ToPromptLines()
    {
        var c = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine($"- Payment terms: {(PaymentTermsDays is null ? "not found" : PaymentTermsDays + " days")}");
        if (BasisDate is not null) sb.AppendLine($"- Counted from: {BasisDate:yyyy-MM-dd} ({BasisDescription})");
        if (DueDate is not null) sb.AppendLine($"- Due date: {DueDate:yyyy-MM-dd}");
        if (EarlyDiscountDeadline is not null)
            sb.AppendLine($"- Early-payment discount until {EarlyDiscountDeadline:yyyy-MM-dd}" +
                          (EarlyDiscountAmount is null ? "" : $" (saves {EarlyDiscountAmount.Value.ToString("0.00", c)})"));
        sb.AppendLine($"- As of {AsOf:yyyy-MM-dd}: {DaysPastDue} day(s) past due");
        if (PenaltyEstimate is not null) sb.AppendLine($"- Estimated late penalty: {PenaltyEstimate.Value.ToString("0.00", c)}");
        foreach (var a in Assumptions) sb.AppendLine($"- Assumption: {a}");
        foreach (var w in Warnings) sb.AppendLine($"- Warning: {w}");
        return sb.ToString();
    }
}
