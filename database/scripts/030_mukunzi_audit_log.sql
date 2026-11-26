-- SmartCare HMS
-- 030_mukunzi_audit_log.sql
-- Owner: Mukunzi Jonathan
-- Include in reset_and_seed.sql (Elvin).
-- Contract: dbo.sp_WriteAuditLog, dbo.sp_ListRecentAuditLogs

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        AuditLogId     BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AuditLog PRIMARY KEY,
        Action         NVARCHAR(100)       NOT NULL,
        EntityType     NVARCHAR(100)       NOT NULL,
        EntityId       NVARCHAR(64)        NULL,
        ActorUserId    NVARCHAR(64)        NULL,
        Outcome        NVARCHAR(50)        NOT NULL,
        OccurredAtUtc  DATETIMEOFFSET(7)   NOT NULL,
        CorrelationId  NVARCHAR(64)        NULL
    );

    CREATE INDEX IX_AuditLog_Entity
        ON dbo.AuditLog (EntityType, EntityId, OccurredAtUtc DESC);

    CREATE INDEX IX_AuditLog_Actor
        ON dbo.AuditLog (ActorUserId, OccurredAtUtc DESC);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WriteAuditLog
    @Action        NVARCHAR(100),
    @EntityType    NVARCHAR(100),
    @EntityId      NVARCHAR(64) = NULL,
    @ActorUserId   NVARCHAR(64) = NULL,
    @Outcome       NVARCHAR(50),
    @OccurredAtUtc DATETIMEOFFSET,
    @CorrelationId NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog
    (
        Action,
        EntityType,
        EntityId,
        ActorUserId,
        Outcome,
        OccurredAtUtc,
        CorrelationId
    )
    VALUES
    (
        @Action,
        @EntityType,
        @EntityId,
        @ActorUserId,
        @Outcome,
        @OccurredAtUtc,
        @CorrelationId
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListRecentAuditLogs
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF @Take IS NULL OR @Take < 1 SET @Take = 50;
    IF @Take > 200 SET @Take = 200;

    SELECT TOP (@Take)
        AuditLogId,
        Action,
        EntityType,
        EntityId,
        ActorUserId,
        Outcome,
        OccurredAtUtc,
        CorrelationId
    FROM dbo.AuditLog
    ORDER BY AuditLogId DESC;
END
GO
