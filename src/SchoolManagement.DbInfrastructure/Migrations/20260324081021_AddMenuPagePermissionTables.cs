using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddMenuPagePermissionTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HasChild = table.Column<bool>(type: "bit", nullable: false),
                    ParentMenuId = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_MenuMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuMasters_MenuMasters_ParentMenuId",
                        column: x => x.ParentMenuId,
                        principalTable: "MenuMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PageMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PageMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageMasters_MenuMasters_MenuId",
                        column: x => x.MenuId,
                        principalTable: "MenuMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PageMasterModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PageMasterModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageMasterModules_PageMasters_PageId",
                        column: x => x.PageId,
                        principalTable: "PageMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuAndPagePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    PageModuleId = table.Column<int>(type: "int", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_MenuAndPagePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuAndPagePermissions_MenuMasters_MenuId",
                        column: x => x.MenuId,
                        principalTable: "MenuMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuAndPagePermissions_PageMasterModules_PageModuleId",
                        column: x => x.PageModuleId,
                        principalTable: "PageMasterModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuAndPagePermissions_PageMasters_PageId",
                        column: x => x.PageId,
                        principalTable: "PageMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuAndPagePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PageMasterModuleActionMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    PageModuleId = table.Column<int>(type: "int", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PageMasterModuleActionMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageMasterModuleActionMappings_PageMasterModules_PageModuleId",
                        column: x => x.PageModuleId,
                        principalTable: "PageMasterModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageMasterModuleActionMappings_PageMasters_PageId",
                        column: x => x.PageId,
                        principalTable: "PageMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId",
                table: "MenuAndPagePermissions",
                columns: new[] { "MenuId", "PageId", "PageModuleId", "ActionId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuAndPagePermissions_PageId",
                table: "MenuAndPagePermissions",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuAndPagePermissions_PageModuleId",
                table: "MenuAndPagePermissions",
                column: "PageModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuAndPagePermissions_RoleId",
                table: "MenuAndPagePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuMasters_ParentMenuId",
                table: "MenuMasters",
                column: "ParentMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_PageMasterModuleActionMappings_PageId_PageModuleId_ActionId",
                table: "PageMasterModuleActionMappings",
                columns: new[] { "PageId", "PageModuleId", "ActionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageMasterModuleActionMappings_PageModuleId",
                table: "PageMasterModuleActionMappings",
                column: "PageModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PageMasterModules_PageId",
                table: "PageMasterModules",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_PageMasters_MenuId",
                table: "PageMasters",
                column: "MenuId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuAndPagePermissions");

            migrationBuilder.DropTable(
                name: "PageMasterModuleActionMappings");

            migrationBuilder.DropTable(
                name: "PageMasterModules");

            migrationBuilder.DropTable(
                name: "PageMasters");

            migrationBuilder.DropTable(
                name: "MenuMasters");
        }
    }
}
