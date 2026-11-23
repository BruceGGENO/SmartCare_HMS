-- Script: db/scripts/003_arthur_revenue_accounting.sql
-- Owner: Cyitatire Uwonizeye Arthur (Member #3)
-- Module: MOD-12 Revenue vs. Usage Accounting
-- Purpose: Tables, Stored Procedures, and Demo Seed Data for Prepaid Packages and Usage Recognition

USE SmartCareHMS_3;
GO

-- 1. Create Tables
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Packages')
BEGIN
    CREATE TABLE dbo.Packages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PatientId INT NOT NULL,
        PatientName NVARCHAR(100) NOT NULL,
        PatientMrn NVARCHAR(50) NOT NULL,
        PackageName NVARCHAR(100) NOT NULL,
        TotalSessions INT NOT NULL,
        UsedSessions INT NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL,
        PurchasedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PackageUsages')
BEGIN
    CREATE TABLE dbo.PackageUsages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PackageId INT NOT NULL FOREIGN KEY REFERENCES dbo.Packages(Id),
        SessionNumber INT NOT NULL,
        RecognizedAmount DECIMAL(18,2) NOT NULL,
        Notes NVARCHAR(200) NULL,
        ConsumedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 2. Stored Procedures

-- A) Summary Reconciliation KPI
CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueSummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ISNULL(SUM(TotalAmount), 0.00) AS TotalCashCollected,
        ISNULL(SUM(CASE WHEN TotalSessions > 0 THEN (TotalAmount / TotalSessions) * UsedSessions ELSE 0 END), 0.00) AS TotalEarnedRevenue,
        ISNULL(SUM(CASE WHEN TotalSessions > 0 THEN TotalAmount - ((TotalAmount / TotalSessions) * UsedSessions) ELSE 0 END), 0.00) AS TotalDeferredRevenue,
        COUNT(Id) AS TotalPackagesCount,
        SUM(CASE WHEN UsedSessions < TotalSessions THEN 1 ELSE 0 END) AS ActivePackagesCount,
        ISNULL(SUM(TotalSessions), 0) AS TotalSessionsSold,
        ISNULL(SUM(UsedSessions), 0) AS TotalSessionsConsumed
    FROM dbo.Packages;
END;
GO

-- B) Get All Packages
CREATE OR ALTER PROCEDURE dbo.sp_GetAllPackages
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        Id,
        PatientId,
        PatientName,
        PatientMrn,
        PackageName,
        TotalSessions,
        UsedSessions,
        TotalAmount,
        PurchasedAt
    FROM dbo.Packages
    ORDER BY Id DESC;
END;
GO

-- C) Get Recent Usages
CREATE OR ALTER PROCEDURE dbo.sp_GetRecentPackageUsages
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 50
        u.Id,
        u.PackageId,
        p.PatientName,
        p.PackageName,
        u.SessionNumber,
        u.RecognizedAmount,
        u.Notes,
        u.ConsumedAt
    FROM dbo.PackageUsages u
    INNER JOIN dbo.Packages p ON u.PackageId = p.Id
    ORDER BY u.ConsumedAt DESC;
END;
GO

-- D) Purchase Package
CREATE OR ALTER PROCEDURE dbo.sp_PurchasePackage
    @PatientId INT,
    @PatientName NVARCHAR(100),
    @PatientMrn NVARCHAR(50),
    @PackageName NVARCHAR(100),
    @TotalSessions INT,
    @TotalAmount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Packages (PatientId, PatientName, PatientMrn, PackageName, TotalSessions, UsedSessions, TotalAmount, PurchasedAt)
    VALUES (@PatientId, @PatientName, @PatientMrn, @PackageName, @TotalSessions, 0, @TotalAmount, SYSUTCDATETIME());

    SELECT SCOPE_IDENTITY() AS NewPackageId;
END;
GO

-- E) Consume 1 Session
CREATE OR ALTER PROCEDURE dbo.sp_ConsumePackageSession
    @PackageId INT,
    @Notes NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalSessions INT;
    DECLARE @UsedSessions INT;
    DECLARE @TotalAmount DECIMAL(18,2);
    DECLARE @PerSessionAmount DECIMAL(18,2);
    DECLARE @NextSession INT;

    SELECT 
        @TotalSessions = TotalSessions,
        @UsedSessions = UsedSessions,
        @TotalAmount = TotalAmount
    FROM dbo.Packages
    WHERE Id = @PackageId;

    IF @TotalSessions IS NULL OR @UsedSessions >= @TotalSessions
    BEGIN
        SELECT 0 AS Success, 'No remaining sessions available in this package.' AS Message;
        RETURN;
    END;

    SET @NextSession = @UsedSessions + 1;
    SET @PerSessionAmount = ROUND(@TotalAmount / @TotalSessions, 2);

    -- Increment usage
    UPDATE dbo.Packages
    SET UsedSessions = @NextSession
    WHERE Id = @PackageId;

    -- Record usage ledger
    INSERT INTO dbo.PackageUsages (PackageId, SessionNumber, RecognizedAmount, Notes, ConsumedAt)
    VALUES (@PackageId, @NextSession, @PerSessionAmount, ISNULL(@Notes, 'Session ' + CAST(@NextSession AS NVARCHAR(10)) + ' attended'), SYSUTCDATETIME());

    SELECT 1 AS Success, 'Session consumed successfully.' AS Message;
END;
GO

-- 3. Demo Seed Data (Populate if empty)
IF NOT EXISTS (SELECT * FROM dbo.Packages)
BEGIN
    INSERT INTO dbo.Packages (PatientId, PatientName, PatientMrn, PackageName, TotalSessions, UsedSessions, TotalAmount, PurchasedAt)
    VALUES
    (1, 'John Doe', 'MRN-1001', 'Physiotherapy Rehabilitation (5 Sessions)', 5, 2, 250.00, DATEADD(DAY, -10, SYSUTCDATETIME())),
    (2, 'Jane Smith', 'MRN-1002', 'Dialysis Treatment Plan (10 Sessions)', 10, 6, 800.00, DATEADD(DAY, -20, SYSUTCDATETIME())),
    (3, 'Robert Johnson', 'MRN-1003', 'Antenatal Wellness Care (4 Sessions)', 4, 4, 200.00, DATEADD(DAY, -30, SYSUTCDATETIME())),
    (4, 'Emily Davis', 'MRN-1004', 'Post-Op Wound Dressing (6 Sessions)', 6, 1, 180.00, DATEADD(DAY, -5, SYSUTCDATETIME())),
    (5, 'Michael Wilson', 'MRN-1005', 'Cardiac Rehabilitation Bundle (8 Sessions)', 8, 3, 400.00, DATEADD(DAY, -14, SYSUTCDATETIME()));

    -- Seed usages corresponding to used sessions
    INSERT INTO dbo.PackageUsages (PackageId, SessionNumber, RecognizedAmount, Notes, ConsumedAt)
    VALUES
    (1, 1, 50.00, 'Initial physiotherapy assessment and exercises', DATEADD(DAY, -8, SYSUTCDATETIME())),
    (1, 2, 50.00, 'Resistance training and ultrasound therapy', DATEADD(DAY, -3, SYSUTCDATETIME())),
    (2, 1, 80.00, 'Dialysis session 1', DATEADD(DAY, -18, SYSUTCDATETIME())),
    (2, 2, 80.00, 'Dialysis session 2', DATEADD(DAY, -15, SYSUTCDATETIME())),
    (2, 3, 80.00, 'Dialysis session 3', DATEADD(DAY, -11, SYSUTCDATETIME())),
    (2, 4, 80.00, 'Dialysis session 4', DATEADD(DAY, -8, SYSUTCDATETIME())),
    (2, 5, 80.00, 'Dialysis session 5', DATEADD(DAY, -5, SYSUTCDATETIME())),
    (2, 6, 80.00, 'Dialysis session 6', DATEADD(DAY, -2, SYSUTCDATETIME())),
    (3, 1, 50.00, 'Trimester 1 ultrasound and vitals', DATEADD(DAY, -25, SYSUTCDATETIME())),
    (3, 2, 50.00, 'Routine prenatal checkup', DATEADD(DAY, -18, SYSUTCDATETIME())),
    (3, 3, 50.00, 'Glucose tolerance and bloodwork', DATEADD(DAY, -10, SYSUTCDATETIME())),
    (3, 4, 50.00, 'Final third-trimester clearance', DATEADD(DAY, -2, SYSUTCDATETIME())),
    (4, 1, 30.00, 'Surgical incision cleaning and dressing change', DATEADD(DAY, -3, SYSUTCDATETIME())),
    (5, 1, 50.00, 'Monitored treadmill exercise session', DATEADD(DAY, -12, SYSUTCDATETIME())),
    (5, 2, 50.00, 'Cardio endurance and blood pressure profiling', DATEADD(DAY, -8, SYSUTCDATETIME())),
    (5, 3, 50.00, 'ECG telemetry and cool-down routine', DATEADD(DAY, -1, SYSUTCDATETIME()));
END;
GO
