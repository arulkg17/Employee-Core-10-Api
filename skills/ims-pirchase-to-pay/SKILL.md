---
name: ims-purchase-to-pay
description: Standard purchase-to-pay rules for the Invoice Management System (IMS): purchase order lifecycle and approval limits, goods receipts (GRN), three-way match tolerances, vendor payment terms, early-payment discounts, late interest, payment holds and release. Use this skill whenever a question or task involves a purchase order, a receipt or goods received, a vendor invoice, a vendor payment, a due date, a payment hold or approval, or "can we pay this", even when the user does not say "policy" or "rules".
---

# IMS purchase-to-pay rules

These are the company's standard rules for the purchase-to-pay cycle: purchase order (PO) -> goods receipt (GRN) -> vendor invoice -> vendor payment.
The numbers below are defaults for the sample company. Finance owns the real values; they live in `thresholds.json` and the tools read them from there.

## Order of authority
1. The vendor agreement wins for anything it states: payment days, due-date basis, late interest, grace period, discount, warranty, termination.
2. These rules apply when the agreement is silent or no agreement exists.
3. General practice only fills gaps, and the answer must say so.

If an agreement and these rules differ, say which one you used and why.

## Core rules

**Purchase orders**
1. A PO is raised for an active vendor and needs an approver by value (PO total, base currency):
   up to 5,000 department manager; up to 25,000 finance manager; above 25,000 CFO.
2. The person who creates a PO cannot approve it. Approval is done by the IMS Approve action only.
3. Only an Approved PO can be received against. A Draft or Cancelled PO cannot.
4. Changing price by more than 2% or quantity after approval sends the PO back for re-approval.
5. A PO can be cancelled before any receipt. After a receipt, only the open balance can be closed or cancelled.
6. The PO date should fall inside the vendor agreement term. If it does not, flag it; do not block it.

**Goods receipts**
7. Every receipt is against one Approved PO and carries the receipt date, the quantity accepted and the quantity rejected.
8. Over-receipt is allowed up to 5% of the ordered quantity. Above that it is rejected or needs buyer approval.
9. Rejected or damaged quantities are recorded with a reason and are never payable.
10. Services and equipment need an acceptance sign-off. The acceptance date, not the delivery date, can start the payment clock when the agreement says so.
11. A receipt that has been matched to an invoice is reversed, never edited.

**Vendor invoices and payments**
12. Pay only invoices that are approved and three-way matched (PO, receipt, invoice).
13. Match tolerance: quantity 5%; price/amount the lower of 2% and 50.00 of the received value. Outside tolerance the invoice is held.
14. An invoice number is unique per vendor. A repeat is a duplicate and is held.
15. Payment terms come from the agreement. Without one, use Net 30 from the invoice approval date.
16. An early-payment discount applies only if paid within the discount window stated in the agreement.
17. Late interest applies only if the agreement states it, only to undisputed amounts, and only after any grace period. Without an agreement clause there is none.
18. A disputed invoice: pay the undisputed part by the due date if the agreement allows it; no late interest on the disputed part.
19. Payments are released in the weekly payment run (Friday). Payments above 25,000 need a second approver. The person who approved the invoice cannot release the payment.

## Which tool answers what
- Vendor, PO, PO totals, status: `find_vendor`, `get_vendor`, `get_vendor_purchase_orders`, `get_purchase_order`.
- Agreement wording (payment days, discount, interest, warranty, termination): `search_vendor_agreements`, `get_agreement_terms`.
- Due date, discount, days past due, penalty for a PO: `get_po_payment_summary`. Never calculate dates or money yourself.
- Who must approve a PO value: `get_approval_level`.
- Is an invoice within match tolerance: `evaluate_three_way_match` (pass the numbers you read from the tools).
- A rule not covered above: `search_p2p_policy`, or read the reference files below.

## How to answer
- State the decision first (for example "Hold" or "Release"), then the reasons, then which rule or agreement clause each reason comes from.
- Say what data is missing instead of guessing it (no receipt found, no agreement terms, PO not approved).
- You can read and explain. You cannot approve, cancel, post or pay: tell the user which IMS action and which role do that.
- Use the numbers exactly as the tools return them.

## Reference files (read only when needed)
- `references/purchase-orders.md`: lifecycle and statuses, approval matrix, changes, cancellation, vendor checks.
- `references/receipts.md`: receipt rules, over/under receipt, rejected goods, acceptance, reversals.
- `references/vendor-payments.md`: due-date basis, discount, interest, disputes, holds, payment run, credit notes.
- `references/agent-playbook.md`: step-by-step tool sequences and answer templates for common questions.
- `thresholds.json`: the numeric defaults (approval limits, tolerances, payment run day).
