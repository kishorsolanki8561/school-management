using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddIsUseForOwnerAdminToMenuAndPage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUsePageForOwnerAdmin",
                table: "PageMasters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUseMenuForOwnerAdmin",
                table: "MenuMasters",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUsePageForOwnerAdmin",
                table: "PageMasters");

            migrationBuilder.DropColumn(
                name: "IsUseMenuForOwnerAdmin",
                table: "MenuMasters");
        }
    }
}
