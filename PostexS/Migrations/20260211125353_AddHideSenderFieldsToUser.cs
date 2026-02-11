using Microsoft.EntityFrameworkCore.Migrations;

namespace TasahelExpress.Migrations
{
    public partial class AddHideSenderFieldsToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideSenderCode",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideSenderName",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideSenderPhone",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideSenderCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HideSenderName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HideSenderPhone",
                table: "AspNetUsers");
        }
    }
}
