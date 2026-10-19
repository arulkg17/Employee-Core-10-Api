using System.ComponentModel;
using ModelContextProtocol.Server;

namespace IMS.MCP.Server.Prompts;

/// <summary>
/// Ready-made workflows. In clients that show MCP prompts (for example VS Code Copilot as slash commands) the user picks one
/// and fills in the PO number or vendor; the text tells the model which skill and tools to use. Needs WithPromptsFromAssembly().
/// </summary>
[McpServerPromptType]
public static class P2pPrompts
{
    [McpServerPrompt(Name = "review_purchase_order"),
     Description("Review a purchase order against the purchase-to-pay rules and its vendor agreement.")]
    public static string ReviewPurchaseOrder([Description("PO number, for example PO-0001")] string poNumber) => $"""
        Review purchase order {poNumber} for the purchase-to-pay process.
        1. Call get_skill with name 'ims-purchase-to-pay' and follow it.
        2. Call get_purchase_order for {poNumber}, then find the vendor with find_vendor if you only have a code or name.
        3. Call get_approval_level with the PO total and say who must approve it.
        4. Call get_po_payment_summary for {poNumber} and report the due date, discount and any late penalty exactly as returned.
        5. Check the vendor agreement with search_vendor_agreements for delivery, warranty and termination points that matter for this PO.
        Answer with: decision or status, reasons with their source (rule number or agreement title and page), missing data, next action and role.
        """;

    [McpServerPrompt(Name = "can_we_pay_invoice"),
     Description("Decide whether a vendor invoice can be paid, using the purchase-to-pay rules.")]
    public static string CanWePayInvoice(
        [Description("PO number the invoice belongs to")] string poNumber,
        [Description("Invoice amount")] string invoiceAmount,
        [Description("Value received so far (optional, say 'unknown' if you do not have it)")] string receivedAmount = "unknown") => $"""
        Can we pay a vendor invoice of {invoiceAmount} for purchase order {poNumber}? Value received so far: {receivedAmount}.
        1. Call get_skill with name 'ims-purchase-to-pay' and follow the answer template in references/agent-playbook.md (get_skill_reference).
        2. Call get_purchase_order and get_po_payment_summary for {poNumber}.
        3. If the received value is known, call evaluate_three_way_match with the PO total, the received value and the invoice amount.
           If it is unknown, say that no decision can be made without the receipt.
        4. Give the decision first (Release, Hold or Cannot decide), then reasons with sources, due date, and the next action and role.
        You can only explain; you cannot approve or pay.
        """;
}
