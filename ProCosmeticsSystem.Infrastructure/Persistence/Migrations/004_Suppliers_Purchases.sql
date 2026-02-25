-- 004_Suppliers_Purchases.sql

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        ContactPerson NVARCHAR(200) NULL,
        Email NVARCHAR(256) NULL,
        Phone NVARCHAR(20) NULL,
        Address NVARCHAR(500) NULL,
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
BEGIN
    CREATE TABLE PurchaseOrders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SupplierId INT NOT NULL REFERENCES Suppliers(Id),
        OrderNumber NVARCHAR(50) NOT NULL,
        OrderDate DATETIME2 NOT NULL,
        ExpectedDeliveryDate DATETIME2 NULL,
        Status INT NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Notes NVARCHAR(1000) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_PurchaseOrders_SupplierId ON PurchaseOrders(SupplierId);
    CREATE INDEX IX_PurchaseOrders_OrderNumber ON PurchaseOrders(OrderNumber);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrderItems')
BEGIN
    CREATE TABLE PurchaseOrderItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PurchaseOrderId INT NOT NULL REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
        ProductId INT NOT NULL REFERENCES Products(Id),
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        TotalPrice DECIMAL(18,2) NOT NULL
    );

    CREATE INDEX IX_PurchaseOrderItems_PurchaseOrderId ON PurchaseOrderItems(PurchaseOrderId);
END
GO
