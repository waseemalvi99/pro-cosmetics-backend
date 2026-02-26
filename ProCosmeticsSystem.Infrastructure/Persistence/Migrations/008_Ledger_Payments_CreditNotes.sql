-- 008_Ledger_Payments_CreditNotes.sql

-- Add credit fields to Customers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'CreditDays')
BEGIN
    ALTER TABLE Customers ADD CreditDays INT NOT NULL DEFAULT 0;
    ALTER TABLE Customers ADD CreditLimit DECIMAL(18,2) NOT NULL DEFAULT 0;
END
GO

-- Add payment term fields to Suppliers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'PaymentTermDays')
BEGIN
    ALTER TABLE Suppliers ADD PaymentTermDays INT NOT NULL DEFAULT 0;
END
GO

-- Add DueDate to Sales
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DueDate')
BEGIN
    ALTER TABLE Sales ADD DueDate DATETIME2 NULL;
END
GO

-- Add PaymentTermDays and DueDate to PurchaseOrders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'PaymentTermDays')
BEGIN
    ALTER TABLE PurchaseOrders ADD PaymentTermDays INT NOT NULL DEFAULT 0;
    ALTER TABLE PurchaseOrders ADD DueDate DATETIME2 NULL;
END
GO

-- Create LedgerEntries table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LedgerEntries')
BEGIN
    CREATE TABLE LedgerEntries (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EntryDate DATETIME2 NOT NULL,
        AccountType INT NOT NULL, -- 0=CustomerReceivable, 1=SupplierPayable
        CustomerId INT NULL REFERENCES Customers(Id),
        SupplierId INT NULL REFERENCES Suppliers(Id),
        ReferenceType NVARCHAR(50) NOT NULL, -- Sale, PurchaseOrder, Payment, CreditNote, DebitNote
        ReferenceId INT NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        DebitAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreditAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsReversed BIT NOT NULL DEFAULT 0,
        ReversedByEntryId INT NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_LedgerEntries_CustomerId ON LedgerEntries(CustomerId);
    CREATE INDEX IX_LedgerEntries_SupplierId ON LedgerEntries(SupplierId);
    CREATE INDEX IX_LedgerEntries_EntryDate ON LedgerEntries(EntryDate);
    CREATE INDEX IX_LedgerEntries_ReferenceType_ReferenceId ON LedgerEntries(ReferenceType, ReferenceId);
END
GO

-- Create Payments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE Payments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(50) NOT NULL,
        PaymentType INT NOT NULL, -- 0=CustomerReceipt, 1=SupplierPayment
        CustomerId INT NULL REFERENCES Customers(Id),
        SupplierId INT NULL REFERENCES Suppliers(Id),
        PaymentDate DATETIME2 NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod INT NOT NULL, -- 0=Cash, 1=Cheque, 2=BankTransfer
        ChequeNumber NVARCHAR(50) NULL,
        BankName NVARCHAR(200) NULL,
        ChequeDate DATETIME2 NULL,
        BankAccountReference NVARCHAR(100) NULL,
        Notes NVARCHAR(1000) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_Payments_ReceiptNumber ON Payments(ReceiptNumber);
    CREATE INDEX IX_Payments_CustomerId ON Payments(CustomerId);
    CREATE INDEX IX_Payments_SupplierId ON Payments(SupplierId);
    CREATE INDEX IX_Payments_PaymentDate ON Payments(PaymentDate);
END
GO

-- Create CreditDebitNotes table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CreditDebitNotes')
BEGIN
    CREATE TABLE CreditDebitNotes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NoteNumber NVARCHAR(50) NOT NULL,
        NoteType INT NOT NULL, -- 0=CreditNote, 1=DebitNote
        AccountType INT NOT NULL, -- 0=Customer, 1=Supplier
        CustomerId INT NULL REFERENCES Customers(Id),
        SupplierId INT NULL REFERENCES Suppliers(Id),
        NoteDate DATETIME2 NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        SaleId INT NULL REFERENCES Sales(Id),
        PurchaseOrderId INT NULL REFERENCES PurchaseOrders(Id),
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    CREATE INDEX IX_CreditDebitNotes_NoteNumber ON CreditDebitNotes(NoteNumber);
    CREATE INDEX IX_CreditDebitNotes_CustomerId ON CreditDebitNotes(CustomerId);
    CREATE INDEX IX_CreditDebitNotes_SupplierId ON CreditDebitNotes(SupplierId);
END
GO

-- Seed permissions
IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Ledger:View')
BEGIN
    INSERT INTO Permissions (Name, Module, Description)
    VALUES
        ('Ledger:View', 'Ledger', 'View ledger entries'),
        ('Ledger:Create', 'Ledger', 'Create manual ledger entries'),
        ('Payments:View', 'Payments', 'View payments'),
        ('Payments:Create', 'Payments', 'Create payments'),
        ('Payments:Delete', 'Payments', 'Void payments'),
        ('CreditNotes:View', 'CreditNotes', 'View credit/debit notes'),
        ('CreditNotes:Create', 'CreditNotes', 'Create credit/debit notes'),
        ('CreditNotes:Delete', 'CreditNotes', 'Void credit/debit notes'),
        ('Accounts:View', 'Accounts', 'View account statements and aging'),
        ('Accounts:Export', 'Accounts', 'Export account statements as PDF');
END
GO

-- Assign new permissions to Admin role
DECLARE @AdminRoleId NVARCHAR(450);
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId)
    SELECT @AdminRoleId, p.Id
    FROM Permissions p
    WHERE p.Name IN ('Ledger:View', 'Ledger:Create', 'Payments:View', 'Payments:Create', 'Payments:Delete',
                     'CreditNotes:View', 'CreditNotes:Create', 'CreditNotes:Delete', 'Accounts:View', 'Accounts:Export')
    AND p.Id NOT IN (SELECT PermissionId FROM RolePermissions WHERE RoleId = @AdminRoleId);
END
GO
