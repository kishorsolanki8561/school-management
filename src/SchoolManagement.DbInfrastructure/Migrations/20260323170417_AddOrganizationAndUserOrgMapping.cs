using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddOrganizationAndUserOrgMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "UserRoleMappings");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "States");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "Cities");

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserOrganizationMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrgId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganizationMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOrganizationMappings_Organizations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOrganizationMappings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMappings_OrgId",
                table: "UserOrganizationMappings",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMappings_UserId_OrgId",
                table: "UserOrganizationMappings",
                columns: new[] { "UserId", "OrgId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOrganizationMappings");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.AddColumn<int>(
                name: "OrgId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrgId",
                table: "UserRoleMappings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrgId",
                table: "States",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrgId",
                table: "Countries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrgId",
                table: "Cities",
                type: "int",
                nullable: true);
        }
    }
}
