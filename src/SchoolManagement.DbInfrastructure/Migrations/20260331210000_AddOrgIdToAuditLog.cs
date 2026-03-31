using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations;

public partial class AddOrgIdToAuditLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "OrgId",
            table: "AuditLogs",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_OrgId",
            table: "AuditLogs",
            column: "OrgId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AuditLogs_OrgId",
            table: "AuditLogs");

        migrationBuilder.DropColumn(
            name: "OrgId",
            table: "AuditLogs");
    }
}
