using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class AddCourierOrderSheets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourierOrderSheets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourierId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    CreateOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsModified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierOrderSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierOrderSheets_AspNetUsers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierOrderSheets_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierOrderSheetItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SheetId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CreateOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsModified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierOrderSheetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierOrderSheetItems_CourierOrderSheets_SheetId",
                        column: x => x.SheetId,
                        principalTable: "CourierOrderSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierOrderSheetItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierOrderSheetItems_OrderId",
                table: "CourierOrderSheetItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierOrderSheetItems_SheetId",
                table: "CourierOrderSheetItems",
                column: "SheetId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierOrderSheets_CourierId",
                table: "CourierOrderSheets",
                column: "CourierId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierOrderSheets_CreatedById",
                table: "CourierOrderSheets",
                column: "CreatedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourierOrderSheetItems");

            migrationBuilder.DropTable(
                name: "CourierOrderSheets");
        }
    }
}
