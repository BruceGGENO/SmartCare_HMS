-- SmartCare HMS
-- 050_arthur_billing_and_revenue.sql
-- Owner: Cyitatire Uwonizeye Arthur (MOD-04 Billing & MOD-12 Revenue vs Usage)
-- Compatible with Elvin's reset_and_seed.sql

SET NOCOUNT ON;
GO

-- 1. Create Invoices and Payments tables
IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PatientId INT NOT NULL,
        PatientName NVARCHAR(100) NOT NULL,
        PatientMrn NVARCHAR(50) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.InvoiceLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NOT NULL FOREIGN KEY REFERENCES dbo.Invoices(Id),
        Description NVARCHAR(200) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Quantity INT NOT NULL DEFAULT 1,
        LineTotal AS (UnitPrice * Quantity) PERSISTED
    );
END
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NOT NULL FOREIGN KEY REFERENCES dbo.Invoices(Id),
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        TransactionReference NVARCHAR(100) NOT NULL,
        PaidAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 2. Create Packages and Usages (MOD-12)
IF OBJECT_ID(N'dbo.Packages', N'U') IS NULL
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
END
GO

IF OBJECT_ID(N'dbo.PackageUsages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageUsages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PackageId INT NOT NULL FOREIGN KEY REFERENCES dbo.Packages(Id),
        SessionNumber INT NOT NULL,
        RecognizedAmount DECIMAL(18,2) NOT NULL,
        Notes NVARCHAR(200) NULL,
        ConsumedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 3. Stored Procedures
CREATE OR ALTER PROCEDURE dbo.sp_GetPendingInvoices
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        Id,
        PatientId,
        PatientName,
        PatientMrn,
        TotalAmount,
        PaidAmount,
        Status,
        CreatedAt
    FROM dbo.Invoices
    WHERE Status = 'Pending' OR PaidAmount < TotalAmount
    ORDER BY CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetInvoiceById
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        Id,
        PatientId,
        PatientName,
        PatientMrn,
        TotalAmount,
        PaidAmount,
        Status,
        CreatedAt
    FROM dbo.Invoices
    WHERE Id = @InvoiceId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetInvoiceLines
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        Id,
        InvoiceId,
        Description,
        UnitPrice,
        Quantity,
        LineTotal
    FROM dbo.InvoiceLines
    WHERE InvoiceId = @InvoiceId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_RecordPayment
    @InvoiceId INT,
    @Amount DECIMAL(18,2),
    @PaymentMethod NVARCHAR(50),
    @TransactionReference NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        INSERT INTO dbo.Payments (InvoiceId, Amount, PaymentMethod, TransactionReference)
        VALUES (@InvoiceId, @Amount, @PaymentMethod, @TransactionReference);

        UPDATE dbo.Invoices
        SET 
            PaidAmount = PaidAmount + @Amount,
            Status = CASE 
                WHEN (PaidAmount + @Amount) >= TotalAmount THEN 'Paid'
                ELSE 'Partial'
            END
        WHERE Id = @InvoiceId;

        COMMIT TRANSACTION;
        SELECT 1;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueSummary
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalCashCollected DECIMAL(18,2);
    DECLARE @TotalEarnedRevenue DECIMAL(18,2);
    DECLARE @TotalDeferredRevenue DECIMAL(18,2);
    DECLARE @TotalSessionsSold INT;
    DECLARE @TotalSessionsConsumed INT;

    SELECT 
        @TotalCashCollected = ISNULL(SUM(TotalAmount), 0),
        @TotalSessionsSold = ISNULL(SUM(TotalSessions), 0)
    FROM dbo.Packages;

    SELECT 
        @TotalEarnedRevenue = ISNULL(SUM(RecognizedAmount), 0),
        @TotalSessionsConsumed = COUNT(Id)
    FROM dbo.PackageUsages;

    SET @TotalDeferredRevenue = @TotalCashCollected - @TotalEarnedRevenue;

    SELECT 
        @TotalCashCollected AS TotalCashCollected,
        @TotalEarnedRevenue AS TotalEarnedRevenue,
        @TotalDeferredRevenue AS TotalDeferredRevenue,
        @TotalSessionsSold AS TotalSessionsSold,
        @TotalSessionsConsumed AS TotalSessionsConsumed;
END;
GO

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

    UPDATE dbo.Packages
    SET UsedSessions = @NextSession
    WHERE Id = @PackageId;

    INSERT INTO dbo.PackageUsages (PackageId, SessionNumber, RecognizedAmount, Notes, ConsumedAt)
    VALUES (@PackageId, @NextSession, @PerSessionAmount, ISNULL(@Notes, 'Session ' + CAST(@NextSession AS NVARCHAR(10)) + ' attended'), SYSUTCDATETIME());

    SELECT 1 AS Success, 'Session consumed successfully.' AS Message;
END;
GO

-- 4. Sample Seed Data
IF NOT EXISTS (SELECT 1 FROM dbo.Invoices)
BEGIN
    INSERT INTO dbo.Invoices (PatientId, PatientName, PatientMrn, TotalAmount, PaidAmount, Status)
    VALUES 
        (1, 'Eric Mugabe', 'MRN-2026-0012', 40.00, 0.00, 'Pending'),
        (2, 'Claire Umutoni', 'MRN-2026-0034', 65.00, 20.00, 'Partial');

    INSERT INTO dbo.InvoiceLines (InvoiceId, Description, UnitPrice, Quantity)
    VALUES 
        (1, 'General Doctor Consultation', 25.00, 1),
        (1, 'Complete Blood Count (CBC) Lab Test', 15.00, 1),
        (2, 'Cardiology Consultation', 45.00, 1),
        (2, 'Amoxicillin 500mg (20 capsules)', 20.00, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Packages)
BEGIN
    INSERT INTO dbo.Packages (PatientId, PatientName, PatientMrn, PackageName, TotalSessions, UsedSessions, TotalAmount, PurchasedAt)
    VALUES
        (1, 'Eric Mugabe', 'MRN-2026-0012', 'Physiotherapy Rehabilitation (5 Sessions)', 5, 2, 250.00, DATEADD(DAY, -10, SYSUTCDATETIME())),
        (2, 'Claire Umutoni', 'MRN-2026-0034', 'Dialysis Treatment Plan (10 Sessions)', 10, 6, 800.00, DATEADD(DAY, -20, SYSUTCDATETIME())),
        (1, 'Eric Mugabe', 'MRN-2026-0012', 'Post-Op Wound Dressing (6 Sessions)', 6, 1, 180.00, DATEADD(DAY, -5, SYSUTCDATETIME()));

    INSERT INTO dbo.PackageUsages (PackageId, SessionNumber, RecognizedAmount, Notes, ConsumedAt)
    VALUES
        (1, 1, 50.00, 'Initial physiotherapy assessment and exercises', DATEADD(DAY, -8, SYSUTCDATETIME())),
        (1, 2, 50.00, 'Resistance training and ultrasound therapy', DATEADD(DAY, -3, SYSUTCDATETIME())),
        (2, 1, 80.00, 'Dialysis session 1', DATEADD(DAY, -18, SYSUTCDATETIME())),
        (2, 2, 80.00, 'Dialysis session 2', DATEADD(DAY, -15, SYSUTCDATETIME())),
        (3, 1, 30.00, 'Surgical incision cleaning and dressing change', DATEADD(DAY, -3, SYSUTCDATETIME()));
END
GO
