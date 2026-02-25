-- 003_Products_Inventory.sql

-- Categories
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        ParentCategoryId INT NULL REFERENCES Categories(Id),
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
END
GO

-- Products
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        SKU NVARCHAR(50) NULL,
        Barcode NVARCHAR(50) NULL,
        Description NVARCHAR(2000) NULL,
        CategoryId INT NULL REFERENCES Categories(Id),
        CostPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        SalePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        ReorderLevel INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_Products_SKU ON Products(SKU);
    CREATE INDEX IX_Products_Barcode ON Products(Barcode);
    CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
    CREATE INDEX IX_Products_IsDeleted ON Products(IsDeleted);
END
GO

-- Product Images
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductImages')
BEGIN
    CREATE TABLE ProductImages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        IsPrimary BIT NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_ProductImages_ProductId ON ProductImages(ProductId);
END
GO

-- Inventory
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Inventory')
BEGIN
    CREATE TABLE Inventory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL UNIQUE REFERENCES Products(Id),
        QuantityOnHand INT NOT NULL DEFAULT 0,
        QuantityReserved INT NOT NULL DEFAULT 0,
        LastRestockedAt DATETIME2 NULL
    );
END
GO

-- Inventory Transactions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryTransactions')
BEGIN
    CREATE TABLE InventoryTransactions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL REFERENCES Products(Id),
        TransactionType INT NOT NULL,
        Quantity INT NOT NULL,
        ReferenceType NVARCHAR(50) NULL,
        ReferenceId INT NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_InventoryTransactions_ProductId ON InventoryTransactions(ProductId);
END
GO
