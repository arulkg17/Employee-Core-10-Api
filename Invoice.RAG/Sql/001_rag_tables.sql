-- Run once, only if Rag:StoreProvider = "Sql".
-- A separate database keeps agreement text and embeddings away from the business database.
IF DB_ID('InvoiceRag') IS NULL CREATE DATABASE InvoiceRag;
GO
USE InvoiceRag;
GO

CREATE TABLE dbo.VendorAgreement (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    VendorId      INT           NOT NULL,           -- id in the business database (validated through CoreAPI)
    Title         NVARCHAR(200) NOT NULL,
    FileName      NVARCHAR(260) NOT NULL,
    StoredPath    NVARCHAR(500) NOT NULL,
    Sha256        CHAR(64)      NOT NULL,
    EffectiveDate DATE          NULL,
    PageCount     INT           NOT NULL,
    ChunkCount    INT           NOT NULL,
    UploadedBy    NVARCHAR(100) NULL,
    UploadedUtc   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_VendorAgreement_Vendor_Hash UNIQUE (VendorId, Sha256)
);
CREATE INDEX IX_VendorAgreement_Vendor ON dbo.VendorAgreement (VendorId);

CREATE TABLE dbo.VendorAgreementChunk (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    AgreementId INT            NOT NULL REFERENCES dbo.VendorAgreement (Id) ON DELETE CASCADE,
    ChunkIndex  INT            NOT NULL,
    Page        INT            NOT NULL,
    Section     NVARCHAR(200)  NULL,
    Content     NVARCHAR(MAX)  NOT NULL,
    Embedding   VARBINARY(MAX) NOT NULL              -- float32 values, L2-normalised
);
CREATE INDEX IX_VendorAgreementChunk_Agreement ON dbo.VendorAgreementChunk (AgreementId);

CREATE TABLE dbo.VendorAgreementTerms (
    AgreementId  INT           NOT NULL PRIMARY KEY REFERENCES dbo.VendorAgreement (Id) ON DELETE CASCADE,
    TermsJson    NVARCHAR(MAX) NOT NULL,
    ExtractedUtc DATETIME2     NOT NULL,
    Reviewed     BIT           NOT NULL DEFAULT 0,
    ReviewedBy   NVARCHAR(100) NULL
);
GO
