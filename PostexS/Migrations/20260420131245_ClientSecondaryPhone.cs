using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class ClientSecondaryPhone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSecondaryPhone",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientSecondaryPhone",
                table: "Orders");
        }
    }
}
