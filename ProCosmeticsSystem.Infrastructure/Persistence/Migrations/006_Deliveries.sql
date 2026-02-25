-- 006_Deliveries.sql

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DeliveryMen')
BEGIN
    CREATE TABLE DeliveryMen (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(256) NULL,
        IsAvailable BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Deliveries')
BEGIN
    CREATE TABLE Deliveries (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SaleId INT NOT NULL REFERENCES Sales(Id),
        DeliveryManId INT NULL REFERENCES DeliveryMen(Id),
        Status INT NOT NULL DEFAULT 0,
        AssignedAt DATETIME2 NULL,
        PickedUpAt DATETIME2 NULL,
        DeliveredAt DATETIME2 NULL,
        DeliveryAddress NVARCHAR(500) NULL,
        Notes NVARCHAR(1000) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_Deliveries_SaleId ON Deliveries(SaleId);
    CREATE INDEX IX_Deliveries_DeliveryManId ON Deliveries(DeliveryManId);
    CREATE INDEX IX_Deliveries_Status ON Deliveries(Status);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
END
GO
