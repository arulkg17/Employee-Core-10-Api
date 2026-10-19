# Vendor invoice and payment rules

## Contents
- Before paying
- Payment terms and due date
- Early-payment discount
- Late interest
- Disputes
- Holds
- Payment run and approval
- Credit notes and partial payments

## Before paying
An invoice is payable only when all are true: the vendor is active; the invoice is approved; the invoice number is not a duplicate for that vendor; PO, receipt and invoice match within tolerance.

Tolerance (default): quantity 5%; amount difference at most 2% of the received value and at most 50.00 (both limits apply). Use `evaluate_three_way_match`.

## Payment terms and due date
1. Read the terms from the vendor agreement: number of days and the basis (invoice date, invoice approval, goods or equipment acceptance, approved service report, proof of delivery).
2. If there is no agreement, use Net 30 from the invoice approval date.
3. Never compute the due date yourself: `get_po_payment_summary` does it from the stored terms. If the terms are missing or not yet reviewed by a person, say so.

Sample agreements show how much the basis varies: net 30 after invoice approval, net 45 after equipment acceptance, net 30 after the monthly service report, net 15 after the event, net 60 after invoice receipt.

## Early-payment discount
Applies only when the agreement states it and the payment is made within its window (for example 2% if paid within 10 days). After the window the full amount is due. The discount is taken on the invoice amount excluding disputed parts.

## Late interest
- Only if the agreement states it (examples in the samples: 0.5% to 1.5% per month, or a flat fee per overdue invoice).
- Only on undisputed amounts, only after any grace period, and capped by applicable law.
- No agreement clause means no late interest.

## Disputes
- A dispute must be raised in writing and in good faith before the due date.
- Pay the undisputed part by the due date if the agreement allows partial payment.
- The disputed part carries no late interest while it is being resolved. Record the dispute reason and owner.

## Holds
Hold the invoice (do not pay) when: duplicate invoice number; no matching receipt; outside match tolerance; PO not approved or cancelled; vendor inactive; missing agreement documents required by the agreement (for example certificates); amount or bank details changed without verification.

## Payment run and approval
- Payments are released in the weekly run (default Friday). An invoice due before the next run is paid in the earlier run to avoid lateness.
- A payment above 25,000 needs a second approver.
- The approver of the invoice cannot release its payment.

## Credit notes and partial payments
- A vendor credit note is netted against the next payment to that vendor, or refunded if no further payment is due.
- A partial payment leaves the balance open with its own due date; late interest runs only on the unpaid undisputed balance.
