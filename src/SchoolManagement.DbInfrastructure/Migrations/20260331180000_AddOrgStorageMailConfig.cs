using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations;

public partial class AddOrgStorageMailConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── OrgStorageConfigs ─────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrgStorageConfigs')
            BEGIN
                CREATE TABLE [OrgStorageConfigs] (
                    [Id]               INT            NOT NULL IDENTITY(1,1),
                    [OrgId]            INT            NOT NULL,
                    [StorageType]      INT            NOT NULL,
                    [IsActive]         BIT            NOT NULL DEFAULT 1,
                    [BasePath]         NVARCHAR(500)  NULL,
                    [BucketName]       NVARCHAR(200)  NULL,
                    [Region]           NVARCHAR(100)  NULL,
                    [AccessKey]        NVARCHAR(200)  NULL,
                    [SecretKey]        NVARCHAR(500)  NULL,
                    [ContainerName]    NVARCHAR(200)  NULL,
                    [ConnectionString] NVARCHAR(1000) NULL,
                    [CreatedAt]        DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedBy]        NVARCHAR(100)  NULL,
                    [ModifiedAt]       DATETIME2      NULL,
                    [ModifiedBy]       NVARCHAR(100)  NULL,
                    [IsDeleted]        BIT            NOT NULL DEFAULT 0,
                    [DeletedBy]        NVARCHAR(100)  NULL,
                    [IpAddress]        NVARCHAR(50)   NULL,
                    [Location]         NVARCHAR(200)  NULL,
                    CONSTRAINT [PK_OrgStorageConfigs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OrgStorageConfigs_Organizations_OrgId]
                        FOREIGN KEY ([OrgId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_OrgStorageConfigs_OrgId]
                    ON [OrgStorageConfigs] ([OrgId])
                    WHERE [IsDeleted] = 0;
            END
        ");

        // ── OrgMailConfigs ────────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrgMailConfigs')
            BEGIN
                CREATE TABLE [OrgMailConfigs] (
                    [Id]          INT            NOT NULL IDENTITY(1,1),
                    [OrgId]       INT            NOT NULL,
                    [SmtpHost]    NVARCHAR(200)  NOT NULL DEFAULT '',
                    [SmtpPort]    INT            NOT NULL DEFAULT 587,
                    [Username]    NVARCHAR(200)  NOT NULL DEFAULT '',
                    [Password]    NVARCHAR(500)  NOT NULL DEFAULT '',
                    [FromAddress] NVARCHAR(200)  NOT NULL DEFAULT '',
                    [FromName]    NVARCHAR(200)  NULL,
                    [EnableSsl]   BIT            NOT NULL DEFAULT 1,
                    [IsActive]    BIT            NOT NULL DEFAULT 1,
                    [CreatedAt]   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedBy]   NVARCHAR(100)  NULL,
                    [ModifiedAt]  DATETIME2      NULL,
                    [ModifiedBy]  NVARCHAR(100)  NULL,
                    [IsDeleted]   BIT            NOT NULL DEFAULT 0,
                    [DeletedBy]   NVARCHAR(100)  NULL,
                    [IpAddress]   NVARCHAR(50)   NULL,
                    [Location]    NVARCHAR(200)  NULL,
                    CONSTRAINT [PK_OrgMailConfigs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OrgMailConfigs_Organizations_OrgId]
                        FOREIGN KEY ([OrgId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_OrgMailConfigs_OrgId]
                    ON [OrgMailConfigs] ([OrgId])
                    WHERE [IsDeleted] = 0;
            END
        ");

        // ── OrgMailTemplates ──────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrgMailTemplates')
            BEGIN
                CREATE TABLE [OrgMailTemplates] (
                    [Id]           INT             NOT NULL IDENTITY(1,1),
                    [OrgId]        INT             NULL,
                    [EventType]    INT             NOT NULL,
                    [Subject]      NVARCHAR(500)   NOT NULL DEFAULT '',
                    [BodyHtml]     NVARCHAR(MAX)   NOT NULL DEFAULT '',
                    [ToAddresses]  NVARCHAR(1000)  NULL,
                    [CcAddresses]  NVARCHAR(1000)  NULL,
                    [BccAddresses] NVARCHAR(1000)  NULL,
                    [IsActive]     BIT             NOT NULL DEFAULT 1,
                    [CreatedAt]    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedBy]    NVARCHAR(100)   NULL,
                    [ModifiedAt]   DATETIME2       NULL,
                    [ModifiedBy]   NVARCHAR(100)   NULL,
                    [IsDeleted]    BIT             NOT NULL DEFAULT 0,
                    [DeletedBy]    NVARCHAR(100)   NULL,
                    [IpAddress]    NVARCHAR(50)    NULL,
                    [Location]     NVARCHAR(200)   NULL,
                    CONSTRAINT [PK_OrgMailTemplates] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OrgMailTemplates_Organizations_OrgId]
                        FOREIGN KEY ([OrgId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION
                );

                -- Unique per (OrgId, EventType); OrgId NULL = global default
                CREATE UNIQUE INDEX [IX_OrgMailTemplates_OrgId_EventType]
                    ON [OrgMailTemplates] ([OrgId], [EventType])
                    WHERE [IsDeleted] = 0;
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS [OrgMailTemplates];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [OrgMailConfigs];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [OrgStorageConfigs];");
    }
}
