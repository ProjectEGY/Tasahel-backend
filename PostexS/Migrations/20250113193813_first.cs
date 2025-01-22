using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class first : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "site",
                table: "AspNetUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "site",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }
    }
}
