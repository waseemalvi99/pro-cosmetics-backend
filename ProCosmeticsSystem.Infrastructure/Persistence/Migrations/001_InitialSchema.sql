-- 001_InitialSchema.sql
-- Identity tables are created by EF Core (AppIdentityDbContext).
-- This script creates the custom Permissions and RolePermissions tables + seed data.

-- Permissions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Permissions')
BEGIN
    CREATE TABLE Permissions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Module NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL
    );
END
GO

-- RolePermissions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RolePermissions')
BEGIN
    CREATE TABLE RolePermissions (
        RoleId INT NOT NULL,
        PermissionId INT NOT NULL,
        PRIMARY KEY (RoleId, PermissionId),
        FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
        FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
    );
END
GO

-- Seed Permissions
INSERT INTO Permissions (Name, Module, Description) VALUES
-- Customers
('Customers:View', 'Customers', 'View customers'),
('Customers:Create', 'Customers', 'Create customers'),
('Customers:Edit', 'Customers', 'Edit customers'),
('Customers:Delete', 'Customers', 'Delete customers'),
-- Products
('Products:View', 'Products', 'View products'),
('Products:Create', 'Products', 'Create products'),
('Products:Edit', 'Products', 'Edit products'),
('Products:Delete', 'Products', 'Delete products'),
-- Sales
('Sales:View', 'Sales', 'View sales'),
('Sales:Create', 'Sales', 'Create sales'),
('Sales:Edit', 'Sales', 'Edit sales'),
('Sales:Delete', 'Sales', 'Delete sales'),
-- Deliveries
('Deliveries:View', 'Deliveries', 'View deliveries'),
('Deliveries:Create', 'Deliveries', 'Create deliveries'),
('Deliveries:Edit', 'Deliveries', 'Edit deliveries'),
('Deliveries:Delete', 'Deliveries', 'Delete deliveries'),
-- Purchases
('Purchases:View', 'Purchases', 'View purchase orders'),
('Purchases:Create', 'Purchases', 'Create purchase orders'),
('Purchases:Edit', 'Purchases', 'Edit purchase orders'),
('Purchases:Delete', 'Purchases', 'Delete purchase orders'),
-- Suppliers
('Suppliers:View', 'Suppliers', 'View suppliers'),
('Suppliers:Create', 'Suppliers', 'Create suppliers'),
('Suppliers:Edit', 'Suppliers', 'Edit suppliers'),
('Suppliers:Delete', 'Suppliers', 'Delete suppliers'),
-- Reports
('Reports:View', 'Reports', 'View reports'),
-- User Management
('UserManagement:View', 'UserManagement', 'View users and roles'),
('UserManagement:Create', 'UserManagement', 'Create users and roles'),
('UserManagement:Edit', 'UserManagement', 'Edit users and roles'),
('UserManagement:Delete', 'UserManagement', 'Delete users and roles');
GO

-- Note: The Admin role and default admin user should be seeded via Identity
-- after running EF Core migrations for the Identity tables.
-- The application startup will handle seeding the Admin role with all permissions.
