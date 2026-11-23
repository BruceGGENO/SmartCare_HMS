-- SmartCare HMS
-- 020_mukunzi_document_storage.sql
-- Owner: Mukunzi Jonathan
-- Include this file in reset_and_seed.sql (Elvin).
-- Contract used by SmartCare.Infrastructure.FileStorage.LocalFileStorage:
--   dbo.sp_InsertDocument
--   dbo.sp_GetDocumentById
-- Files live on disk under App_Data/uploads (GUID names), not in this table.
-- When the Patient table exists, add a FK from EntityId where EntityType = 'Patient'.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Document', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Document
    (
        DocumentId        UNIQUEIDENTIFIER  NOT NULL
            CONSTRAINT PK_Document PRIMARY KEY,
        OriginalFileName  NVARCHAR(255)     NOT NULL,
        ContentType       NVARCHAR(100)     NOT NULL,
        SizeBytes         INT               NOT NULL,
        StoredFileName    NVARCHAR(260)     NOT NULL,
        EntityType        NVARCHAR(100)     NULL,
        EntityId          NVARCHAR(64)      NULL,
        UploadedByUserId  NVARCHAR(64)      NULL,
        UploadedAtUtc     DATETIMEOFFSET(7) NOT NULL
    );

    CREATE INDEX IX_Document_Entity
        ON dbo.Document (EntityType, EntityId, UploadedAtUtc DESC);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InsertDocument
    @DocumentId       UNIQUEIDENTIFIER,
    @OriginalFileName NVARCHAR(255),
    @ContentType      NVARCHAR(100),
    @SizeBytes        INT,
    @StoredFileName   NVARCHAR(260),
    @EntityType       NVARCHAR(100) = NULL,
    @EntityId         NVARCHAR(64) = NULL,
    @UploadedByUserId NVARCHAR(64) = NULL,
    @UploadedAtUtc    DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Document
    (
        DocumentId,
        OriginalFileName,
        ContentType,
        SizeBytes,
        StoredFileName,
        EntityType,
        EntityId,
        UploadedByUserId,
        UploadedAtUtc
    )
    VALUES
    (
        @DocumentId,
        @OriginalFileName,
        @ContentType,
        @SizeBytes,
        @StoredFileName,
        @EntityType,
        @EntityId,
        @UploadedByUserId,
        @UploadedAtUtc
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDocumentById
    @DocumentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DocumentId,
        OriginalFileName,
        ContentType,
        SizeBytes,
        StoredFileName,
        EntityType,
        EntityId,
        UploadedByUserId,
        UploadedAtUtc
    FROM dbo.Document
    WHERE DocumentId = @DocumentId;
END
GO
