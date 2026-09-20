/*
============================================================
Invoice Management System
CI Database Initialization
Database: Accounts_Test_CI

Purpose:
    Creates the database schema and stored procedures
    required by Invoice.DAL.Test / Invoice.BAL.Test /
    Invoice.CoreAPI.Test.

This script is intended for a fresh CI database.
============================================================
*/
SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================
   CATEGORY
   ============================================================ */

IF OBJECT_ID('dbo.Category', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Category
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Code VARCHAR(5) NOT NULL,
        Name VARCHAR(25) NOT NULL,
        Description VARCHAR(100) NULL,
        IsActive BIT NULL,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME NULL,
        UpdatedBy VARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,

        CONSTRAINT PK_Category
            PRIMARY KEY (Id)
    );
END
GO


/* ============================================================
   CUSTOMER
   ============================================================ */

IF OBJECT_ID('dbo.Customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer
    (
        Id INT IDENTITY(1,1) NOT NULL,
        CustomerCode VARCHAR(20) NOT NULL,
        CustomerName NVARCHAR(100) NOT NULL,
        ContactPerson NVARCHAR(100) NULL,
        MobileNo VARCHAR(20) NULL,
        Email VARCHAR(100) NULL,
        Address1 NVARCHAR(200) NULL,
        Address2 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        State NVARCHAR(100) NULL,
        Country NVARCHAR(100) NULL,
        ZipCode VARCHAR(20) NULL,
        GstNo VARCHAR(50) NULL,
        IsActive BIT NOT NULL,
        IsDeleted BIT NOT NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedDate DATETIME NOT NULL,
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,

        CONSTRAINT PK_Customer
            PRIMARY KEY (Id)
    );
END
GO


/* ============================================================
   ITEMMASTER
   ============================================================ */

IF OBJECT_ID('dbo.Itemmaster', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Itemmaster
    (
        Id INT IDENTITY(1,1) NOT NULL,
        CategoryId INT NOT NULL,
        ItemBarCode VARCHAR(25) NOT NULL,
        ItemCode VARCHAR(10) NOT NULL,
        ItemName VARCHAR(100) NOT NULL,
        Description VARCHAR(250) NULL,
        Uom VARCHAR(3) NOT NULL,
        Rate DECIMAL(18,2) NULL,
        MinimumStock DECIMAL(18,2) NULL,
        MaximumStock DECIMAL(18,2) NULL,
        IsActive BIT NULL,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME NULL,
        UpdatedBy VARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,

        CONSTRAINT PK_Itemmaster
            PRIMARY KEY (Id),

        CONSTRAINT FK_Itemmaster_Category
            FOREIGN KEY (CategoryId)
            REFERENCES dbo.Category(Id)
    );
END
GO


/* ============================================================
   PRODUCTS
   ============================================================ */

IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(150) NULL,
        Description NVARCHAR(500) NULL,
        Price DECIMAL(10,2) NOT NULL,
        Stock INT NOT NULL,
        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_Products
            PRIMARY KEY (Id)
    );
END
GO


/* ============================================================
   USERS
   ============================================================ */

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) NOT NULL,
        UserName VARCHAR(100) NOT NULL,
        Email VARCHAR(255) NOT NULL,
        PasswordHash VARCHAR(500) NOT NULL,
        FirstName VARCHAR(100) NOT NULL,
        MiddleName VARCHAR(100) NULL,
        LastName VARCHAR(100) NOT NULL,
        DisplayName VARCHAR(200) NOT NULL,
        PhoneNumber VARCHAR(25) NOT NULL,
        AlternatePhone VARCHAR(25) NULL,
        AddressLine1 VARCHAR(255) NOT NULL,
        AddressLine2 VARCHAR(255) NULL,
        City VARCHAR(100) NOT NULL,
        State VARCHAR(100) NOT NULL,
        ZipCode VARCHAR(20) NOT NULL,
        Country VARCHAR(100) NOT NULL,
        DateOfBirth DATE NULL,
        IsActive BIT NOT NULL,
        IsDeleted BIT NOT NULL,
        LastLoginDate DATETIME NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedDate DATETIME NOT NULL,
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,

        CONSTRAINT PK_Users
            PRIMARY KEY (Id)
    );
END
GO


/* ============================================================
   VENDOR
   ============================================================ */

IF OBJECT_ID('dbo.Vendor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vendor
    (
        Id INT IDENTITY(1,1) NOT NULL,
        VendorCode VARCHAR(20) NOT NULL,
        VendorName NVARCHAR(100) NOT NULL,
        ContactPerson NVARCHAR(100) NULL,
        MobileNo VARCHAR(20) NULL,
        Email VARCHAR(100) NULL,
        Address1 NVARCHAR(200) NULL,
        Address2 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        State NVARCHAR(100) NULL,
        Country NVARCHAR(100) NULL,
        ZipCode VARCHAR(20) NULL,
        GstNo VARCHAR(50) NULL,
        IsActive BIT NOT NULL,
        IsDeleted BIT NOT NULL,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME NOT NULL,
        UpdatedBy VARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,

        CONSTRAINT PK_Vendor
            PRIMARY KEY (Id)
    );
END
GO


/* ============================================================
   CATEGORY PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_Category_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Category
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Category_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Code,
        Name,
        Description,
        IsActive,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Category
    ORDER BY Id ASC;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Category_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Code,
        Name,
        Description,
        IsActive,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Category
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Category_Insert
    @Code VARCHAR(5),
    @Name VARCHAR(25),
    @Description VARCHAR(100),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Category
    (
        Code,
        Name,
        Description,
        IsActive,
        CreatedBy,
        CreatedDate
    )
    OUTPUT INSERTED.Id
    VALUES
    (
        @Code,
        @Name,
        @Description,
        @IsActive,
        SYSTEM_USER,
        GETDATE()
    );
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Category_Update
    @Id INT,
    @Code VARCHAR(5),
    @Name VARCHAR(25),
    @Description VARCHAR(100),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Category
    SET
        Code = @Code,
        Name = @Name,
        Description = @Description,
        IsActive = @IsActive,
        UpdatedBy = SYSTEM_USER,
        UpdatedDate = GETDATE()
    WHERE Id = @Id;
END
GO


/* ============================================================
   CUSTOMER PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_Customer_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Customer
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Customer_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        CustomerCode,
        CustomerName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Customer;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Customer_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        CustomerCode,
        CustomerName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Customer
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Customer_GetPaged
    @CustomerCode VARCHAR(20) = NULL,
    @CustomerName VARCHAR(100) = NULL,
    @MobileNo NVARCHAR(20) = NULL,
    @City NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF (@PageNumber <= 0)
        SET @PageNumber = 1;

    IF (@PageSize <= 0)
        SET @PageSize = 10;

    DECLARE @Offset INT =
        (@PageNumber - 1) * @PageSize;

    SELECT
        Id,
        CustomerCode,
        CustomerName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate,
        COUNT(*) OVER() AS TotalRecords
    FROM dbo.Customer
    WHERE
        (@CustomerCode IS NULL
            OR CustomerCode LIKE '%' + @CustomerCode + '%')
        AND
        (@CustomerName IS NULL
            OR CustomerName LIKE '%' + @CustomerName + '%')
        AND
        (@MobileNo IS NULL
            OR MobileNo LIKE '%' + @MobileNo + '%')
        AND
        (@City IS NULL
            OR City LIKE '%' + @City + '%')
    ORDER BY Id ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(1) AS TotalRecords
    FROM dbo.Customer
    WHERE
        (@CustomerCode IS NULL
            OR CustomerCode LIKE '%' + @CustomerCode + '%')
        AND
        (@CustomerName IS NULL
            OR CustomerName LIKE '%' + @CustomerName + '%')
        AND
        (@MobileNo IS NULL
            OR MobileNo LIKE '%' + @MobileNo + '%')
        AND
        (@City IS NULL
            OR City LIKE '%' + @City + '%');
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Customer_Insert
    @CustomerCode VARCHAR(20),
    @CustomerName NVARCHAR(100),
    @ContactPerson NVARCHAR(100) = NULL,
    @MobileNo VARCHAR(20) = NULL,
    @Email VARCHAR(100) = NULL,
    @Address1 NVARCHAR(200) = NULL,
    @Address2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @Country NVARCHAR(100) = NULL,
    @ZipCode VARCHAR(20) = NULL,
    @GstNo VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Customer
    (
        CustomerCode,
        CustomerName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    )
    VALUES
    (
        @CustomerCode,
        @CustomerName,
        @ContactPerson,
        @MobileNo,
        @Email,
        @Address1,
        @Address2,
        @City,
        @State,
        @Country,
        @ZipCode,
        @GstNo,
        1,
        0,
        SYSTEM_USER,
        GETDATE(),
        NULL,
        NULL
    );
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Customer_Update
    @Id INT,
    @CustomerCode VARCHAR(20),
    @CustomerName NVARCHAR(100),
    @ContactPerson NVARCHAR(100) = NULL,
    @MobileNo VARCHAR(20) = NULL,
    @Email VARCHAR(100) = NULL,
    @Address1 NVARCHAR(200) = NULL,
    @Address2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @Country NVARCHAR(100) = NULL,
    @ZipCode VARCHAR(20) = NULL,
    @GstNo VARCHAR(50) = NULL,
    @IsActive BIT,
    @IsDeleted BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Customer
    SET
        CustomerCode = @CustomerCode,
        CustomerName = @CustomerName,
        ContactPerson = @ContactPerson,
        MobileNo = @MobileNo,
        Email = @Email,
        Address1 = @Address1,
        Address2 = @Address2,
        City = @City,
        State = @State,
        Country = @Country,
        ZipCode = @ZipCode,
        GstNo = @GstNo,
        IsActive = @IsActive,
        IsDeleted = @IsDeleted,
        UpdatedBy = SYSTEM_USER,
        UpdatedDate = GETDATE()
    WHERE Id = @Id;
END
GO


/* ============================================================
   ITEMMASTER PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Itemmaster
    WHERE Id = @Id;

    SELECT @Id AS Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_GetActiveCountByCategory
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM dbo.Itemmaster
    WHERE CategoryId = @CategoryId
      AND IsActive = 1;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        I.Id,
        I.CategoryId,
        C.Code AS CategoryCode,
        C.Name AS CategoryName,
        I.ItemBarCode,
        I.Itemcode,
        I.Itemname,
        I.Description,
        I.Uom,
        I.Rate,
        I.Minimumstock,
        I.Maximumstock,
        I.IsActive,
        I.Createdby,
        I.Createddate,
        I.Updatedby,
        I.Updateddate
    FROM dbo.Itemmaster I
    INNER JOIN dbo.Category C
        ON I.CategoryId = C.Id
    ORDER BY I.Id ASC;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        I.Id,
        I.CategoryId,
        C.Code AS CategoryCode,
        C.Name AS CategoryName,
        I.ItemBarCode,
        I.Itemcode,
        I.Itemname,
        I.Description,
        I.Uom,
        I.Rate,
        I.Minimumstock,
        I.Maximumstock,
        I.IsActive,
        I.Createdby,
        I.Createddate,
        I.Updatedby,
        I.Updateddate
    FROM dbo.Itemmaster I
    INNER JOIN dbo.Category C
        ON I.CategoryId = C.Id
    WHERE I.Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_GetPaged
    @CategoryId INT = NULL,
    @ItemBarCode VARCHAR(25) = NULL,
    @ItemCode VARCHAR(10) = NULL,
    @ItemName VARCHAR(100) = NULL,
    @Uom VARCHAR(3) = NULL,
    @IsActive BIT = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF (@PageNumber <= 0)
        SET @PageNumber = 1;

    IF (@PageSize <= 0)
        SET @PageSize = 10;

    DECLARE @Offset INT =
        (@PageNumber - 1) * @PageSize;

    SELECT
        I.Id,
        I.CategoryId,
        C.Code AS CategoryCode,
        C.Name AS CategoryName,
        I.ItemBarCode,
        I.Itemcode,
        I.Itemname,
        I.Description,
        I.Uom,
        I.Rate,
        I.Minimumstock,
        I.Maximumstock,
        I.IsActive,
        I.Createdby,
        I.Createddate,
        I.Updatedby,
        I.Updateddate
    FROM dbo.Itemmaster I
    INNER JOIN dbo.Category C
        ON I.CategoryId = C.Id
    WHERE
        (@CategoryId IS NULL
            OR I.CategoryId = @CategoryId)
        AND
        (@ItemBarCode IS NULL
            OR I.ItemBarCode LIKE '%' + @ItemBarCode + '%')
        AND
        (@ItemCode IS NULL
            OR I.Itemcode LIKE '%' + @ItemCode + '%')
        AND
        (@ItemName IS NULL
            OR I.Itemname LIKE '%' + @ItemName + '%')
        AND
        (@Uom IS NULL
            OR I.Uom LIKE '%' + @Uom + '%')
        AND
        (@IsActive IS NULL
            OR I.IsActive = @IsActive)
    ORDER BY I.Id DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT
        COUNT(1) AS TotalRecords
    FROM dbo.Itemmaster I
    INNER JOIN dbo.Category C
        ON I.CategoryId = C.Id
    WHERE
        (@CategoryId IS NULL
            OR I.CategoryId = @CategoryId)
        AND
        (@ItemBarCode IS NULL
            OR I.ItemBarCode LIKE '%' + @ItemBarCode + '%')
        AND
        (@ItemCode IS NULL
            OR I.Itemcode LIKE '%' + @ItemCode + '%')
        AND
        (@ItemName IS NULL
            OR I.Itemname LIKE '%' + @ItemName + '%')
        AND
        (@Uom IS NULL
            OR I.Uom LIKE '%' + @Uom + '%')
        AND
        (@IsActive IS NULL
            OR I.IsActive = @IsActive);
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_Insert
    @CategoryId INT,
    @ItemBarCode VARCHAR(25),
    @Itemcode VARCHAR(10),
    @Itemname VARCHAR(100),
    @Description VARCHAR(250),
    @Uom VARCHAR(3),
    @Rate DECIMAL(18,2),
    @Minimumstock DECIMAL(18,2),
    @Maximumstock DECIMAL(18,2),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Itemmaster
    (
        CategoryId,
        ItemBarCode,
        Itemcode,
        Itemname,
        Description,
        Uom,
        Rate,
        Minimumstock,
        Maximumstock,
        IsActive,
        Createdby,
        Createddate,
        Updatedby,
        Updateddate
    )
    VALUES
    (
        @CategoryId,
        @ItemBarCode,
        @Itemcode,
        @Itemname,
        @Description,
        @Uom,
        @Rate,
        @Minimumstock,
        @Maximumstock,
        @IsActive,
        SYSTEM_USER,
        GETDATE(),
        NULL,
        NULL
    );
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Itemmaster_Update
    @Id INT,
    @CategoryId INT,
    @ItemBarCode VARCHAR(25),
    @Itemcode VARCHAR(10),
    @Itemname VARCHAR(100),
    @Description VARCHAR(250),
    @Uom VARCHAR(3),
    @Rate DECIMAL(18,2),
    @Minimumstock DECIMAL(18,2),
    @Maximumstock DECIMAL(18,2),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT OFF;

    UPDATE dbo.Itemmaster
    SET
        CategoryId = @CategoryId,
        ItemBarCode = @ItemBarCode,
        Itemcode = @Itemcode,
        Itemname = @Itemname,
        Description = @Description,
        Uom = @Uom,
        Rate = @Rate,
        Minimumstock = @Minimumstock,
        Maximumstock = @Maximumstock,
        IsActive = @IsActive,
        Updatedby = SYSTEM_USER,
        Updateddate = GETDATE()
    WHERE Id = @Id;

    SELECT @Id AS Id;
END
GO


/* ============================================================
   PRODUCT PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_Product_Delete
    @Id INT,
    @RowVersion VARBINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Products
    WHERE
        Id = @Id
        AND RowVersion = @RowVersion;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Product_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Name,
        Description,
        Price,
        Stock,
        RowVersion
    FROM dbo.Products
    ORDER BY Id DESC;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Product_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Name,
        Description,
        Price,
        Stock,
        RowVersion
    FROM dbo.Products
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Product_Insert
    @Id INT OUTPUT,
    @Name NVARCHAR(150),
    @Description NVARCHAR(500) = NULL,
    @Price DECIMAL(10,2),
    @Stock INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Products
    (
        Name,
        Description,
        Price,
        Stock
    )
    VALUES
    (
        @Name,
        @Description,
        @Price,
        @Stock
    );

    SET @Id = CONVERT(INT, SCOPE_IDENTITY());
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Product_Update
    @Id INT,
    @Name NVARCHAR(150),
    @Description NVARCHAR(500) = NULL,
    @Price DECIMAL(10,2),
    @Stock INT,
    @RowVersion VARBINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Products
    SET
        Name = @Name,
        Description = @Description,
        Price = @Price,
        Stock = @Stock
    WHERE
        Id = @Id
        AND RowVersion = @RowVersion;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO


/* ============================================================
   USER PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_User_Delete
    @Id INT,
    @UpdatedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE Id = @Id
          AND IsDeleted = 0
    )
    BEGIN
        SELECT CAST(0 AS BIT) AS Success;
        RETURN;
    END;

    UPDATE dbo.Users
    SET
        IsDeleted = 1,
        IsActive = 0,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETUTCDATE()
    WHERE Id = @Id;

    SELECT CAST(1 AS BIT) AS Success;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        UserName,
        Email,
        PasswordHash,
        FirstName,
        MiddleName,
        LastName,
        DisplayName,
        PhoneNumber,
        AlternatePhone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        ZipCode,
        Country,
        DateOfBirth,
        IsActive,
        IsDeleted,
        LastLoginDate,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Users
    WHERE IsDeleted = 0
    ORDER BY Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_GetByEmail
    @Email VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        UserName,
        Email,
        PasswordHash,
        FirstName,
        MiddleName,
        LastName,
        DisplayName,
        PhoneNumber,
        AlternatePhone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        ZipCode,
        Country,
        DateOfBirth,
        IsActive,
        IsDeleted,
        LastLoginDate,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Users
    WHERE Email = @Email
      AND IsDeleted = 0;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        UserName,
        Email,
        PasswordHash,
        FirstName,
        MiddleName,
        LastName,
        DisplayName,
        PhoneNumber,
        AlternatePhone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        ZipCode,
        Country,
        DateOfBirth,
        IsActive,
        IsDeleted,
        LastLoginDate,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Users
    WHERE Id = @Id
      AND IsDeleted = 0;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_GetByUserName
    @UserName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        UserName,
        Email,
        PasswordHash,
        FirstName,
        MiddleName,
        LastName,
        DisplayName,
        PhoneNumber,
        AlternatePhone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        ZipCode,
        Country,
        DateOfBirth,
        IsActive,
        IsDeleted,
        LastLoginDate,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Users
    WHERE UserName = @UserName
      AND IsDeleted = 0;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_GetPaged
    @UserName VARCHAR(100) = NULL,
    @Email VARCHAR(255) = NULL,
    @FirstName VARCHAR(100) = NULL,
    @LastName VARCHAR(100) = NULL,
    @IsActive BIT = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF (@PageNumber <= 0)
        SET @PageNumber = 1;

    IF (@PageSize <= 0)
        SET @PageSize = 10;

    DECLARE @Offset INT =
        (@PageNumber - 1) * @PageSize;

    SELECT
        Id,
        UserName,
        Email,
        PasswordHash,
        FirstName,
        MiddleName,
        LastName,
        DisplayName,
        PhoneNumber,
        AlternatePhone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        ZipCode,
        Country,
        DateOfBirth,
        IsActive,
        IsDeleted,
        LastLoginDate,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate,
        COUNT(*) OVER() AS TotalRecords
    FROM dbo.Users
    WHERE
        IsDeleted = 0
        AND
        (
            @UserName IS NULL
            OR UserName LIKE '%' + @UserName + '%'
        )
        AND
        (
            @Email IS NULL
            OR Email LIKE '%' + @Email + '%'
        )
        AND
        (
            @FirstName IS NULL
            OR FirstName LIKE '%' + @FirstName + '%'
        )
        AND
        (
            @LastName IS NULL
            OR LastName LIKE '%' + @LastName + '%'
        )
        AND
        (
            @IsActive IS NULL
            OR IsActive = @IsActive
        )
    ORDER BY Id ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT
        COUNT(1) AS TotalRecords
    FROM dbo.Users
    WHERE
        IsDeleted = 0
        AND
        (
            @UserName IS NULL
            OR UserName LIKE '%' + @UserName + '%'
        )
        AND
        (
            @Email IS NULL
            OR Email LIKE '%' + @Email + '%'
        )
        AND
        (
            @FirstName IS NULL
            OR FirstName LIKE '%' + @FirstName + '%'
        )
        AND
        (
            @LastName IS NULL
            OR LastName LIKE '%' + @LastName + '%'
        )
        AND
        (
            @IsActive IS NULL
            OR IsActive = @IsActive
        );
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_Insert
    @UserName VARCHAR(100),
    @Email VARCHAR(255),
    @PasswordHash VARCHAR(500),
    @FirstName VARCHAR(100),
    @MiddleName VARCHAR(100) = NULL,
    @LastName VARCHAR(100),
    @DisplayName VARCHAR(200),
    @PhoneNumber VARCHAR(25),
    @AlternatePhone VARCHAR(25) = NULL,
    @AddressLine1 VARCHAR(255),
    @AddressLine2 VARCHAR(255) = NULL,
    @City VARCHAR(100),
    @State VARCHAR(100),
    @ZipCode VARCHAR(20),
    @Country VARCHAR(100),
    @DateOfBirth DATE = NULL,
    @IsActive BIT = 1,
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserName = @UserName
          AND IsDeleted = 0
    )
    BEGIN
        THROW 50001, 'Username already exists.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE Email = @Email
          AND IsDeleted = 0
    )
    BEGIN
        THROW 50002, 'Email already exists.', 1;
    END;

    INSERT INTO dbo.Users
    (
        UserName,
        Email,
        PasswordHash,
        FirstName,
        MiddleName,
        LastName,
        DisplayName,
        PhoneNumber,
        AlternatePhone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        ZipCode,
        Country,
        DateOfBirth,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate
    )
    VALUES
    (
        @UserName,
        @Email,
        @PasswordHash,
        @FirstName,
        @MiddleName,
        @LastName,
        @DisplayName,
        @PhoneNumber,
        @AlternatePhone,
        @AddressLine1,
        @AddressLine2,
        @City,
        @State,
        @ZipCode,
        @Country,
        @DateOfBirth,
        @IsActive,
        0,
        @CreatedBy,
        GETUTCDATE()
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_Update
    @Id INT,
    @UserName VARCHAR(100),
    @Email VARCHAR(255),
    @PasswordHash VARCHAR(500),
    @FirstName VARCHAR(100),
    @MiddleName VARCHAR(100) = NULL,
    @LastName VARCHAR(100),
    @DisplayName VARCHAR(200),
    @PhoneNumber VARCHAR(25),
    @AlternatePhone VARCHAR(25) = NULL,
    @AddressLine1 VARCHAR(255),
    @AddressLine2 VARCHAR(255) = NULL,
    @City VARCHAR(100),
    @State VARCHAR(100),
    @ZipCode VARCHAR(20),
    @Country VARCHAR(100),
    @DateOfBirth DATE = NULL,
    @IsActive BIT,
    @UpdatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE Id = @Id
          AND IsDeleted = 0
    )
    BEGIN
        THROW 50003, 'User not found.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserName = @UserName
          AND Id <> @Id
          AND IsDeleted = 0
    )
    BEGIN
        THROW 50004, 'Username already exists.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE Email = @Email
          AND Id <> @Id
          AND IsDeleted = 0
    )
    BEGIN
        THROW 50005, 'Email already exists.', 1;
    END;

    UPDATE dbo.Users
    SET
        UserName = @UserName,
        Email = @Email,
        PasswordHash = @PasswordHash,
        FirstName = @FirstName,
        MiddleName = @MiddleName,
        LastName = @LastName,
        DisplayName = @DisplayName,
        PhoneNumber = @PhoneNumber,
        AlternatePhone = @AlternatePhone,
        AddressLine1 = @AddressLine1,
        AddressLine2 = @AddressLine2,
        City = @City,
        State = @State,
        ZipCode = @ZipCode,
        Country = @Country,
        DateOfBirth = @DateOfBirth,
        IsActive = @IsActive,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETUTCDATE()
    WHERE Id = @Id;

    SELECT CAST(1 AS BIT) AS Success;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_User_UpdateLastLogin
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET LastLoginDate = GETUTCDATE()
    WHERE Id = @Id
      AND IsDeleted = 0
      AND IsActive = 1;

    IF @@ROWCOUNT > 0
        SELECT CAST(1 AS BIT) AS Success;
    ELSE
        SELECT CAST(0 AS BIT) AS Success;
END
GO


/* ============================================================
   VENDOR PROCEDURES
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_Vendor_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Vendor
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Vendor_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        VendorCode,
        VendorName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Vendor;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Vendor_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        VendorCode,
        VendorName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Vendor
    WHERE Id = @Id;
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Vendor_GetPaged
    @VendorCode VARCHAR(20) = NULL,
    @VendorName VARCHAR(100) = NULL,
    @MobileNo NVARCHAR(20) = NULL,
    @City NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF (@PageNumber <= 0)
        SET @PageNumber = 1;

    IF (@PageSize <= 0)
        SET @PageSize = 10;

    DECLARE @Offset INT =
        (@PageNumber - 1) * @PageSize;

    SELECT
        Id,
        VendorCode,
        VendorName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.Vendor
    WHERE
        (@VendorCode IS NULL
            OR VendorCode LIKE '%' + @VendorCode + '%')
        AND
        (@VendorName IS NULL
            OR VendorName LIKE '%' + @VendorName + '%')
        AND
        (@MobileNo IS NULL
            OR MobileNo LIKE '%' + @MobileNo + '%')
        AND
        (@City IS NULL
            OR City LIKE '%' + @City + '%')
    ORDER BY Id ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT
        COUNT(1) AS TotalRecords
    FROM dbo.Vendor
    WHERE
        (@VendorCode IS NULL
            OR VendorCode LIKE '%' + @VendorCode + '%')
        AND
        (@VendorName IS NULL
            OR VendorName LIKE '%' + @VendorName + '%')
        AND
        (@MobileNo IS NULL
            OR MobileNo LIKE '%' + @MobileNo + '%')
        AND
        (@City IS NULL
            OR City LIKE '%' + @City + '%');
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Vendor_Insert
    @VendorCode VARCHAR(20),
    @VendorName NVARCHAR(100),
    @ContactPerson NVARCHAR(100) = NULL,
    @MobileNo VARCHAR(20) = NULL,
    @Email VARCHAR(100) = NULL,
    @Address1 NVARCHAR(200) = NULL,
    @Address2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @Country NVARCHAR(100) = NULL,
    @ZipCode VARCHAR(20) = NULL,
    @GstNo VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Vendor
    (
        VendorCode,
        VendorName,
        ContactPerson,
        MobileNo,
        Email,
        Address1,
        Address2,
        City,
        State,
        Country,
        ZipCode,
        GstNo,
        IsActive,
        IsDeleted,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    )
    VALUES
    (
        @VendorCode,
        @VendorName,
        @ContactPerson,
        @MobileNo,
        @Email,
        @Address1,
        @Address2,
        @City,
        @State,
        @Country,
        @ZipCode,
        @GstNo,
        1,
        0,
        SYSTEM_USER,
        GETDATE(),
        NULL,
        NULL
    );
END
GO


CREATE OR ALTER PROCEDURE dbo.sp_Vendor_Update
    @Id INT,
    @VendorCode VARCHAR(20),
    @VendorName NVARCHAR(100),
    @ContactPerson NVARCHAR(100) = NULL,
    @MobileNo VARCHAR(20) = NULL,
    @Email VARCHAR(100) = NULL,
    @Address1 NVARCHAR(200) = NULL,
    @Address2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @Country NVARCHAR(100) = NULL,
    @ZipCode VARCHAR(20) = NULL,
    @GstNo VARCHAR(50) = NULL,
    @IsActive BIT,
    @IsDeleted BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Vendor
    SET
        VendorCode = @VendorCode,
        VendorName = @VendorName,
        ContactPerson = @ContactPerson,
        MobileNo = @MobileNo,
        Email = @Email,
        Address1 = @Address1,
        Address2 = @Address2,
        City = @City,
        State = @State,
        Country = @Country,
        ZipCode = @ZipCode,
        GstNo = @GstNo,
        IsActive = @IsActive,
        IsDeleted = @IsDeleted,
        UpdatedBy = SYSTEM_USER,
        UpdatedDate = GETDATE()
    WHERE Id = @Id;
END
GO