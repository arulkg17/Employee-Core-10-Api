# Goods receipt (GRN) rules

## Contents
- What a receipt records
- Over- and under-receipt
- Rejected and damaged goods
- Services and acceptance
- Corrections

## What a receipt records
One receipt belongs to one Approved PO. It records the receipt date, the quantity accepted, the quantity rejected with a reason, and who received it. Several receipts can exist for one PO (partial deliveries).

## Over- and under-receipt
- Received quantity can be up to 5% above the ordered quantity (default). Example: ordered 100, up to 105 can be received.
- Above that, the extra quantity is returned or the buyer approves it in writing; the PO is not silently changed.
- Under-receipt leaves the PO Partially Received. The open balance stays on the PO until it is received or closed.
- The payable value of a receipt is the accepted quantity at the PO price.

## Rejected and damaged goods
- Record the quantity and the reason (damaged, wrong item, not as specified).
- Rejected quantity is never payable and is not counted as received.
- Vendor remedies (replace, credit, re-perform) come from the agreement warranty clause. Search it with `search_vendor_agreements`.

## Services and acceptance
- Services and equipment are received when someone with authority signs acceptance (service report approved, equipment accepted).
- If the agreement says payment is due "after acceptance" or "after the approved service report", the acceptance date starts the payment clock, not the delivery or invoice date.

## Corrections
- A receipt that is not yet matched can be corrected by finance or the receiver's manager.
- A receipt already matched to an invoice is reversed with a reversing entry and re-entered. It is never edited.
