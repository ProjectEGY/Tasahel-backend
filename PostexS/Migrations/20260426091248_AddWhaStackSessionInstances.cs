using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class AddWhaStackSessionInstances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WhaStackSessionInstanceId",
                table: "WhatsAppMessageLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WhaStackSessionInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    TotalSentSuccess = table.Column<long>(type: "bigint", nullable: false),
                    TotalSentFailed = table.Column<long>(type: "bigint", nullable: false),
                    CreateOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsModified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhaStackSessionInstances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessageLogs_WhaStackSessionInstanceId",
                table: "WhatsAppMessageLogs",
                column: "WhaStackSessionInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppMessageLogs_WhaStackSessionInstances_WhaStackSessionInstanceId",
                table: "WhatsAppMessageLogs",
                column: "WhaStackSessionInstanceId",
                principalTable: "WhaStackSessionInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppMessageLogs_WhaStackSessionInstances_WhaStackSessionInstanceId",
                table: "WhatsAppMessageLogs");

            migrationBuilder.DropTable(
                name: "WhaStackSessionInstances");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppMessageLogs_WhaStackSessionInstanceId",
                table: "WhatsAppMessageLogs");

            migrationBuilder.DropColumn(
                name: "WhaStackSessionInstanceId",
                table: "WhatsAppMessageLogs");
        }
    }
}
