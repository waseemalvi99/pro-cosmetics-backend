-- 002_Customers.sql

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE Customers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        Email NVARCHAR(256) NULL,
        Phone NVARCHAR(20) NULL,
        Address NVARCHAR(500) NULL,
        City NVARCHAR(100) NULL,
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_Customers_FullName ON Customers(FullName);
    CREATE INDEX IX_Customers_Email ON Customers(Email);
    CREATE INDEX IX_Customers_IsDeleted ON Customers(IsDeleted);
END
GO
