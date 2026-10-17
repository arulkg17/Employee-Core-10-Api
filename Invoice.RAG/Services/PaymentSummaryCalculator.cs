using Invoice.RAG.Models;

namespace Invoice.RAG.Services;

/// <summary>
/// Dates and money are worked out here in plain C# (easy to unit test), never by the language model.
/// Every guess is written into Assumptions / Warnings so the reader can see it.
/// </summary>
public static class PaymentSummaryCalculator
{
    public static PaymentSummary Calculate(
        PoFacts po, int agreementId, AgreementTerms terms, bool reviewed, DateOnly asOf, DateOnly? invoiceDate)
    {
        var s = new PaymentSummary
        {
            PoNumber = po.PoNumber,
            AgreementId = agreementId,
            TermsReviewed = reviewed,
            AsOf = asOf,
            PoAmount = po.Amount,
            PaymentTermsDays = terms.PaymentTermsDays
        };

        if (!reviewed)
            s.Warnings.Add("These terms were extracted by AI and have not been reviewed by a person. Check them against the PDF before relying on them.");
        if (po.Amount is null)
            s.Warnings.Add("The PO amount was not found, so money amounts cannot be calculated.");

        // Which date do the "days" count from?
        DateOnly? basis;
        if (terms.DueDateBasis is "invoice_date" or "goods_receipt_date")
        {
            if (invoiceDate is not null)
            {
                basis = invoiceDate;
                s.BasisDescription = "invoice/receipt date supplied in the request";
            }
            else
            {
                basis = po.PoDate;
                s.BasisDescription = "PO date, used as a stand-in";
                s.Warnings.Add($"The agreement counts days from the {terms.DueDateBasis.Replace('_', ' ')}, which is not known here. " +
                               "The PO date was used instead; pass invoiceDate for an exact result.");
            }
        }
        else
        {
            basis = invoiceDate ?? po.PoDate;
            s.BasisDescription = invoiceDate is not null ? "date supplied in the request" : "PO date";
            if (terms.DueDateBasis is null or "other")
                s.Assumptions.Add("The agreement does not clearly say what the payment days count from; the PO date was assumed.");
        }

        s.BasisDate = basis;

        if (basis is null)
        {
            s.Warnings.Add("No usable start date was found (the PO has no date and none was supplied).");
            return s;
        }

        if (terms.PaymentTermsDays is null)
        {
            s.Warnings.Add("No payment terms in days were found in the agreement.");
            return s;
        }

        s.DueDate = basis.Value.AddDays(terms.PaymentTermsDays.Value);
        s.DaysPastDue = Math.Max(0, asOf.DayNumber - s.DueDate.Value.DayNumber);

        var grace = terms.GracePeriodDays ?? 0;
        s.PenaltyStartsOn = s.DueDate.Value.AddDays(grace);
        s.PenaltyDays = Math.Max(0, asOf.DayNumber - s.PenaltyStartsOn.Value.DayNumber);

        if (terms.EarlyPaymentDiscountDays is not null && terms.EarlyPaymentDiscountPercent is not null)
        {
            s.EarlyDiscountDeadline = basis.Value.AddDays(terms.EarlyPaymentDiscountDays.Value);
            if (po.Amount is not null)
                s.EarlyDiscountAmount = Math.Round(po.Amount.Value * terms.EarlyPaymentDiscountPercent.Value / 100m, 2);
        }

        if (s.PenaltyDays > 0)
        {
            if (terms.LatePenaltyPercent is null || terms.LatePenaltyPeriod is null)
            {
                s.Warnings.Add("The payment is past due, but no late-penalty rate was found in the agreement.");
            }
            else if (po.Amount is not null)
            {
                var periods = terms.LatePenaltyPeriod switch
                {
                    "day" => s.PenaltyDays,
                    "week" => (int)Math.Ceiling(s.PenaltyDays / 7.0),
                    "month" => (int)Math.Ceiling(s.PenaltyDays / 30.0),
                    _ => 1
                };

                var penalty = po.Amount.Value * terms.LatePenaltyPercent.Value / 100m * periods;

                if (terms.LatePenaltyCapPercent is not null)
                {
                    var cap = po.Amount.Value * terms.LatePenaltyCapPercent.Value / 100m;
                    if (penalty > cap)
                    {
                        penalty = cap;
                        s.Assumptions.Add("The penalty was limited by the cap stated in the agreement.");
                    }
                }

                s.PenaltyEstimate = Math.Round(penalty, 2);
                s.Assumptions.Add("Simple (non-compounding) penalty on the PO amount; a part period counts as a full period; a month is 30 days.");
            }
        }

        return s;
    }
}
