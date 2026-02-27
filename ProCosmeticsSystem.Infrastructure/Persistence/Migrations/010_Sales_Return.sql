ALTER TABLE SaleItems ADD QuantityReturned INT NOT NULL DEFAULT 0;
ALTER TABLE Sales ADD ReturnedAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

INSERT INTO Permissions (Name, Module, Description)
VALUES ('Sales:Return', 'Sales', 'Process sales returns');

DECLARE @AdminRoleId NVARCHAR(450);
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId)
    SELECT @AdminRoleId, Id FROM Permissions WHERE Name = 'Sales:Return'
    AND Id NOT IN (SELECT PermissionId FROM RolePermissions WHERE RoleId = @AdminRoleId);
END
