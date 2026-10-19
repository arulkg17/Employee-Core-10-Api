# Purchase order rules

## Contents
- Lifecycle
- Approval matrix
- Changes after approval
- Cancel and close
- Vendor and agreement checks
- Roles (segregation of duties)

## Lifecycle
| Status | Meaning | What can happen next |
|---|---|---|
| Draft | Being prepared | Edit, submit, delete |
| Submitted | Waiting for approval | Approve (IMS Approve action), reject, return to Draft |
| Approved | Vendor can deliver | Receive, change (re-approval), Cancel (no receipt yet) |
| Partially Received | Some quantity received | Receive more, close the open balance |
| Received | Full quantity received | Match invoice, Closed |
| Closed | Received and invoiced, or balance closed | Nothing; reopen only by finance |
| Cancelled | Withdrawn | Nothing |

The exact status names in the IMS database may differ. Use the status the tool returns and map it to the closest row; say so if unsure.

## Approval matrix (default, on PO total in base currency)
| PO total | Approver |
|---|---|
| up to 5,000 | Department manager |
| 5,000.01 to 25,000 | Finance manager |
| above 25,000 | CFO |

Use `get_approval_level` for the answer; do not compare amounts yourself. The boundary value belongs to the lower band (5,000 is a department manager PO).

## Changes after approval
- Price change above 2%, any quantity change, or a different vendor: back to Submitted and re-approved at the approver level of the new total.
- Delivery date or contact changes do not need re-approval.

## Cancel and close
- Cancel is allowed only while nothing has been received. It is an IMS action (PurchaseOrder Cancel) done by the buyer or an approver, with a reason.
- After a receipt, the open balance (ordered minus received) can be closed. Received quantities stay payable.

## Vendor and agreement checks
- The vendor must be active.
- If a vendor agreement exists, the PO date should be inside its term. Outside the term: warn the buyer, do not block.
- Prices on an accepted PO are fixed for that PO; catalogue or rate changes in an agreement apply to new POs only.

## Roles (segregation of duties)
- Creator of a PO cannot approve it.
- Receiver of goods cannot approve the vendor invoice for the same PO.
- Approver of an invoice cannot release its payment.
