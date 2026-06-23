-- Performance indexes for Tasahel Express
-- Run this script on the production database via hosting panel SQL management
-- All indexes use IF NOT EXISTS to be safe to re-run

-- 1. Orders.ClientId - used in almost every query (3.8M user lookups were caused by missing this)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_ClientId' AND object_id = OBJECT_ID('Orders'))
CREATE NONCLUSTERED INDEX [IX_Orders_ClientId] ON [Orders] ([ClientId]) INCLUDE ([Code], [ClientPhone], [ClientCode], [Status], [IsDeleted]);
GO

-- 2. Orders.DeliveryId + Status - used by all driver app queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_DeliveryId_Status' AND object_id = OBJECT_ID('Orders'))
CREATE NONCLUSTERED INDEX [IX_Orders_DeliveryId_Status] ON [Orders] ([DeliveryId], [Status], [IsDeleted]) INCLUDE ([ClientId], [Code], [ClientPhone]);
GO

-- 3. Orders.Code - used for search and QR code scan
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_Code' AND object_id = OBJECT_ID('Orders'))
CREATE NONCLUSTERED INDEX [IX_Orders_Code] ON [Orders] ([Code]) INCLUDE ([ClientId], [DeliveryId], [IsDeleted]);
GO

-- 4. Orders.CompletedId - used by wallet/archive queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_CompletedId' AND object_id = OBJECT_ID('Orders'))
CREATE NONCLUSTERED INDEX [IX_Orders_CompletedId] ON [Orders] ([CompletedId]) WHERE [CompletedId] IS NOT NULL;
GO

-- 5. Orders settlement queries (OrderCompleted + Status)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_OrderCompleted_Status' AND object_id = OBJECT_ID('Orders'))
CREATE NONCLUSTERED INDEX [IX_Orders_OrderCompleted_Status] ON [Orders] ([OrderCompleted], [Status], [IsDeleted], [Finished]) INCLUDE ([ClientId]);
GO

-- 6. Locations.DeliveryId - location tracking had 91,545 query runs
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Locations_DeliveryId_CreateOn' AND object_id = OBJECT_ID('Locations'))
CREATE NONCLUSTERED INDEX [IX_Locations_DeliveryId_CreateOn] ON [Locations] ([DeliveryId], [IsDeleted]) INCLUDE ([CreateOn]);
GO

-- 7. Notifications.UserId - notification queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId' AND object_id = OBJECT_ID('Notifications'))
CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId], [IsDeleted]) INCLUDE ([IsSeen]);
GO

PRINT 'All indexes created successfully';
