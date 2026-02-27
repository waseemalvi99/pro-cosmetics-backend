-- 011: Add Email:Send permission
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Email:Send')
BEGIN
    INSERT INTO Permissions (Name, Module, Description)
    VALUES ('Email:Send', 'Email', 'Send ad-hoc emails');
END
GO

-- Assign to Admin role
DECLARE @AdminRoleId INT = (SELECT Id FROM AspNetRoles WHERE NormalizedName = 'ADMIN');
IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId)
    SELECT @AdminRoleId, Id FROM Permissions
    WHERE Name = 'Email:Send'
      AND Id NOT IN (SELECT PermissionId FROM RolePermissions WHERE RoleId = @AdminRoleId);
END
GO
