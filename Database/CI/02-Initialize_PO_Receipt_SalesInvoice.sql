/* ============================================================
   PURCHASEORDER
   ============================================================ */

IF OBJECT_ID('dbo.PurchaseOrder', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrder
(
    Id INT IDENTITY(1,1) NOT NULL,
    PONumber VARCHAR(30) NOT NULL,
    PODate DATETIME NOT NULL,
    VendorId INT NOT NULL,
    Status VARCHAR(20) NOT NULL
        CONSTRAINT DF_PurchaseOrder_Status DEFAULT ('Draft'),
    Notes NVARCHAR(500) NULL,
    SubTotal DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PurchaseOrder_SubTotal DEFAULT (0),
    TaxAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PurchaseOrder_TaxAmount DEFAULT (0),
    TotalAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PurchaseOrder_TotalAmount DEFAULT (0),
    IsDeleted BIT NOT NULL
        CONSTRAINT DF_PurchaseOrder_IsDeleted DEFAULT (0),
    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME NOT NULL
        CONSTRAINT DF_PurchaseOrder_CreatedDate DEFAULT (GETDATE()),
    UpdatedBy VARCHAR(100) NULL,
    UpdatedDate DATETIME NULL,
    CONSTRAINT PK_PurchaseOrder
        PRIMARY KEY (Id),
    CONSTRAINT UQ_PurchaseOrder_PONumber
        UNIQUE (PONumber),
    CONSTRAINT FK_PurchaseOrder_Vendor
        FOREIGN KEY (VendorId)
        REFERENCES dbo.Vendor(Id)
);

END
GO


/* ============================================================
   PURCHASEORDERDETAIL
   ============================================================ */
IF OBJECT_ID('dbo.PurchaseOrderDetail', 'U') IS NULL
BEGIN
   CREATE TABLE dbo.PurchaseOrderDetail
(
    Id INT IDENTITY(1,1) NOT NULL,
    PurchaseOrderId INT NOT NULL,
    ItemmasterId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    Rate DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PurchaseOrderDetail_DiscountAmount DEFAULT (0),
    TaxPercent DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_PurchaseOrderDetail_TaxPercent DEFAULT (0),
    TaxAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PurchaseOrderDetail_TaxAmount DEFAULT (0),
    LineTotal DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PurchaseOrderDetail_LineTotal DEFAULT (0),
    CONSTRAINT PK_PurchaseOrderDetail
        PRIMARY KEY (Id),
    CONSTRAINT FK_PurchaseOrderDetail_PurchaseOrder
        FOREIGN KEY (PurchaseOrderId)
        REFERENCES dbo.PurchaseOrder(Id),
    CONSTRAINT FK_PurchaseOrderDetail_Itemmaster
        FOREIGN KEY (ItemmasterId)
        REFERENCES dbo.Itemmaster(Id)
);
END
GO
-- ====  Purchase Order Insert ======= 
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Insert
(
    @PONumber       VARCHAR(30),
    @PODate         DATETIME,
    @VendorId       INT,
    @Status         VARCHAR(20),
    @Notes          NVARCHAR(500) = NULL,
    @SubTotal       DECIMAL(18,2),
    @TaxAmount      DECIMAL(18,2),
    @TotalAmount    DECIMAL(18,2),
    @CreatedBy      VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PurchaseOrder
    (
        PONumber,
        PODate,
        VendorId,
        Status,
        Notes,
        SubTotal,
        TaxAmount,
        TotalAmount,
        IsDeleted,
        CreatedBy,
        CreatedDate
    )
    VALUES
    (
        @PONumber,
        @PODate,
        @VendorId,
        @Status,
        @Notes,
        @SubTotal,
        @TaxAmount,
        @TotalAmount,
        0,
        @CreatedBy,
        GETDATE()
    );

    -- Return newly created Purchase Order Id
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO
--===  GetAll Purchase Order ===
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        PONumber,
        PODate,
        VendorId,
        Status,
        Notes,
        SubTotal,
        TaxAmount,
        TotalAmount,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.PurchaseOrder
    WHERE IsDeleted = 0
    ORDER BY Id DESC;
END;
GO
--===  Get PO by ID =============
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        PONumber,
        PODate,
        VendorId,
        Status,
        Notes,
        SubTotal,
        TaxAmount,
        TotalAmount,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.PurchaseOrder
    WHERE Id = @Id
      AND IsDeleted = 0;
END;
GO
--==== Update PO ======
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Update
(
    @Id             INT,
    @PONumber       VARCHAR(30),
    @PODate         DATETIME,
    @VendorId       INT,
    @Status         VARCHAR(20),
    @Notes          NVARCHAR(500) = NULL,
    @SubTotal       DECIMAL(18,2),
    @TaxAmount      DECIMAL(18,2),
    @TotalAmount    DECIMAL(18,2),
    @UpdatedBy      VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.PurchaseOrder
    SET
        PONumber = @PONumber,
        PODate = @PODate,
        VendorId = @VendorId,
        Status = @Status,
        Notes = @Notes,
        SubTotal = @SubTotal,
        TaxAmount = @TaxAmount,
        TotalAmount = @TotalAmount,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @Id
      AND IsDeleted = 0;
END;
GO
-- ====  Delete PO =======
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Delete
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.PurchaseOrder
    SET
        IsDeleted = 1,
        UpdatedDate = GETDATE()
    WHERE Id = @Id
      AND IsDeleted = 0;
END;
GO
-- ==== GetPaged PO ======
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetPaged
(
    @PONumber       VARCHAR(30) = NULL,
    @VendorId       INT = NULL,
    @Status         VARCHAR(20) = NULL,
    @PageNumber     INT = 1,
    @PageSize       INT = 10
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Safety
    IF @PageNumber < 1
        SET @PageNumber = 1;

    IF @PageSize < 1
        SET @PageSize = 10;

    DECLARE @Offset INT;

    SET @Offset =
        (@PageNumber - 1) * @PageSize;

    -- ============================================================
    -- RESULT SET 1 - PAGED DATA
    -- ============================================================

    SELECT
        Id,
        PONumber,
        PODate,
        VendorId,
        Status,
        Notes,
        SubTotal,
        TaxAmount,
        TotalAmount,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.PurchaseOrder
    WHERE IsDeleted = 0

      AND
      (
          @PONumber IS NULL
          OR PONumber LIKE '%' + @PONumber + '%'
      )

      AND
      (
          @VendorId IS NULL
          OR VendorId = @VendorId
      )

      AND
      (
          @Status IS NULL
          OR Status = @Status
      )

    ORDER BY Id DESC

    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;


    -- ============================================================
    -- RESULT SET 2 - TOTAL RECORDS
    -- ============================================================

    SELECT COUNT(*) AS TotalRecords
    FROM dbo.PurchaseOrder
    WHERE IsDeleted = 0

      AND
      (
          @PONumber IS NULL
          OR PONumber LIKE '%' + @PONumber + '%'
      )

      AND
      (
          @VendorId IS NULL
          OR VendorId = @VendorId
      )

      AND
      (
          @Status IS NULL
          OR Status = @Status
      );
END;
GO
--====  PO Details insert ===
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_Insert
(
    @PurchaseOrderId   INT,
    @ItemmasterId      INT,
    @Quantity          DECIMAL(18,2),
    @Rate              DECIMAL(18,2),
    @DiscountAmount    DECIMAL(18,2),
    @TaxPercent        DECIMAL(5,2),
    @TaxAmount         DECIMAL(18,2),
    @LineTotal         DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PurchaseOrderDetail
    (
        PurchaseOrderId,
        ItemmasterId,
        Quantity,
        Rate,
        DiscountAmount,
        TaxPercent,
        TaxAmount,
        LineTotal
    )
    VALUES
    (
        @PurchaseOrderId,
        @ItemmasterId,
        @Quantity,
        @Rate,
        @DiscountAmount,
        @TaxPercent,
        @TaxAmount,
        @LineTotal
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO
--====  Get PO Details by PO ID =====
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_GetByPurchaseOrderId
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        PurchaseOrderId,
        ItemmasterId,
        Quantity,
        Rate,
        DiscountAmount,
        TaxPercent,
        TaxAmount,
        LineTotal
    FROM dbo.PurchaseOrderDetail
    WHERE PurchaseOrderId = @PurchaseOrderId
    ORDER BY Id;
END;
GO
--====  Get PO Details by Details ID ====
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        PurchaseOrderId,
        ItemmasterId,
        Quantity,
        Rate,
        DiscountAmount,
        TaxPercent,
        TaxAmount,
        LineTotal
    FROM dbo.PurchaseOrderDetail
    WHERE Id = @Id;
END;
GO
--===  PO Details update ====  
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_Update
(
    @Id                INT,
    @ItemmasterId      INT,
    @Quantity          DECIMAL(18,2),
    @Rate              DECIMAL(18,2),
    @DiscountAmount    DECIMAL(18,2),
    @TaxPercent        DECIMAL(5,2),
    @TaxAmount         DECIMAL(18,2),
    @LineTotal         DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.PurchaseOrderDetail
    SET
        ItemmasterId = @ItemmasterId,
        Quantity = @Quantity,
        Rate = @Rate,
        DiscountAmount = @DiscountAmount,
        TaxPercent = @TaxPercent,
        TaxAmount = @TaxAmount,
        LineTotal = @LineTotal
    WHERE Id = @Id;
END;
GO
--====  PO Details Delete =======
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_Delete
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DELETE FROM dbo.PurchaseOrderDetail
    WHERE Id = @Id;
END;
GO
--====  Delete PO Details by PO ID =====
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_DeleteByPurchaseOrderId
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DELETE FROM dbo.PurchaseOrderDetail
    WHERE PurchaseOrderId = @PurchaseOrderId;
END;
GO

/* ############################################################
   PART 2 - PO CHANGES + RECEIPT + SALES INVOICE
   ############################################################ */
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================
   A. PURCHASE ORDER CHANGES
   ============================================================ */

-- A1. Track how much of each PO line has been received
IF COL_LENGTH('dbo.PurchaseOrderDetail', 'ReceivedQuantity') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderDetail
        ADD ReceivedQuantity DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_PurchaseOrderDetail_ReceivedQuantity DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PurchaseOrderDetail_ReceivedQuantity')
BEGIN
    ALTER TABLE dbo.PurchaseOrderDetail
        ADD CONSTRAINT CK_PurchaseOrderDetail_ReceivedQuantity
        CHECK (ReceivedQuantity >= 0);
END
GO

-- A2. Status workflow. NOCHECK so old rows with other values do not block the script;
--     clean them, then run: ALTER TABLE dbo.PurchaseOrder WITH CHECK CHECK CONSTRAINT CK_PurchaseOrder_Status;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PurchaseOrder_Status')
BEGIN
    ALTER TABLE dbo.PurchaseOrder WITH NOCHECK
        ADD CONSTRAINT CK_PurchaseOrder_Status
        CHECK (Status IN ('Draft','Approved','PartiallyReceived','Received','Cancelled'));
END
GO

-- A3. PO detail reads now return ReceivedQuantity
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_GetByPurchaseOrderId
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, PurchaseOrderId, ItemmasterId, Quantity, Rate, DiscountAmount,
           TaxPercent, TaxAmount, LineTotal, ReceivedQuantity
    FROM dbo.PurchaseOrderDetail
    WHERE PurchaseOrderId = @PurchaseOrderId
    ORDER BY Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderDetail_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, PurchaseOrderId, ItemmasterId, Quantity, Rate, DiscountAmount,
           TaxPercent, TaxAmount, LineTotal, ReceivedQuantity
    FROM dbo.PurchaseOrderDetail
    WHERE Id = @Id;
END;
GO

-- A4. Header update: only Draft POs are editable; status can only be Draft/Approved here
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Update
(
    @Id             INT,
    @PONumber       VARCHAR(30),
    @PODate         DATETIME,
    @VendorId       INT,
    @Status         VARCHAR(20),
    @Notes          NVARCHAR(500) = NULL,
    @SubTotal       DECIMAL(18,2),
    @TaxAmount      DECIMAL(18,2),
    @TotalAmount    DECIMAL(18,2),
    @UpdatedBy      VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status
    FROM dbo.PurchaseOrder
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        RETURN;                                   -- not found => 0 rows affected

    IF @Current <> 'Draft'
        THROW 51409, 'Only Draft purchase orders can be edited.', 1;

    IF @Status NOT IN ('Draft', 'Approved')
        THROW 51400, 'Status can only be set to Draft or Approved here. Use the Approve / Cancel actions.', 1;

    UPDATE dbo.PurchaseOrder
    SET PONumber = @PONumber,
        PODate = @PODate,
        VendorId = @VendorId,
        Status = @Status,
        Notes = @Notes,
        SubTotal = @SubTotal,
        TaxAmount = @TaxAmount,
        TotalAmount = @TotalAmount,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

-- A5. Delete: only Draft or Cancelled POs
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Delete
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status
    FROM dbo.PurchaseOrder
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        RETURN;

    IF @Current NOT IN ('Draft', 'Cancelled')
        THROW 51410, 'Only Draft or Cancelled purchase orders can be deleted.', 1;

    UPDATE dbo.PurchaseOrder
    SET IsDeleted = 1,
        UpdatedDate = GETDATE()
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

-- A6. Approve (Draft -> Approved)
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Approve
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status
    FROM dbo.PurchaseOrder WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        THROW 51404, 'Purchase order not found.', 1;

    IF @Current <> 'Draft'
        THROW 51411, 'Only Draft purchase orders can be approved.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrderDetail WHERE PurchaseOrderId = @Id)
        THROW 51413, 'A purchase order without lines cannot be approved.', 1;

    UPDATE dbo.PurchaseOrder
    SET Status = 'Approved', UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id;

    SELECT CAST(1 AS BIT) AS Success;
END;
GO

-- A7. Cancel (Draft/Approved -> Cancelled, only when nothing is received and no open receipt exists)
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Cancel
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status
    FROM dbo.PurchaseOrder WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        THROW 51404, 'Purchase order not found.', 1;

    IF @Current NOT IN ('Draft', 'Approved')
        THROW 51414, 'Only Draft or Approved purchase orders can be cancelled. Cancel the posted receipts first.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Receipt WHERE PurchaseOrderId = @Id AND IsDeleted = 0 AND Status IN ('Draft','Posted'))
        THROW 51415, 'This purchase order has open receipts. Cancel or delete them first.', 1;

    UPDATE dbo.PurchaseOrder
    SET Status = 'Cancelled', UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id;

    SELECT CAST(1 AS BIT) AS Success;
END;
GO

/* ============================================================
   B. SEQUENCES (auto document numbers)
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_ReceiptNumber')
    CREATE SEQUENCE dbo.seq_ReceiptNumber AS INT START WITH 1 INCREMENT BY 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_SalesInvoiceNumber')
    CREATE SEQUENCE dbo.seq_SalesInvoiceNumber AS INT START WITH 1 INCREMENT BY 1;
GO

/* ============================================================
   C. RECEIPT (Goods Receipt against a Purchase Order)
   ============================================================ */
IF OBJECT_ID('dbo.Receipt', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Receipt
    (
        Id INT IDENTITY(1,1) NOT NULL,
        ReceiptNumber VARCHAR(30) NOT NULL,
        ReceiptDate DATETIME NOT NULL,
        PurchaseOrderId INT NOT NULL,
        VendorId INT NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Receipt_Status DEFAULT ('Draft'),
        Notes NVARCHAR(500) NULL,
        SubTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receipt_SubTotal DEFAULT (0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receipt_TaxAmount DEFAULT (0),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receipt_TotalAmount DEFAULT (0),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Receipt_IsDeleted DEFAULT (0),
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_Receipt_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy VARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,
        CONSTRAINT PK_Receipt PRIMARY KEY (Id),
        CONSTRAINT UQ_Receipt_ReceiptNumber UNIQUE (ReceiptNumber),
        CONSTRAINT FK_Receipt_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder(Id),
        CONSTRAINT FK_Receipt_Vendor FOREIGN KEY (VendorId) REFERENCES dbo.Vendor(Id),
        CONSTRAINT CK_Receipt_Status CHECK (Status IN ('Draft','Posted','Cancelled'))
    );
    CREATE INDEX IX_Receipt_PurchaseOrderId ON dbo.Receipt (PurchaseOrderId);
END
GO

IF OBJECT_ID('dbo.ReceiptDetail', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceiptDetail
    (
        Id INT IDENTITY(1,1) NOT NULL,
        ReceiptId INT NOT NULL,
        PurchaseOrderDetailId INT NOT NULL,
        ItemmasterId INT NOT NULL,
        ReceivedQuantity DECIMAL(18,2) NOT NULL,
        Rate DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReceiptDetail_DiscountAmount DEFAULT (0),
        TaxPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_ReceiptDetail_TaxPercent DEFAULT (0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReceiptDetail_TaxAmount DEFAULT (0),
        LineTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReceiptDetail_LineTotal DEFAULT (0),
        CONSTRAINT PK_ReceiptDetail PRIMARY KEY (Id),
        CONSTRAINT FK_ReceiptDetail_Receipt FOREIGN KEY (ReceiptId) REFERENCES dbo.Receipt(Id),
        CONSTRAINT FK_ReceiptDetail_PurchaseOrderDetail FOREIGN KEY (PurchaseOrderDetailId) REFERENCES dbo.PurchaseOrderDetail(Id),
        CONSTRAINT FK_ReceiptDetail_Itemmaster FOREIGN KEY (ItemmasterId) REFERENCES dbo.Itemmaster(Id),
        CONSTRAINT CK_ReceiptDetail_Quantity CHECK (ReceivedQuantity > 0),
        CONSTRAINT UQ_ReceiptDetail_Receipt_POLine UNIQUE (ReceiptId, PurchaseOrderDetailId)
    );
    CREATE INDEX IX_ReceiptDetail_ItemmasterId ON dbo.ReceiptDetail (ItemmasterId);
    CREATE INDEX IX_ReceiptDetail_PurchaseOrderDetailId ON dbo.ReceiptDetail (PurchaseOrderDetailId);
END
GO

/* ============================================================
   D. SALES INVOICE
   ============================================================ */
IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesInvoice
    (
        Id INT IDENTITY(1,1) NOT NULL,
        InvoiceNumber VARCHAR(30) NOT NULL,
        InvoiceDate DATETIME NOT NULL,
        DueDate DATETIME NULL,
        CustomerId INT NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_SalesInvoice_Status DEFAULT ('Draft'),
        Notes NVARCHAR(500) NULL,
        SubTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_SubTotal DEFAULT (0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_TaxAmount DEFAULT (0),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_TotalAmount DEFAULT (0),
        IsDeleted BIT NOT NULL CONSTRAINT DF_SalesInvoice_IsDeleted DEFAULT (0),
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_SalesInvoice_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy VARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,
        CONSTRAINT PK_SalesInvoice PRIMARY KEY (Id),
        CONSTRAINT UQ_SalesInvoice_InvoiceNumber UNIQUE (InvoiceNumber),
        CONSTRAINT FK_SalesInvoice_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id),
        CONSTRAINT CK_SalesInvoice_Status CHECK (Status IN ('Draft','Posted','Cancelled')),
        CONSTRAINT CK_SalesInvoice_DueDate CHECK (DueDate IS NULL OR DueDate >= InvoiceDate)
    );
    CREATE INDEX IX_SalesInvoice_CustomerId ON dbo.SalesInvoice (CustomerId);
END
GO

IF OBJECT_ID('dbo.SalesInvoiceDetail', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesInvoiceDetail
    (
        Id INT IDENTITY(1,1) NOT NULL,
        SalesInvoiceId INT NOT NULL,
        ItemmasterId INT NOT NULL,
        Quantity DECIMAL(18,2) NOT NULL,
        Rate DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoiceDetail_DiscountAmount DEFAULT (0),
        TaxPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_SalesInvoiceDetail_TaxPercent DEFAULT (0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoiceDetail_TaxAmount DEFAULT (0),
        LineTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoiceDetail_LineTotal DEFAULT (0),
        CONSTRAINT PK_SalesInvoiceDetail PRIMARY KEY (Id),
        CONSTRAINT FK_SalesInvoiceDetail_SalesInvoice FOREIGN KEY (SalesInvoiceId) REFERENCES dbo.SalesInvoice(Id),
        CONSTRAINT FK_SalesInvoiceDetail_Itemmaster FOREIGN KEY (ItemmasterId) REFERENCES dbo.Itemmaster(Id),
        CONSTRAINT CK_SalesInvoiceDetail_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_SalesInvoiceDetail_Rate CHECK (Rate >= 0),
        CONSTRAINT CK_SalesInvoiceDetail_Discount CHECK (DiscountAmount >= 0)
    );
    CREATE INDEX IX_SalesInvoiceDetail_SalesInvoiceId ON dbo.SalesInvoiceDetail (SalesInvoiceId);
    CREATE INDEX IX_SalesInvoiceDetail_ItemmasterId ON dbo.SalesInvoiceDetail (ItemmasterId);
END
GO

/* ============================================================
   E. STOCK  (derived: posted receipts - posted sales invoices)
   ============================================================ */
CREATE OR ALTER VIEW dbo.vw_ItemStock
AS
SELECT
    I.Id AS ItemmasterId,
    I.ItemCode AS ItemCode,
    I.ItemName AS ItemName,
    CAST(ISNULL(R.Qty, 0) AS DECIMAL(18,2)) AS ReceivedQuantity,
    CAST(ISNULL(S.Qty, 0) AS DECIMAL(18,2)) AS SoldQuantity,
    CAST(ISNULL(R.Qty, 0) - ISNULL(S.Qty, 0) AS DECIMAL(18,2)) AS OnHandQuantity
FROM dbo.Itemmaster I
LEFT JOIN
(
    SELECT D.ItemmasterId, SUM(D.ReceivedQuantity) AS Qty
    FROM dbo.ReceiptDetail D
    INNER JOIN dbo.Receipt H ON H.Id = D.ReceiptId
    WHERE H.Status = 'Posted' AND H.IsDeleted = 0
    GROUP BY D.ItemmasterId
) R ON R.ItemmasterId = I.Id
LEFT JOIN
(
    SELECT D.ItemmasterId, SUM(D.Quantity) AS Qty
    FROM dbo.SalesInvoiceDetail D
    INNER JOIN dbo.SalesInvoice H ON H.Id = D.SalesInvoiceId
    WHERE H.Status = 'Posted' AND H.IsDeleted = 0
    GROUP BY D.ItemmasterId
) S ON S.ItemmasterId = I.Id;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Stock_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ItemmasterId, ItemCode, ItemName, ReceivedQuantity, SoldQuantity, OnHandQuantity
    FROM dbo.vw_ItemStock
    ORDER BY ItemmasterId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Stock_GetByItemmasterId
(
    @ItemmasterId INT
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ItemmasterId, ItemCode, ItemName, ReceivedQuantity, SoldQuantity, OnHandQuantity
    FROM dbo.vw_ItemStock
    WHERE ItemmasterId = @ItemmasterId;
END;
GO

/* ============================================================
   F. RECEIPT PROCEDURES
   ============================================================ */

-- Internal helper: recompute PO status from its line quantities
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_RefreshReceiptStatus
(
    @PurchaseOrderId INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AnyReceived BIT = 0, @AnyOpen BIT = 0;

    IF EXISTS (SELECT 1 FROM dbo.PurchaseOrderDetail WHERE PurchaseOrderId = @PurchaseOrderId AND ReceivedQuantity > 0)
        SET @AnyReceived = 1;

    IF EXISTS (SELECT 1 FROM dbo.PurchaseOrderDetail WHERE PurchaseOrderId = @PurchaseOrderId AND ReceivedQuantity < Quantity)
        SET @AnyOpen = 1;

    UPDATE dbo.PurchaseOrder
    SET Status = CASE
                    WHEN @AnyReceived = 0 THEN 'Approved'
                    WHEN @AnyOpen = 1     THEN 'PartiallyReceived'
                    ELSE 'Received'
                 END,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @PurchaseOrderId
      AND Status IN ('Approved', 'PartiallyReceived', 'Received');
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_Insert
(
    @ReceiptNumber   VARCHAR(30) = NULL,
    @ReceiptDate     DATETIME,
    @PurchaseOrderId INT,
    @Notes           NVARCHAR(500) = NULL,
    @SubTotal        DECIMAL(18,2),
    @TaxAmount       DECIMAL(18,2),
    @TotalAmount     DECIMAL(18,2),
    @CreatedBy       VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @VendorId INT, @PoStatus VARCHAR(20);

    SELECT @VendorId = VendorId, @PoStatus = Status
    FROM dbo.PurchaseOrder
    WHERE Id = @PurchaseOrderId AND IsDeleted = 0;

    IF @VendorId IS NULL
        THROW 51404, 'Purchase order not found.', 1;

    IF @PoStatus NOT IN ('Approved', 'PartiallyReceived')
        THROW 51412, 'Goods can be received only against an Approved or PartiallyReceived purchase order.', 1;

    IF @ReceiptNumber IS NULL OR LTRIM(RTRIM(@ReceiptNumber)) = ''
        SET @ReceiptNumber = 'GRN-' + RIGHT('000000' + CAST(NEXT VALUE FOR dbo.seq_ReceiptNumber AS VARCHAR(10)), 6);

    INSERT INTO dbo.Receipt
    (
        ReceiptNumber, ReceiptDate, PurchaseOrderId, VendorId, Status, Notes,
        SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES
    (
        @ReceiptNumber, @ReceiptDate, @PurchaseOrderId, @VendorId, 'Draft', @Notes,
        @SubTotal, @TaxAmount, @TotalAmount, 0, @CreatedBy, GETDATE()
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ReceiptNumber, ReceiptDate, PurchaseOrderId, VendorId, Status, Notes,
           SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.Receipt
    WHERE IsDeleted = 0
    ORDER BY Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ReceiptNumber, ReceiptDate, PurchaseOrderId, VendorId, Status, Notes,
           SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.Receipt
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_GetPaged
(
    @ReceiptNumber   VARCHAR(30) = NULL,
    @PurchaseOrderId INT = NULL,
    @VendorId        INT = NULL,
    @Status          VARCHAR(20) = NULL,
    @PageNumber      INT = 1,
    @PageSize        INT = 10
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT Id, ReceiptNumber, ReceiptDate, PurchaseOrderId, VendorId, Status, Notes,
           SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.Receipt
    WHERE IsDeleted = 0
      AND (@ReceiptNumber IS NULL OR ReceiptNumber LIKE '%' + @ReceiptNumber + '%')
      AND (@PurchaseOrderId IS NULL OR PurchaseOrderId = @PurchaseOrderId)
      AND (@VendorId IS NULL OR VendorId = @VendorId)
      AND (@Status IS NULL OR Status = @Status)
    ORDER BY Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) AS TotalRecords
    FROM dbo.Receipt
    WHERE IsDeleted = 0
      AND (@ReceiptNumber IS NULL OR ReceiptNumber LIKE '%' + @ReceiptNumber + '%')
      AND (@PurchaseOrderId IS NULL OR PurchaseOrderId = @PurchaseOrderId)
      AND (@VendorId IS NULL OR VendorId = @VendorId)
      AND (@Status IS NULL OR Status = @Status);
END;
GO

-- Only date / notes / totals are editable. PO, vendor and number are fixed after creation.
CREATE OR ALTER PROCEDURE dbo.sp_Receipt_Update
(
    @Id          INT,
    @ReceiptDate DATETIME,
    @Notes       NVARCHAR(500) = NULL,
    @SubTotal    DECIMAL(18,2),
    @TaxAmount   DECIMAL(18,2),
    @TotalAmount DECIMAL(18,2),
    @UpdatedBy   VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status FROM dbo.Receipt WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        RETURN;

    IF @Current <> 'Draft'
        THROW 51420, 'Only Draft receipts can be edited.', 1;

    UPDATE dbo.Receipt
    SET ReceiptDate = @ReceiptDate,
        Notes = @Notes,
        SubTotal = @SubTotal,
        TaxAmount = @TaxAmount,
        TotalAmount = @TotalAmount,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_Delete
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status FROM dbo.Receipt WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        RETURN;

    IF @Current <> 'Draft'
        THROW 51421, 'Only Draft receipts can be deleted. Cancel a posted receipt instead.', 1;

    UPDATE dbo.Receipt
    SET IsDeleted = 1, UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

-- ---------------- Receipt detail ----------------

CREATE OR ALTER PROCEDURE dbo.sp_ReceiptDetail_Insert
(
    @ReceiptId             INT,
    @PurchaseOrderDetailId INT,
    @ItemmasterId          INT,
    @ReceivedQuantity      DECIMAL(18,2),
    @Rate                  DECIMAL(18,2),
    @DiscountAmount        DECIMAL(18,2),
    @TaxPercent            DECIMAL(5,2),
    @TaxAmount             DECIMAL(18,2),
    @LineTotal             DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status VARCHAR(20), @PoId INT;

    SELECT @Status = Status, @PoId = PurchaseOrderId
    FROM dbo.Receipt
    WHERE Id = @ReceiptId AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 51404, 'Receipt not found.', 1;

    IF @Status <> 'Draft'
        THROW 51420, 'Only Draft receipts can be edited.', 1;

    DECLARE @PoLineQty DECIMAL(18,2), @PoLineReceived DECIMAL(18,2), @PoLineItem INT;

    SELECT @PoLineQty = Quantity, @PoLineReceived = ReceivedQuantity, @PoLineItem = ItemmasterId
    FROM dbo.PurchaseOrderDetail
    WHERE Id = @PurchaseOrderDetailId AND PurchaseOrderId = @PoId;

    IF @PoLineQty IS NULL
        THROW 51422, 'The purchase order line does not belong to the receipt''s purchase order.', 1;

    IF @PoLineItem <> @ItemmasterId
        THROW 51423, 'The item does not match the purchase order line.', 1;

    IF @ReceivedQuantity > (@PoLineQty - @PoLineReceived)
        THROW 51424, 'Received quantity exceeds the outstanding quantity of the purchase order line.', 1;

    INSERT INTO dbo.ReceiptDetail
    (
        ReceiptId, PurchaseOrderDetailId, ItemmasterId, ReceivedQuantity,
        Rate, DiscountAmount, TaxPercent, TaxAmount, LineTotal
    )
    VALUES
    (
        @ReceiptId, @PurchaseOrderDetailId, @ItemmasterId, @ReceivedQuantity,
        @Rate, @DiscountAmount, @TaxPercent, @TaxAmount, @LineTotal
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReceiptDetail_GetByReceiptId
(
    @ReceiptId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ReceiptId, PurchaseOrderDetailId, ItemmasterId, ReceivedQuantity,
           Rate, DiscountAmount, TaxPercent, TaxAmount, LineTotal
    FROM dbo.ReceiptDetail
    WHERE ReceiptId = @ReceiptId
    ORDER BY Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReceiptDetail_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ReceiptId, PurchaseOrderDetailId, ItemmasterId, ReceivedQuantity,
           Rate, DiscountAmount, TaxPercent, TaxAmount, LineTotal
    FROM dbo.ReceiptDetail
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReceiptDetail_Update
(
    @Id               INT,
    @ReceivedQuantity DECIMAL(18,2),
    @Rate             DECIMAL(18,2),
    @DiscountAmount   DECIMAL(18,2),
    @TaxPercent       DECIMAL(5,2),
    @TaxAmount        DECIMAL(18,2),
    @LineTotal        DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Status VARCHAR(20), @PoDetailId INT;

    SELECT @Status = R.Status, @PoDetailId = D.PurchaseOrderDetailId
    FROM dbo.ReceiptDetail D
    INNER JOIN dbo.Receipt R ON R.Id = D.ReceiptId
    WHERE D.Id = @Id AND R.IsDeleted = 0;

    IF @Status IS NULL
        RETURN;

    IF @Status <> 'Draft'
        THROW 51420, 'Only Draft receipts can be edited.', 1;

    IF EXISTS (SELECT 1 FROM dbo.PurchaseOrderDetail WHERE Id = @PoDetailId AND @ReceivedQuantity > (Quantity - ReceivedQuantity))
        THROW 51424, 'Received quantity exceeds the outstanding quantity of the purchase order line.', 1;

    UPDATE dbo.ReceiptDetail
    SET ReceivedQuantity = @ReceivedQuantity,
        Rate = @Rate,
        DiscountAmount = @DiscountAmount,
        TaxPercent = @TaxPercent,
        TaxAmount = @TaxAmount,
        LineTotal = @LineTotal
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReceiptDetail_Delete
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = R.Status
    FROM dbo.ReceiptDetail D
    INNER JOIN dbo.Receipt R ON R.Id = D.ReceiptId
    WHERE D.Id = @Id;

    IF @Status IS NULL
        RETURN;

    IF @Status <> 'Draft'
        THROW 51420, 'Only Draft receipts can be edited.', 1;

    DELETE FROM dbo.ReceiptDetail WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReceiptDetail_DeleteByReceiptId
(
    @ReceiptId INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = Status FROM dbo.Receipt WHERE Id = @ReceiptId;

    IF @Status IS NULL
        RETURN;

    IF @Status <> 'Draft'
        THROW 51420, 'Only Draft receipts can be edited.', 1;

    DELETE FROM dbo.ReceiptDetail WHERE ReceiptId = @ReceiptId;
END;
GO

-- ---------------- Receipt workflow ----------------

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_Post
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @PoId INT, @PoStatus VARCHAR(20);

    SELECT @Status = Status, @PoId = PurchaseOrderId
    FROM dbo.Receipt WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 51404, 'Receipt not found.', 1;

    IF @Status <> 'Draft'
        THROW 51425, 'Only Draft receipts can be posted.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.ReceiptDetail WHERE ReceiptId = @Id)
        THROW 51426, 'A receipt without lines cannot be posted.', 1;

    SELECT @PoStatus = Status
    FROM dbo.PurchaseOrder WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @PoId AND IsDeleted = 0;

    IF @PoStatus IS NULL OR @PoStatus NOT IN ('Approved', 'PartiallyReceived')
        THROW 51412, 'Goods can be received only against an Approved or PartiallyReceived purchase order.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.ReceiptDetail RD
        INNER JOIN dbo.PurchaseOrderDetail PD WITH (UPDLOCK) ON PD.Id = RD.PurchaseOrderDetailId
        WHERE RD.ReceiptId = @Id
          AND PD.ReceivedQuantity + RD.ReceivedQuantity > PD.Quantity
    )
        THROW 51424, 'Received quantity exceeds the outstanding quantity of the purchase order line.', 1;

    UPDATE PD
    SET PD.ReceivedQuantity = PD.ReceivedQuantity + RD.ReceivedQuantity
    FROM dbo.PurchaseOrderDetail PD
    INNER JOIN dbo.ReceiptDetail RD ON RD.PurchaseOrderDetailId = PD.Id
    WHERE RD.ReceiptId = @Id;

    EXEC dbo.sp_PurchaseOrder_RefreshReceiptStatus @PoId, @UpdatedBy;

    UPDATE dbo.Receipt
    SET Status = 'Posted', UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id;

    COMMIT TRANSACTION;

    SELECT CAST(1 AS BIT) AS Success;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Receipt_Cancel
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;   -- stock check must not race with sales invoice posting

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @PoId INT;

    SELECT @Status = Status, @PoId = PurchaseOrderId
    FROM dbo.Receipt WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 51404, 'Receipt not found.', 1;

    IF @Status = 'Cancelled'
        THROW 51428, 'The receipt is already cancelled.', 1;

    IF @Status = 'Posted'
    BEGIN
        DECLARE @Msg NVARCHAR(400) = NULL;

        SELECT TOP (1)
            @Msg = 'Cannot cancel: stock of item ' + S.ItemCode + ' has already been sold (on hand '
                   + CAST(S.OnHandQuantity AS VARCHAR(30)) + ', receipt quantity ' + CAST(R.Qty AS VARCHAR(30)) + ').'
        FROM
        (
            SELECT ItemmasterId, SUM(ReceivedQuantity) AS Qty
            FROM dbo.ReceiptDetail
            WHERE ReceiptId = @Id
            GROUP BY ItemmasterId
        ) R
        INNER JOIN dbo.vw_ItemStock S ON S.ItemmasterId = R.ItemmasterId
        WHERE S.OnHandQuantity - R.Qty < 0;

        IF @Msg IS NOT NULL
            THROW 51427, @Msg, 1;

        UPDATE PD
        SET PD.ReceivedQuantity = PD.ReceivedQuantity - RD.ReceivedQuantity
        FROM dbo.PurchaseOrderDetail PD
        INNER JOIN dbo.ReceiptDetail RD ON RD.PurchaseOrderDetailId = PD.Id
        WHERE RD.ReceiptId = @Id;
    END

    UPDATE dbo.Receipt
    SET Status = 'Cancelled', UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id;

    IF @Status = 'Posted'
        EXEC dbo.sp_PurchaseOrder_RefreshReceiptStatus @PoId, @UpdatedBy;

    COMMIT TRANSACTION;

    SELECT CAST(1 AS BIT) AS Success;
END;
GO

/* ============================================================
   G. SALES INVOICE PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_Insert
(
    @InvoiceNumber VARCHAR(30) = NULL,
    @InvoiceDate   DATETIME,
    @DueDate       DATETIME = NULL,
    @CustomerId    INT,
    @Notes         NVARCHAR(500) = NULL,
    @SubTotal      DECIMAL(18,2),
    @TaxAmount     DECIMAL(18,2),
    @TotalAmount   DECIMAL(18,2),
    @CreatedBy     VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE Id = @CustomerId AND IsDeleted = 0 AND IsActive = 1)
        THROW 51430, 'Customer not found or inactive.', 1;

    IF @DueDate IS NOT NULL AND @DueDate < @InvoiceDate
        THROW 51431, 'Due date cannot be earlier than the invoice date.', 1;

    IF @InvoiceNumber IS NULL OR LTRIM(RTRIM(@InvoiceNumber)) = ''
        SET @InvoiceNumber = 'INV-' + RIGHT('000000' + CAST(NEXT VALUE FOR dbo.seq_SalesInvoiceNumber AS VARCHAR(10)), 6);

    INSERT INTO dbo.SalesInvoice
    (
        InvoiceNumber, InvoiceDate, DueDate, CustomerId, Status, Notes,
        SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES
    (
        @InvoiceNumber, @InvoiceDate, @DueDate, @CustomerId, 'Draft', @Notes,
        @SubTotal, @TaxAmount, @TotalAmount, 0, @CreatedBy, GETDATE()
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, InvoiceNumber, InvoiceDate, DueDate, CustomerId, Status, Notes,
           SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.SalesInvoice
    WHERE IsDeleted = 0
    ORDER BY Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, InvoiceNumber, InvoiceDate, DueDate, CustomerId, Status, Notes,
           SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.SalesInvoice
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_GetPaged
(
    @InvoiceNumber VARCHAR(30) = NULL,
    @CustomerId    INT = NULL,
    @Status        VARCHAR(20) = NULL,
    @PageNumber    INT = 1,
    @PageSize      INT = 10
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT Id, InvoiceNumber, InvoiceDate, DueDate, CustomerId, Status, Notes,
           SubTotal, TaxAmount, TotalAmount, IsDeleted, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.SalesInvoice
    WHERE IsDeleted = 0
      AND (@InvoiceNumber IS NULL OR InvoiceNumber LIKE '%' + @InvoiceNumber + '%')
      AND (@CustomerId IS NULL OR CustomerId = @CustomerId)
      AND (@Status IS NULL OR Status = @Status)
    ORDER BY Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) AS TotalRecords
    FROM dbo.SalesInvoice
    WHERE IsDeleted = 0
      AND (@InvoiceNumber IS NULL OR InvoiceNumber LIKE '%' + @InvoiceNumber + '%')
      AND (@CustomerId IS NULL OR CustomerId = @CustomerId)
      AND (@Status IS NULL OR Status = @Status);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_Update
(
    @Id          INT,
    @InvoiceDate DATETIME,
    @DueDate     DATETIME = NULL,
    @CustomerId  INT,
    @Notes       NVARCHAR(500) = NULL,
    @SubTotal    DECIMAL(18,2),
    @TaxAmount   DECIMAL(18,2),
    @TotalAmount DECIMAL(18,2),
    @UpdatedBy   VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status FROM dbo.SalesInvoice WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        RETURN;

    IF @Current <> 'Draft'
        THROW 51440, 'Only Draft sales invoices can be edited.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE Id = @CustomerId AND IsDeleted = 0 AND IsActive = 1)
        THROW 51430, 'Customer not found or inactive.', 1;

    IF @DueDate IS NOT NULL AND @DueDate < @InvoiceDate
        THROW 51431, 'Due date cannot be earlier than the invoice date.', 1;

    UPDATE dbo.SalesInvoice
    SET InvoiceDate = @InvoiceDate,
        DueDate = @DueDate,
        CustomerId = @CustomerId,
        Notes = @Notes,
        SubTotal = @SubTotal,
        TaxAmount = @TaxAmount,
        TotalAmount = @TotalAmount,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_Delete
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Current VARCHAR(20);

    SELECT @Current = Status FROM dbo.SalesInvoice WHERE Id = @Id AND IsDeleted = 0;

    IF @Current IS NULL
        RETURN;

    IF @Current <> 'Draft'
        THROW 51441, 'Only Draft sales invoices can be deleted. Cancel a posted invoice instead.', 1;

    UPDATE dbo.SalesInvoice
    SET IsDeleted = 1, UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

-- ---------------- Sales invoice detail ----------------

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoiceDetail_Insert
(
    @SalesInvoiceId INT,
    @ItemmasterId   INT,
    @Quantity       DECIMAL(18,2),
    @Rate           DECIMAL(18,2),
    @DiscountAmount DECIMAL(18,2),
    @TaxPercent     DECIMAL(5,2),
    @TaxAmount      DECIMAL(18,2),
    @LineTotal      DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = Status FROM dbo.SalesInvoice WHERE Id = @SalesInvoiceId AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 51404, 'Sales invoice not found.', 1;

    IF @Status <> 'Draft'
        THROW 51440, 'Only Draft sales invoices can be edited.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Itemmaster WHERE Id = @ItemmasterId AND IsActive = 1)
        THROW 51442, 'Item not found or inactive.', 1;

    INSERT INTO dbo.SalesInvoiceDetail
    (
        SalesInvoiceId, ItemmasterId, Quantity, Rate, DiscountAmount, TaxPercent, TaxAmount, LineTotal
    )
    VALUES
    (
        @SalesInvoiceId, @ItemmasterId, @Quantity, @Rate, @DiscountAmount, @TaxPercent, @TaxAmount, @LineTotal
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoiceDetail_GetBySalesInvoiceId
(
    @SalesInvoiceId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SalesInvoiceId, ItemmasterId, Quantity, Rate, DiscountAmount, TaxPercent, TaxAmount, LineTotal
    FROM dbo.SalesInvoiceDetail
    WHERE SalesInvoiceId = @SalesInvoiceId
    ORDER BY Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoiceDetail_GetById
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SalesInvoiceId, ItemmasterId, Quantity, Rate, DiscountAmount, TaxPercent, TaxAmount, LineTotal
    FROM dbo.SalesInvoiceDetail
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoiceDetail_Update
(
    @Id             INT,
    @ItemmasterId   INT,
    @Quantity       DECIMAL(18,2),
    @Rate           DECIMAL(18,2),
    @DiscountAmount DECIMAL(18,2),
    @TaxPercent     DECIMAL(5,2),
    @TaxAmount      DECIMAL(18,2),
    @LineTotal      DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = H.Status
    FROM dbo.SalesInvoiceDetail D
    INNER JOIN dbo.SalesInvoice H ON H.Id = D.SalesInvoiceId
    WHERE D.Id = @Id AND H.IsDeleted = 0;

    IF @Status IS NULL
        RETURN;

    IF @Status <> 'Draft'
        THROW 51440, 'Only Draft sales invoices can be edited.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Itemmaster WHERE Id = @ItemmasterId AND IsActive = 1)
        THROW 51442, 'Item not found or inactive.', 1;

    UPDATE dbo.SalesInvoiceDetail
    SET ItemmasterId = @ItemmasterId,
        Quantity = @Quantity,
        Rate = @Rate,
        DiscountAmount = @DiscountAmount,
        TaxPercent = @TaxPercent,
        TaxAmount = @TaxAmount,
        LineTotal = @LineTotal
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoiceDetail_Delete
(
    @Id INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = H.Status
    FROM dbo.SalesInvoiceDetail D
    INNER JOIN dbo.SalesInvoice H ON H.Id = D.SalesInvoiceId
    WHERE D.Id = @Id;

    IF @Status IS NULL
        RETURN;

    IF @Status <> 'Draft'
        THROW 51440, 'Only Draft sales invoices can be edited.', 1;

    DELETE FROM dbo.SalesInvoiceDetail WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoiceDetail_DeleteBySalesInvoiceId
(
    @SalesInvoiceId INT
)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = Status FROM dbo.SalesInvoice WHERE Id = @SalesInvoiceId;

    IF @Status IS NULL
        RETURN;

    IF @Status <> 'Draft'
        THROW 51440, 'Only Draft sales invoices can be edited.', 1;

    DELETE FROM dbo.SalesInvoiceDetail WHERE SalesInvoiceId = @SalesInvoiceId;
END;
GO

-- ---------------- Sales invoice workflow ----------------

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_Post
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;   -- two invoices must not both sell the last unit

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @CustomerOk BIT = 0;

    SELECT @Status = H.Status,
           @CustomerOk = CASE WHEN C.Id IS NULL THEN 0 ELSE 1 END
    FROM dbo.SalesInvoice H WITH (UPDLOCK, ROWLOCK)
    LEFT JOIN dbo.Customer C ON C.Id = H.CustomerId AND C.IsDeleted = 0 AND C.IsActive = 1
    WHERE H.Id = @Id AND H.IsDeleted = 0;

    IF @Status IS NULL
        THROW 51404, 'Sales invoice not found.', 1;

    IF @Status <> 'Draft'
        THROW 51443, 'Only Draft sales invoices can be posted.', 1;

    IF @CustomerOk = 0
        THROW 51430, 'Customer not found or inactive.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.SalesInvoiceDetail WHERE SalesInvoiceId = @Id)
        THROW 51444, 'A sales invoice without lines cannot be posted.', 1;

    DECLARE @Msg NVARCHAR(400) = NULL;

    SELECT TOP (1)
        @Msg = 'Insufficient stock for item ' + S.ItemCode + ' (available '
               + CAST(S.OnHandQuantity AS VARCHAR(30)) + ', required ' + CAST(Q.Qty AS VARCHAR(30)) + ').'
    FROM
    (
        SELECT ItemmasterId, SUM(Quantity) AS Qty
        FROM dbo.SalesInvoiceDetail
        WHERE SalesInvoiceId = @Id
        GROUP BY ItemmasterId
    ) Q
    INNER JOIN dbo.vw_ItemStock S ON S.ItemmasterId = Q.ItemmasterId
    WHERE S.OnHandQuantity < Q.Qty
    ORDER BY S.ItemmasterId;

    IF @Msg IS NOT NULL
        THROW 51432, @Msg, 1;

    UPDATE dbo.SalesInvoice
    SET Status = 'Posted', UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id;

    COMMIT TRANSACTION;

    SELECT CAST(1 AS BIT) AS Success;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalesInvoice_Cancel
(
    @Id INT,
    @UpdatedBy VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Status VARCHAR(20);

    SELECT @Status = Status
    FROM dbo.SalesInvoice WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 51404, 'Sales invoice not found.', 1;

    IF @Status = 'Cancelled'
        THROW 51445, 'The sales invoice is already cancelled.', 1;

    -- Cancelling a Posted invoice returns the stock automatically (vw_ItemStock only counts Posted invoices)
    UPDATE dbo.SalesInvoice
    SET Status = 'Cancelled', UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
    WHERE Id = @Id;

    SELECT CAST(1 AS BIT) AS Success;
END;
GO