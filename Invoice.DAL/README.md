# Purchase Order API (EF Core + Stored Procedures)

Header + item lines, saved in ONE transaction. Built to match your Vendor / Itemmaster pattern
(Entity -> Repository EFSp -> Service EFSp -> Controller -> tests).

## 1. Where every file goes (folders mirror your solution)

| File in this package | What it is |
|---|---|
| `Database/PurchaseOrder/01_PurchaseOrder_Schema_And_Procedures.sql` | tables, table type, sequence, 10 stored procedures |
| `Invoice.Data/Entities/PurchaseOrderEntity.cs`, `PurchaseOrderItemEntity.cs`, `PurchaseOrderStatus.cs` | entities |
| `Invoice.DTOs/PurchaseOrderDto.cs` (+ item DTO), `PurchaseOrderSaveDto.cs` (+ item), `PurchaseOrderFilterDto.cs` | DTOs |
| `Invoice.Model/BusinessRuleException.cs` | rule violations -> HTTP 400 |
| `Invoice.DAL/Contracts/IPurchaseOrderRepository.cs`, `Invoice.DAL/Repositories/PurchaseOrderRepositoryEFSp.cs` | repository |
| `Invoice.BAL/Contracts/IPurchaseOrderService.cs`, `Invoice.BAL/Services/PurchaseOrderServiceEFSp.cs`, `Invoice.BAL/Mapper/PurchaseOrderProfile.cs` | service + AutoMapper |
| `Invoice.CoreAPI/Controllers/PurchaseOrderController.cs` | controller |
| `Invoice.DAL.Test/PurchaseOrderRepositoryEFSpTests.cs`, `AssemblyInfo.cs` | 34 integration tests |
| `Invoice.BAL.Test/PurchaseOrderServiceTests.cs` | 16 unit tests |
| `Invoice.CoreAPI.Test/PurchaseOrderControllerTests.cs` | 27 unit tests |
| `ModifiedFiles/...` | your 3 existing files with the small changes already applied |

`ModifiedFiles` (compare with a diff tool, or just overwrite):
* `Invoice.Data/Db/AppDbContext.cs` : +2 lines (`PurchaseOrders`, `PurchaseOrderItems` DbSets)
* `Invoice.CoreAPI/Program.cs` : +1 AutoMapper profile line, +2 DI lines
* `Database/CI/01_Initialize_Accounts_Test.sql` : your CI script + the purchase order script appended

## 2. Steps

1. Run `01_PurchaseOrder_Schema_And_Procedures.sql` on **Accounts**, **Accounts_Test** (and the CI database - already
   included in the modified CI script). It is safe to run twice.
2. Copy the files in, apply the 3 modified files.
3. `dotnet build`, then `dotnet test`.
   The DAL tests need the SQL script on your test database. `AssemblyInfo.cs` turns test parallelism off for
   Invoice.DAL.Test, because the existing Vendor count tests compare "count before / after" on a shared database
   and the new tests also insert vendors.

## 3. Endpoints (same style as Vendor, all `[Authorize]`)

| Method | URL | Purpose |
|---|---|---|
| GET | `/api/v1/PurchaseOrder/GetAll` | all (headers only) |
| GET | `/api/v1/PurchaseOrder/GetById/{id}` | header + items |
| GET | `/api/v1/PurchaseOrder/GetAllPaged?poNumber=&vendorId=&status=&fromDate=&toDate=&pageNumber=1&pageSize=10` | grid |
| POST | `/api/v1/PurchaseOrder/Create` | returns the saved PO (PO number, totals, items) |
| PUT | `/api/v1/PurchaseOrder/Update/{id}` | Draft only |
| PUT | `/api/v1/PurchaseOrder/Approve/{id}` | Draft -> Approved |
| PUT | `/api/v1/PurchaseOrder/Cancel/{id}` | Draft/Approved -> Cancelled |
| DELETE | `/api/v1/PurchaseOrder/Delete/{id}` | Draft only, soft delete |

Create / Update body (no totals, no status, no PO number - the server decides them):

```json
{
  "vendorId": 3,
  "poDate": "2026-10-03",
  "expectedDeliveryDate": "2026-10-10",
  "remarks": "Urgent",
  "items": [
    { "itemId": 11, "quantity": 10, "rate": 50, "taxPercent": 5 },
    { "itemId": 12, "quantity": 2,  "rate": 100, "taxPercent": 18 }
  ]
}
```

Create response (`data`): `poNumber` like `PO-2026-00007`, `status` "Draft", `subTotal` 700, `taxAmount` 61,
`totalAmount` 761, and each item with `itemCode`, `itemName`, `uom`, `lineAmount`, `taxAmount`, `lineTotal`.

HTTP codes: 200 ok, 400 business rule broken (message is safe to show to the user), 404 not found, 500 error.
Same `ApiResponse<T>` wrapper as your other controllers (`success`, `message`, `data`, `error`, `totalRecords`).

## 4. Business rules (all in the stored procedures, one place)

* vendor must exist and be active; at least one item; no repeated item; items must be active
* quantity > 0, rate >= 0, tax percent 0-100
* totals are calculated in SQL per line: `lineAmount = qty x rate`, `tax = lineAmount x tax% / 100`
  (rounded to 2 decimals), header totals = sum of lines
* status flow: Draft -> Approved, Draft -> Cancelled, Approved -> Cancelled. Anything else = 400
* only Draft can be edited or deleted; delete is a soft delete (`IsDeleted = 1`, row kept for audit)
* the row is locked while status / edit changes run, so two users cannot approve and edit at the same time
* `CreatedBy` / `UpdatedBy` = the logged-in user name from the JWT (`ClaimTypes.Name`)
* PO number comes from a SQL sequence (`PO-<year>-<5 digits>`); a rolled-back save may leave a gap in the numbers

## 5. Angular (TypeScript)

```ts
export interface PurchaseOrderItem {
  id: number; itemId: number; itemCode?: string; itemName?: string; uom?: string;
  quantity: number; rate: number; taxPercent: number;
  lineAmount: number; taxAmount: number; lineTotal: number;
}

export interface PurchaseOrder {
  id: number; poNumber: string; poDate: string; vendorId: number;
  vendorCode?: string; vendorName?: string; expectedDeliveryDate?: string | null;
  status: 'Draft' | 'Approved' | 'Cancelled'; remarks?: string | null;
  subTotal: number; taxAmount: number; totalAmount: number;
  items: PurchaseOrderItem[];
}

export interface PurchaseOrderSave {
  vendorId: number; poDate: string; expectedDeliveryDate?: string | null; remarks?: string | null;
  items: { itemId: number; quantity: number; rate: number; taxPercent: number }[];
}
```

* Send dates as `'yyyy-MM-dd'` strings.
* Show the line/total numbers that come BACK from the API; if the screen shows a live preview, treat it as a preview only.
* On HTTP 400 show `error.error.message` to the user (it is already a readable sentence).
* Approve / Cancel / Delete buttons: show them only when `status` allows it (the API enforces it anyway).

## 6. Not compiled here

I wrote this against your real code (same packages and patterns) but my environment has no .NET SDK / SQL Server,
so nothing was built or run. If `dotnet build` or a test fails, send me the message and I will fix it.
