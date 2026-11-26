-- SmartCare HMS
-- 055_mukunzi_revenue.sql
-- Owner: Mukunzi Jonathan (Web API port of Arthur MOD-12)
-- Prepaid packages + usage recognition (cash vs earned vs deferred).
-- Separate from dbo.TreatmentPackage (portal session balances in 020).
-- Requires 010, 020, 025. Include in reset_and_seed.sql (Elvin).

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.PrepaidPackage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrepaidPackage
    (
        PackageId      INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PrepaidPackage PRIMARY KEY,
        PatientId      INT               NOT NULL,
        PatientName    NVARCHAR(200)     NOT NULL,
        PatientMrn     NVARCHAR(20)      NOT NULL,
        PackageName    NVARCHAR(100)     NOT NULL,
        TotalSessions  INT               NOT NULL,
        UsedSessions   INT               NOT NULL
            CONSTRAINT DF_PrepaidPackage_Used DEFAULT (0),
        TotalAmount    DECIMAL(18,2)     NOT NULL,
        PurchasedAtUtc DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_PrepaidPackage_Purchased DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_PrepaidPackage_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId),
        CONSTRAINT CK_PrepaidPackage_Sessions CHECK (TotalSessions > 0 AND UsedSessions >= 0 AND UsedSessions <= TotalSessions),
        CONSTRAINT CK_PrepaidPackage_Amount CHECK (TotalAmount >= 0)
    );

    CREATE INDEX IX_PrepaidPackage_Patient ON dbo.PrepaidPackage (PatientId);
END
GO

IF OBJECT_ID(N'dbo.PackageUsage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageUsage
    (
        UsageId           INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PackageUsage PRIMARY KEY,
        PackageId         INT               NOT NULL,
        SessionNumber     INT               NOT NULL,
        RecognizedAmount  DECIMAL(18,2)     NOT NULL,
        Notes             NVARCHAR(200)     NULL,
        ConsumedAtUtc     DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_PackageUsage_Consumed DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_PackageUsage_Package FOREIGN KEY (PackageId) REFERENCES dbo.PrepaidPackage (PackageId)
    );

    CREATE INDEX IX_PackageUsage_Package ON dbo.PackageUsage (PackageId);
END
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'revenue.read', N'View revenue summary, packages, and usage'),
    (N'revenue.purchase', N'Purchase prepaid packages'),
    (N'revenue.consume', N'Consume a prepaid package session')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'revenue.read'),
(N'Super Admin', N'revenue.purchase'),
(N'Super Admin', N'revenue.consume'),
(N'Hospital Admin', N'revenue.read'),
(N'Hospital Admin', N'revenue.purchase'),
(N'Hospital Admin', N'revenue.consume'),
(N'Billing Staff', N'revenue.read'),
(N'Billing Staff', N'revenue.purchase'),
(N'Billing Staff', N'revenue.consume'),
(N'Receptionist', N'revenue.read'),
(N'Receptionist', N'revenue.purchase'),
(N'Doctor', N'revenue.read'),
(N'Doctor', N'revenue.consume'),
(N'Nurse', N'revenue.read'),
(N'Nurse', N'revenue.consume');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode
FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueSummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(SUM(TotalAmount), 0.00) AS TotalCashCollected,
        ISNULL(SUM(CASE WHEN TotalSessions > 0
            THEN (TotalAmount / TotalSessions) * UsedSessions ELSE 0 END), 0.00) AS TotalEarnedRevenue,
        ISNULL(SUM(CASE WHEN TotalSessions > 0
            THEN TotalAmount - ((TotalAmount / TotalSessions) * UsedSessions) ELSE 0 END), 0.00) AS TotalDeferredRevenue,
        COUNT(*) AS TotalPackagesCount,
        SUM(CASE WHEN UsedSessions < TotalSessions THEN 1 ELSE 0 END) AS ActivePackagesCount,
        ISNULL(SUM(TotalSessions), 0) AS TotalSessionsSold,
        ISNULL(SUM(UsedSessions), 0) AS TotalSessionsConsumed
    FROM dbo.PrepaidPackage;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListPrepaidPackages
    @PatientId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PackageId,
        PatientId,
        PatientName,
        PatientMrn,
        PackageName,
        TotalSessions,
        UsedSessions,
        TotalAmount,
        PurchasedAtUtc
    FROM dbo.PrepaidPackage
    WHERE @PatientId IS NULL OR PatientId = @PatientId
    ORDER BY PackageId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPrepaidPackageById
    @PackageId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PackageId,
        PatientId,
        PatientName,
        PatientMrn,
        PackageName,
        TotalSessions,
        UsedSessions,
        TotalAmount,
        PurchasedAtUtc
    FROM dbo.PrepaidPackage
    WHERE PackageId = @PackageId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListRecentPackageUsages
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (50)
        u.UsageId,
        u.PackageId,
        p.PatientName,
        p.PackageName,
        u.SessionNumber,
        u.RecognizedAmount,
        u.Notes,
        u.ConsumedAtUtc
    FROM dbo.PackageUsage u
    INNER JOIN dbo.PrepaidPackage p ON p.PackageId = u.PackageId
    ORDER BY u.ConsumedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PurchasePrepaidPackage
    @PatientId     INT,
    @PackageName   NVARCHAR(100),
    @TotalSessions INT,
    @TotalAmount   DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PatientName NVARCHAR(200);
    DECLARE @PatientMrn NVARCHAR(20);

    SELECT @PatientName = FullName, @PatientMrn = Mrn
    FROM dbo.Patient
    WHERE PatientId = @PatientId;

    IF @PatientName IS NULL
        THROW 50011, N'Patient not found.', 1;

    IF @TotalSessions <= 0
        THROW 50012, N'Total sessions must be greater than zero.', 1;

    IF @TotalAmount < 0
        THROW 50013, N'Total amount cannot be negative.', 1;

    INSERT INTO dbo.PrepaidPackage
        (PatientId, PatientName, PatientMrn, PackageName, TotalSessions, UsedSessions, TotalAmount)
    VALUES
        (@PatientId, @PatientName, @PatientMrn, @PackageName, @TotalSessions, 0, @TotalAmount);

    SELECT
        PackageId,
        PatientId,
        PatientName,
        PatientMrn,
        PackageName,
        TotalSessions,
        UsedSessions,
        TotalAmount,
        PurchasedAtUtc
    FROM dbo.PrepaidPackage
    WHERE PackageId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsumePrepaidPackageSession
    @PackageId INT,
    @Notes     NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TotalSessions INT;
    DECLARE @UsedSessions INT;
    DECLARE @TotalAmount DECIMAL(18,2);
    DECLARE @PerSessionAmount DECIMAL(18,2);
    DECLARE @NextSession INT;

    SELECT
        @TotalSessions = TotalSessions,
        @UsedSessions = UsedSessions,
        @TotalAmount = TotalAmount
    FROM dbo.PrepaidPackage
    WHERE PackageId = @PackageId;

    IF @TotalSessions IS NULL
        THROW 50014, N'Package not found.', 1;

    IF @UsedSessions >= @TotalSessions
        THROW 50015, N'No remaining sessions available in this package.', 1;

    SET @NextSession = @UsedSessions + 1;
    SET @PerSessionAmount = ROUND(@TotalAmount / @TotalSessions, 2);

    BEGIN TRANSACTION;

    UPDATE dbo.PrepaidPackage
    SET UsedSessions = @NextSession
    WHERE PackageId = @PackageId;

    INSERT INTO dbo.PackageUsage (PackageId, SessionNumber, RecognizedAmount, Notes)
    VALUES (
        @PackageId,
        @NextSession,
        @PerSessionAmount,
        ISNULL(@Notes, N'Session ' + CAST(@NextSession AS NVARCHAR(10)) + N' attended')
    );

    DECLARE @UsageId INT = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT
        u.UsageId,
        u.PackageId,
        p.PatientName,
        p.PackageName,
        u.SessionNumber,
        u.RecognizedAmount,
        u.Notes,
        u.ConsumedAtUtc
    FROM dbo.PackageUsage u
    INNER JOIN dbo.PrepaidPackage p ON p.PackageId = u.PackageId
    WHERE u.UsageId = @UsageId;
END
GO
