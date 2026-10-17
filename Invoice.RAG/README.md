# Invoice.RAG

Web API that lets you upload **vendor agreement PDFs**, then ask questions that combine
**unstructured text (the PDF)** with **structured business data (vendor + purchase order from CoreAPI)**:
payment terms, due date, late-payment penalty, early-payment discount, termination notice...

```
PDF upload ─► text per page ─► chunks (+page, +clause heading) ─► embeddings (Ollama) ─► store
                                                                                          │
Question + optional poNumber/vendorId ─► CoreAPI (PO, vendor, user's JWT forwarded)       │
                                      ─► retrieve best chunks for that vendor ◄───────────┘
                                      ─► C# computes due date / penalty from the stored terms
                                      ─► model words the answer and cites [1], [2] (document + page)
```

## Design rules
* **The model never does arithmetic.** It reads terms (JSON) and words answers; due dates and penalties are computed in
  `PaymentSummaryCalculator` (plain C#, unit-testable). Assumptions and warnings are returned with every result.
* **Extracted terms are a draft.** They are stored as *not reviewed* until a person corrects/approves them with
  `PUT /api/v1/vendor-agreements/{id}/terms`. Every answer says whether the terms were reviewed.
* **Business data only through CoreAPI** (no direct access to the business database), so BAL rules and the caller's
  permissions apply. The upload checks that the vendor exists.
* **Vendor-scoped retrieval.** A question about vendor 1 can only see vendor 1's agreements.
* **Local models.** Agreement text never leaves your machine (Ollama).
* Agreement text is treated as untrusted (prompt-injection safe prompts; the model has no tools).

## Setup
1. Models: `ollama pull nomic-embed-text` and `ollama pull qwen2.5:7b-instruct` (names are in `appsettings.json`).
2. Same JWT settings as CoreAPI: `dotnet user-secrets set "Jwt:Key" "<same key as CoreAPI>"` (issuer/audience default to `Invoice.Api`).
3. Run CoreAPI (5063) and Ollama, then `dotnet run` (listens on http://localhost:5070).
4. First try uses `"StoreProvider": "InMemory"` (data lost on restart). For real use run `Sql/001_rag_tables.sql`,
   set `ConnectionStrings:RagDb` and `"StoreProvider": "Sql"`.
5. Test with `Http/Invoice.RAG.http` (upload, extract terms, ask, payment summary).

## Endpoints (all need a JWT except /health)
| Method | Route | Purpose |
|---|---|---|
| POST | /api/v1/vendor-agreements | upload PDF (form: file, vendorId, title?, effectiveDate?) |
| GET | /api/v1/vendor-agreements?vendorId= | list |
| GET | /api/v1/vendor-agreements/{id}/file | original PDF |
| DELETE | /api/v1/vendor-agreements/{id} | delete |
| POST | /api/v1/vendor-agreements/{id}/extract-terms | model reads payment terms (draft) |
| GET/PUT | /api/v1/vendor-agreements/{id}/terms | read / correct + approve |
| POST | /api/v1/rag/search | retrieval only: best clauses + page + score (no model call) - use it to tune search |
| POST | /api/v1/rag/ask | question (+ poNumber or vendorId); sources include page, clause and the supporting text |
| GET | /api/v1/rag/po/{poNumber}/payment-summary | due date, discount, penalty (no model call) |

## Known limits
* Scanned PDFs have no text layer: they are rejected with a clear message (OCR needed first).
* PO field names are guessed from `Rag:PoFields` (`totalAmount`, `poDate`...). Adjust the map to your PurchaseOrderDto.
* "Newest agreement with terms" is used when no agreementId is given (no expiry/supersede logic yet).
* A 7B local model is slow on CPU: `extract-terms` is one call per agreement, `ask` is one call per question.
* If you change the embedding model, re-upload the agreements (old vectors are skipped with a warning).
* The PDF text extractor uses `ContentOrderTextExtractor`; if it does not compile with your PdfPig version use `page.Text`.
