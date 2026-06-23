using Microsoft.EntityFrameworkCore.Migrations;

namespace PostexS.Migrations
{
    public partial class AddSearchPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index on Orders.ClientId - used in almost every query
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_ClientId' AND object_id = OBJECT_ID('Orders')) " +
                "CREATE NONCLUSTERED INDEX [IX_Orders_ClientId] ON [Orders] ([ClientId]) INCLUDE ([Code], [ClientPhone], [ClientCode], [Status], [IsDeleted]);");

            // Index on Orders.DeliveryId - used by driver app queries
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_DeliveryId_Status' AND object_id = OBJECT_ID('Orders')) " +
                "CREATE NONCLUSTERED INDEX [IX_Orders_DeliveryId_Status] ON [Orders] ([DeliveryId], [Status], [IsDeleted]) INCLUDE ([ClientId], [Code], [ClientPhone]);");

            // Index on Orders.Code - used for search and QR code scan
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_Code' AND object_id = OBJECT_ID('Orders')) " +
                "CREATE NONCLUSTERED INDEX [IX_Orders_Code] ON [Orders] ([Code]) INCLUDE ([ClientId], [DeliveryId], [IsDeleted]);");

            // Index on Orders.CompletedId - used by Archive queries
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_CompletedId' AND object_id = OBJECT_ID('Orders')) " +
                "CREATE NONCLUSTERED INDEX [IX_Orders_CompletedId] ON [Orders] ([CompletedId]) WHERE [CompletedId] IS NOT NULL;");

            // Index on Orders for settlement queries
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_OrderCompleted_Status' AND object_id = OBJECT_ID('Orders')) " +
                "CREATE NONCLUSTERED INDEX [IX_Orders_OrderCompleted_Status] ON [Orders] ([OrderCompleted], [Status], [IsDeleted], [Finished]) INCLUDE ([ClientId]);");

            // Index on Locations.DeliveryId - used by location tracking (91K queries)
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Locations_DeliveryId_CreateOn' AND object_id = OBJECT_ID('Locations')) " +
                "CREATE NONCLUSTERED INDEX [IX_Locations_DeliveryId_CreateOn] ON [Locations] ([DeliveryId], [IsDeleted]) INCLUDE ([CreateOn]);");

            // Index on Notifications.UserId - used by notification queries
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId' AND object_id = OBJECT_ID('Notifications')) " +
                "CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId], [IsDeleted]) INCLUDE ([IsSeen]);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Orders_ClientId] ON [Orders];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Orders_DeliveryId_Status] ON [Orders];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Orders_Code] ON [Orders];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Orders_CompletedId] ON [Orders];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Orders_OrderCompleted_Status] ON [Orders];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Locations_DeliveryId_CreateOn] ON [Locations];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Notifications_UserId] ON [Notifications];");
        }
    }
}
