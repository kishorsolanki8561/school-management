using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations
{
    public partial class AddSchoolApprovalRequestAndOrgApprovalFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Organizations — add approval + school-code columns (idempotent) ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'SchoolCode')
                    ALTER TABLE [Organizations] ADD [SchoolCode] nvarchar(50) NULL;");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'IsApproved')
                    ALTER TABLE [Organizations] ADD [IsApproved] bit NOT NULL DEFAULT 0;");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'ApprovedAt')
                    ALTER TABLE [Organizations] ADD [ApprovedAt] datetime2 NULL;");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'ApprovedBy')
                    ALTER TABLE [Organizations] ADD [ApprovedBy] nvarchar(200) NULL;");

            // ── 2. UserRoleMappings — add OrgId column if missing ────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'OrgId')
                    ALTER TABLE [UserRoleMappings] ADD [OrgId] int NULL;");

            // ── 3. UserRoleMappings — rebuild indexes (idempotent) ───────────────────
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_UserId_RoleId')
                    DROP INDEX [IX_UserRoleMappings_UserId_RoleId] ON [UserRoleMappings];");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_OrgId')
                    CREATE INDEX [IX_UserRoleMappings_OrgId] ON [UserRoleMappings] ([OrgId]);");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_UserId_RoleId_OrgId')
                    CREATE UNIQUE INDEX [IX_UserRoleMappings_UserId_RoleId_OrgId]
                    ON [UserRoleMappings] ([UserId], [RoleId], [OrgId])
                    WHERE [OrgId] IS NOT NULL;");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_UserId_RoleId_NoOrg')
                    CREATE UNIQUE INDEX [IX_UserRoleMappings_UserId_RoleId_NoOrg]
                    ON [UserRoleMappings] ([UserId], [RoleId])
                    WHERE [OrgId] IS NULL AND [IsDeleted] = 0;");

            // ── 4. UserRoleMappings — add FK to Organizations (idempotent) ───────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRoleMappings_Organizations_OrgId')
                    ALTER TABLE [UserRoleMappings] ADD CONSTRAINT [FK_UserRoleMappings_Organizations_OrgId]
                    FOREIGN KEY ([OrgId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;");

            // ── 5. SchoolApprovalRequests table ──────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SchoolApprovalRequests')
                BEGIN
                    CREATE TABLE [SchoolApprovalRequests] (
                        [Id]                int IDENTITY(1,1) NOT NULL,
                        [OrgId]             int NOT NULL,
                        [RequestedByUserId] int NOT NULL,
                        [Status]            int NOT NULL,
                        [RejectionReason]   nvarchar(1000) NULL,
                        [ReviewedByUserId]  int NULL,
                        [ReviewedAt]        datetime2 NULL,
                        [CreatedAt]         datetime2 NOT NULL,
                        [CreatedBy]         nvarchar(max) NOT NULL,
                        [ModifiedAt]        datetime2 NULL,
                        [ModifiedBy]        nvarchar(max) NULL,
                        [IsDeleted]         bit NOT NULL,
                        [DeletedBy]         nvarchar(max) NULL,
                        [IpAddress]         nvarchar(max) NULL,
                        [Location]          nvarchar(max) NULL,
                        CONSTRAINT [PK_SchoolApprovalRequests] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SchoolApprovalRequests_Organizations_OrgId]
                            FOREIGN KEY ([OrgId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_SchoolApprovalRequests_Users_RequestedByUserId]
                            FOREIGN KEY ([RequestedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_SchoolApprovalRequests_Users_ReviewedByUserId]
                            FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_SchoolApprovalRequests_OrgId]
                        ON [SchoolApprovalRequests] ([OrgId]);
                    CREATE INDEX [IX_SchoolApprovalRequests_RequestedByUserId]
                        ON [SchoolApprovalRequests] ([RequestedByUserId]);
                    CREATE INDEX [IX_SchoolApprovalRequests_ReviewedByUserId]
                        ON [SchoolApprovalRequests] ([ReviewedByUserId]);
                END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SchoolApprovalRequests')
                    DROP TABLE [SchoolApprovalRequests];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRoleMappings_Organizations_OrgId')
                    ALTER TABLE [UserRoleMappings] DROP CONSTRAINT [FK_UserRoleMappings_Organizations_OrgId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_UserId_RoleId_OrgId')
                    DROP INDEX [IX_UserRoleMappings_UserId_RoleId_OrgId] ON [UserRoleMappings];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_UserId_RoleId_NoOrg')
                    DROP INDEX [IX_UserRoleMappings_UserId_RoleId_NoOrg] ON [UserRoleMappings];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_OrgId')
                    DROP INDEX [IX_UserRoleMappings_OrgId] ON [UserRoleMappings];");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'IX_UserRoleMappings_UserId_RoleId')
                    CREATE UNIQUE INDEX [IX_UserRoleMappings_UserId_RoleId]
                    ON [UserRoleMappings] ([UserId], [RoleId]);");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('UserRoleMappings') AND name = 'OrgId')
                    ALTER TABLE [UserRoleMappings] DROP COLUMN [OrgId];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'SchoolCode')
                    ALTER TABLE [Organizations] DROP COLUMN [SchoolCode];");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'IsApproved')
                    ALTER TABLE [Organizations] DROP COLUMN [IsApproved];");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'ApprovedAt')
                    ALTER TABLE [Organizations] DROP COLUMN [ApprovedAt];");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'ApprovedBy')
                    ALTER TABLE [Organizations] DROP COLUMN [ApprovedBy];");
        }
    }
}
