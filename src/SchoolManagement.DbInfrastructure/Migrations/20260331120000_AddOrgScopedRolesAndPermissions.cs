using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddOrgScopedRolesAndPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Roles — add OrgId + SystemRoleId columns ──────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Roles') AND name = 'OrgId')
                    ALTER TABLE [Roles] ADD [OrgId] int NULL;");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Roles') AND name = 'SystemRoleId')
                    ALTER TABLE [Roles] ADD [SystemRoleId] int NULL;");

            // ── 2. Roles — swap unique index from (Name) to (Name, OrgId) ────────────
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Roles') AND name = 'IX_Roles_Name')
                    DROP INDEX [IX_Roles_Name] ON [Roles];");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Roles') AND name = 'IX_Roles_Name_OrgId')
                    CREATE UNIQUE INDEX [IX_Roles_Name_OrgId]
                    ON [Roles] ([Name], [OrgId])
                    WHERE [OrgId] IS NOT NULL;");

            // Separate unique index for system roles (OrgId IS NULL)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Roles') AND name = 'IX_Roles_Name_System')
                    CREATE UNIQUE INDEX [IX_Roles_Name_System]
                    ON [Roles] ([Name])
                    WHERE [OrgId] IS NULL;");

            // ── 3. Roles — add FKs ────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Roles_Organizations_OrgId')
                    ALTER TABLE [Roles] ADD CONSTRAINT [FK_Roles_Organizations_OrgId]
                    FOREIGN KEY ([OrgId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Roles_Roles_SystemRoleId')
                    ALTER TABLE [Roles] ADD CONSTRAINT [FK_Roles_Roles_SystemRoleId]
                    FOREIGN KEY ([SystemRoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION;");

            // ── 4. MenuAndPagePermissions — add OrgId column ─────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'OrgId')
                    ALTER TABLE [MenuAndPagePermissions] ADD [OrgId] int NULL;");

            // ── 5. MenuAndPagePermissions — swap unique index to include OrgId ────────
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId')
                    DROP INDEX [IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId] ON [MenuAndPagePermissions];");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_OrgId')
                    CREATE UNIQUE INDEX [IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_OrgId]
                    ON [MenuAndPagePermissions] ([MenuId], [PageId], [PageModuleId], [ActionId], [RoleId], [OrgId])
                    WHERE [OrgId] IS NOT NULL;");

            // Separate unique index for system-level permissions (OrgId IS NULL)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_System')
                    CREATE UNIQUE INDEX [IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_System]
                    ON [MenuAndPagePermissions] ([MenuId], [PageId], [PageModuleId], [ActionId], [RoleId])
                    WHERE [OrgId] IS NULL;");

            // ── 6. MenuAndPagePermissions — add FK for OrgId ─────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuAndPagePermissions_Organizations_OrgId')
                    ALTER TABLE [MenuAndPagePermissions] ADD CONSTRAINT [FK_MenuAndPagePermissions_Organizations_OrgId]
                    FOREIGN KEY ([OrgId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuAndPagePermissions_Organizations_OrgId')
                    ALTER TABLE [MenuAndPagePermissions] DROP CONSTRAINT [FK_MenuAndPagePermissions_Organizations_OrgId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_OrgId')
                    DROP INDEX [IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_OrgId] ON [MenuAndPagePermissions];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_System')
                    DROP INDEX [IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId_System] ON [MenuAndPagePermissions];");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId')
                    CREATE UNIQUE INDEX [IX_MenuAndPagePermissions_MenuId_PageId_PageModuleId_ActionId_RoleId]
                    ON [MenuAndPagePermissions] ([MenuId], [PageId], [PageModuleId], [ActionId], [RoleId]);");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuAndPagePermissions') AND name = 'OrgId')
                    ALTER TABLE [MenuAndPagePermissions] DROP COLUMN [OrgId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Roles_Roles_SystemRoleId')
                    ALTER TABLE [Roles] DROP CONSTRAINT [FK_Roles_Roles_SystemRoleId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Roles_Organizations_OrgId')
                    ALTER TABLE [Roles] DROP CONSTRAINT [FK_Roles_Organizations_OrgId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Roles') AND name = 'IX_Roles_Name_OrgId')
                    DROP INDEX [IX_Roles_Name_OrgId] ON [Roles];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Roles') AND name = 'IX_Roles_Name_System')
                    DROP INDEX [IX_Roles_Name_System] ON [Roles];");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Roles') AND name = 'IX_Roles_Name')
                    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Roles') AND name = 'SystemRoleId')
                    ALTER TABLE [Roles] DROP COLUMN [SystemRoleId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Roles') AND name = 'OrgId')
                    ALTER TABLE [Roles] DROP COLUMN [OrgId];");
        }
    }
}
