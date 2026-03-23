using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddPayloadAndContextToErrorLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ErrorLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ErrorLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "ErrorLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestPayload",
                table: "ErrorLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsePayload",
                table: "ErrorLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "ErrorLogs",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "RequestPayload",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "ResponsePayload",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "ErrorLogs");
        }
    }
}
