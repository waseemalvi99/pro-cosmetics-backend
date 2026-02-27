ALTER TABLE PurchaseOrders ADD ReceivedAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
ALTER TABLE PurchaseOrders ADD CloseReason NVARCHAR(500) NULL;

-- Backfill ReceivedAmount for existing received/partially received POs
UPDATE po SET ReceivedAmount = (
    SELECT ISNULL(SUM(poi.QuantityReceived * poi.UnitPrice), 0)
    FROM PurchaseOrderItems poi WHERE poi.PurchaseOrderId = po.Id
) FROM PurchaseOrders po WHERE po.Status IN (2, 3);

-- Add Purchases:Close permission
INSERT INTO Permissions (Name, Module, Description)
VALUES ('Purchases:Close', 'Purchases', 'Close partially received purchase orders');

-- Assign to Admin role
DECLARE @AdminRoleId NVARCHAR(450);
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId)
    SELECT @AdminRoleId, Id FROM Permissions WHERE Name = 'Purchases:Close'
    AND Id NOT IN (SELECT PermissionId FROM RolePermissions WHERE RoleId = @AdminRoleId);
END
