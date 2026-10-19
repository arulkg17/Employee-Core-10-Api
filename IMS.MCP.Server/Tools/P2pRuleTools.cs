using System.ComponentModel;
using IMS.MCP.Server.Clients;
using IMS.MCP.Server.Skills;
using ModelContextProtocol.Server;

namespace IMS.MCP.Server.Tools;

/// <summary>
/// Rules that are numbers are applied by code, not by the model. The limits come from thresholds.json of the
/// ims-purchase-to-pay skill, so Finance changes a file instead of a prompt. These tools read nothing from the databases:
/// the caller passes the amounts it read with the other tools.
/// </summary>
[McpServerToolType]
public sealed class P2pRuleTools(SkillCatalog catalog)
{
    [McpServerTool(Name = "get_approval_level"),
     Description("Who must approve a purchase order of a given total (base currency), by the company approval matrix. " +
                 "A value exactly on a limit belongs to the lower band.")]
    public Task<string> GetApprovalLevel(
        [Description("The purchase order total.")] decimal amount,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(() =>
        {
            if (amount < 0) return Task.FromResult<object>(new { error = "'amount' cannot be negative." });

            var limits = catalog.Thresholds().ApprovalLimits;
            var band = limits.FirstOrDefault(l => l.UpTo is null || amount <= l.UpTo.Value);

            return Task.FromResult<object>(band is null
                ? (object)new { error = "No approval matrix is configured." }
                : new
                {
                    amount,
                    approver = band.Approver,
                    band = band.UpTo is null ? "above the highest limit" : $"up to {band.UpTo:0.##}",
                    secondApprovalForPayment = amount > catalog.Thresholds().SecondApprovalAbove
                });
        });

    [McpServerTool(Name = "evaluate_three_way_match"),
     Description("Check a vendor invoice against the purchase order and the goods received, using the company tolerances. " +
                 "Pass the amounts (and quantities when you have them) that you read with the other tools. " +
                 "Returns Matched, MatchedWithinTolerance or Mismatch with the reasons and the next action.")]
    public Task<string> EvaluateThreeWayMatch(
        [Description("Total value of the purchase order.")] decimal poAmount,
        [Description("Value of the goods or services accepted so far, at PO prices.")] decimal receivedAmount,
        [Description("Amount on the vendor invoice.")] decimal invoiceAmount,
        [Description("Optional. Ordered quantity.")] decimal? poQuantity = null,
        [Description("Optional. Accepted (not rejected) received quantity.")] decimal? receivedQuantity = null,
        [Description("Optional. Invoiced quantity.")] decimal? invoiceQuantity = null,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(() =>
        {
            var t = catalog.Thresholds();
            var reasons = new List<string>();

            // Quantities
            if (poQuantity is > 0 && receivedQuantity is not null)
            {
                var limit = poQuantity.Value * (1 + t.OverReceiptTolerancePercent / 100m);
                if (receivedQuantity > limit)
                    reasons.Add($"Over-receipt: received {receivedQuantity:0.##} against {poQuantity:0.##} ordered (limit {limit:0.##}, {t.OverReceiptTolerancePercent:0.##}% tolerance).");
            }
            if (receivedQuantity is not null && invoiceQuantity is not null)
            {
                var limit = receivedQuantity.Value * (1 + t.Match.QuantityTolerancePercent / 100m);
                if (invoiceQuantity > limit)
                    reasons.Add($"Invoiced quantity {invoiceQuantity:0.##} is more than received {receivedQuantity:0.##} beyond the {t.Match.QuantityTolerancePercent:0.##}% tolerance.");
            }

            // Amounts: the allowed difference is the LOWER of the percentage and the absolute limit.
            var diff = invoiceAmount - receivedAmount;
            var allowed = Math.Min(Math.Abs(receivedAmount) * t.Match.PriceTolerancePercent / 100m, t.Match.AmountToleranceAbsolute);
            if (Math.Abs(diff) > allowed)
                reasons.Add(diff > 0
                    ? $"Invoice {invoiceAmount:0.00} is {diff:0.00} above the received value {receivedAmount:0.00} (allowed {allowed:0.00})."
                    : $"Invoice {invoiceAmount:0.00} is {-diff:0.00} below the received value {receivedAmount:0.00} (allowed {allowed:0.00}); check for a missing line or credit.");

            var overPo = invoiceAmount - poAmount;
            var allowedPo = Math.Min(Math.Abs(poAmount) * t.Match.PriceTolerancePercent / 100m, t.Match.AmountToleranceAbsolute);
            if (overPo > allowedPo)
                reasons.Add($"Invoice {invoiceAmount:0.00} is above the PO total {poAmount:0.00} by more than the allowed {allowedPo:0.00}.");

            var status = reasons.Count > 0 ? "Mismatch" : diff == 0 ? "Matched" : "MatchedWithinTolerance";
            var action = status switch
            {
                "Matched" => "Release for payment in the next payment run, when payment terms and approvals allow.",
                "MatchedWithinTolerance" => "Release for payment and keep a note of the small difference.",
                _ => "Hold the invoice and send it to the buyer. Pay the undisputed part only if the vendor agreement allows partial payment."
            };

            return Task.FromResult<object>(new
            {
                status,
                difference = Math.Round(diff, 2),
                allowedDifference = Math.Round(allowed, 2),
                reasons,
                nextAction = action,
                rules = "ims-purchase-to-pay rules 8, 12, 13 (over-receipt, payable only when matched, tolerance)"
            });
        });
}
