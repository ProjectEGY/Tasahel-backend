using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class UseCustomCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseCustomCode",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseCustomCode",
                table: "Orders");
        }
    }
}
