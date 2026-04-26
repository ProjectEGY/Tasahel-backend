using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class RemoveWhatsAppBotCloudInstances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppMessageLogs_WhatsAppBotCloudInstances_BotCloudInstanceId",
                table: "WhatsAppMessageLogs");

            migrationBuilder.DropTable(
                name: "WhatsAppBotCloudInstances");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppMessageLogs_BotCloudInstanceId",
                table: "WhatsAppMessageLogs");

            migrationBuilder.DropColumn(
                name: "BotCloudInstanceId",
                table: "WhatsAppMessageLogs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BotCloudInstanceId",
                table: "WhatsAppMessageLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WhatsAppBotCloudInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    CreateOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsModified = table.Column<bool>(type: "bit", nullable: false),
                    LastFailureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalSentFailed = table.Column<long>(type: "bigint", nullable: false),
                    TotalSentSuccess = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppBotCloudInstances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessageLogs_BotCloudInstanceId",
                table: "WhatsAppMessageLogs",
                column: "BotCloudInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppMessageLogs_WhatsAppBotCloudInstances_BotCloudInstanceId",
                table: "WhatsAppMessageLogs",
                column: "BotCloudInstanceId",
                principalTable: "WhatsAppBotCloudInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
