-- Add QuantityReceived column to track partial receives
ALTER TABLE PurchaseOrderItems ADD QuantityReceived INT NOT NULL DEFAULT 0;
