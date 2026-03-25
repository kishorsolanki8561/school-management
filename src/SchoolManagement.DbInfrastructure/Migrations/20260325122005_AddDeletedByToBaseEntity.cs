using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddDeletedByToBaseEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "UserRoleMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "UserOrganizationMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "States",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PageMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PageMasterModules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PageMasterModuleActionMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MenuMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MenuAndPagePermissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "UserRoleMappings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "UserOrganizationMappings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "States");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PageMasters");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PageMasterModules");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PageMasterModuleActionMappings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MenuMasters");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MenuAndPagePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Cities");
        }
    }
}
