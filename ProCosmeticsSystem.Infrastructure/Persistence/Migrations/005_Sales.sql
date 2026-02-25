-- 005_Sales.sql

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Salesmen')
BEGIN
    CREATE TABLE Salesmen (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(256) NULL,
        CommissionRate DECIMAL(5,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    CREATE TABLE Sales (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SaleNumber NVARCHAR(50) NOT NULL,
        CustomerId INT NULL REFERENCES Customers(Id),
        SalesmanId INT NULL REFERENCES Salesmen(Id),
        SaleDate DATETIME2 NOT NULL,
        SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Tax DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        PaymentMethod INT NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0,
        Notes NVARCHAR(1000) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_Sales_SaleNumber ON Sales(SaleNumber);
    CREATE INDEX IX_Sales_CustomerId ON Sales(CustomerId);
    CREATE INDEX IX_Sales_SalesmanId ON Sales(SalesmanId);
    CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleItems')
BEGIN
    CREATE TABLE SaleItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SaleId INT NOT NULL REFERENCES Sales(Id) ON DELETE CASCADE,
        ProductId INT NOT NULL REFERENCES Products(Id),
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalPrice DECIMAL(18,2) NOT NULL
    );

    CREATE INDEX IX_SaleItems_SaleId ON SaleItems(SaleId);
END
GO
