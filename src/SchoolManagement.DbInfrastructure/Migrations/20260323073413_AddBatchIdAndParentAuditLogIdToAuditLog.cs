using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddBatchIdAndParentAuditLogIdToAuditLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchId",
                table: "AuditLogs",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentAuditLogId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_BatchId",
                table: "AuditLogs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ParentAuditLogId",
                table: "AuditLogs",
                column: "ParentAuditLogId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_BatchId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ParentAuditLogId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ParentAuditLogId",
                table: "AuditLogs");
        }
    }
}
