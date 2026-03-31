using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.DbInfrastructure.Migrations;

public partial class AddUnifiedNotificationSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Drop old mail tables (replaced by unified notification system) ─────
        migrationBuilder.Sql("DROP TABLE IF EXISTS [OrgMailTemplates];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [OrgMailConfigs];");

        // ── OrgNotificationConfigs ────────────────────────────────────────────
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrgNotificationConfigs')
            BEGIN
                CREATE TABLE [OrgNotificationConfigs] (
                    [Id]           INT           NOT NULL IDENTITY(1,1),
                    [OrgId]        INT           NOT NULL,
                    [Channel]      INT           NOT NULL,
                    [IsActive]     BIT           NOT NULL DEFAULT 1,

                    -- Email
                    [SmtpHost]     NVARCHAR(200) NULL,
                    [SmtpPort]     INT           NULL,
                    [SmtpUsername] NVARCHAR(200) NULL,
                    [SmtpPassword] NVARCHAR(500) NULL,
                    [FromAddress]  NVARCHAR(200) NULL,
                    [FromName]     NVARCHAR(200) NULL,
                    [EnableSsl]    BIT           NULL,

                    -- SMS
                    [SmsProvider]  INT           NULL,
                    [ApiKey]       NVARCHAR(500) NULL,
                    [AccountSid]   NVARCHAR(200) NULL,
                    [AuthToken]    NVARCHAR(500) NULL,
                    [SenderNumber] NVARCHAR(50)  NULL,
                    [SenderName]   NVARCHAR(200) NULL,

                    -- Push
                    [PushServerKey] NVARCHAR(500) NULL,
                    [PushSenderId]  NVARCHAR(200) NULL,

                    [CreatedAt]  DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedBy]  NVARCHAR(100) NULL,
                    [ModifiedAt] DATETIME2    NULL,
                    [ModifiedBy] NVARCHAR(100) NULL,
                    [IsDeleted]  BIT          NOT NULL DEFAULT 0,
                    [DeletedBy]  NVARCHAR(100) NULL,
                    [IpAddress]  NVARCHAR(50)  NULL,
                    [Location]   NVARCHAR(200) NULL,

                    CONSTRAINT [PK_OrgNotificationConfigs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OrgNotificationConfigs_Organizations_OrgId]
                        FOREIGN KEY ([OrgId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_OrgNotificationConfigs_OrgId_Channel]
                    ON [OrgNotificationConfigs] ([OrgId], [Channel])
                    WHERE [IsDeleted] = 0;
            END
        ");

        // ── NotificationTemplates ─────────────────────────────────────────────
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NotificationTemplates')
            BEGIN
                CREATE TABLE [NotificationTemplates] (
                    [Id]          INT            NOT NULL IDENTITY(1,1),
                    [OrgId]       INT            NULL,
                    [EventType]   INT            NOT NULL,
                    [Channel]     INT            NULL,
                    [Subject]     NVARCHAR(500)  NOT NULL DEFAULT '',
                    [Body]        NVARCHAR(MAX)  NOT NULL DEFAULT '',
                    [IsBodyHtml]  BIT            NOT NULL DEFAULT 1,
                    [ToAddresses] NVARCHAR(1000) NULL,
                    [CcAddresses] NVARCHAR(1000) NULL,
                    [BccAddresses] NVARCHAR(1000) NULL,
                    [IsActive]    BIT            NOT NULL DEFAULT 1,

                    [CreatedAt]  DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedBy]  NVARCHAR(100) NULL,
                    [ModifiedAt] DATETIME2    NULL,
                    [ModifiedBy] NVARCHAR(100) NULL,
                    [IsDeleted]  BIT          NOT NULL DEFAULT 0,
                    [DeletedBy]  NVARCHAR(100) NULL,
                    [IpAddress]  NVARCHAR(50)  NULL,
                    [Location]   NVARCHAR(200) NULL,

                    CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_NotificationTemplates_Organizations_OrgId]
                        FOREIGN KEY ([OrgId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION
                );

                -- Unique per (OrgId, EventType, Channel) — all nullable
                CREATE UNIQUE INDEX [IX_NotificationTemplates_OrgId_EventType_Channel]
                    ON [NotificationTemplates] ([OrgId], [EventType], [Channel])
                    WHERE [IsDeleted] = 0;
            END
        ");

        // ── InAppNotifications ────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InAppNotifications')
            BEGIN
                CREATE TABLE [InAppNotifications] (
                    [Id]        INT           NOT NULL IDENTITY(1,1),
                    [UserId]    INT           NOT NULL,
                    [OrgId]     INT           NULL,
                    [EventType] INT           NOT NULL,
                    [Title]     NVARCHAR(500) NOT NULL DEFAULT '',
                    [Body]      NVARCHAR(MAX) NOT NULL DEFAULT '',
                    [IsRead]    BIT           NOT NULL DEFAULT 0,
                    [ReadAt]    DATETIME2     NULL,

                    [CreatedAt]  DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedBy]  NVARCHAR(100) NULL,
                    [ModifiedAt] DATETIME2    NULL,
                    [ModifiedBy] NVARCHAR(100) NULL,
                    [IsDeleted]  BIT          NOT NULL DEFAULT 0,
                    [DeletedBy]  NVARCHAR(100) NULL,
                    [IpAddress]  NVARCHAR(50)  NULL,
                    [Location]   NVARCHAR(200) NULL,

                    CONSTRAINT [PK_InAppNotifications] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_InAppNotifications_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_InAppNotifications_Organizations_OrgId]
                        FOREIGN KEY ([OrgId]) REFERENCES [Organizations]([Id]) ON DELETE NO ACTION
                );

                CREATE INDEX [IX_InAppNotifications_UserId]
                    ON [InAppNotifications] ([UserId]);

                CREATE INDEX [IX_InAppNotifications_UserId_IsRead]
                    ON [InAppNotifications] ([UserId], [IsRead]);
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS [InAppNotifications];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [NotificationTemplates];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [OrgNotificationConfigs];");
    }
}
