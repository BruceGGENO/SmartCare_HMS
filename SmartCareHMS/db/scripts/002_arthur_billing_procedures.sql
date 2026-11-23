-- Script: db/scripts/002_arthur_billing_procedures.sql
-- Owner: Cyitatire Uwonizeye Arthur
-- Module: MOD-04 Billing & Invoicing
-- Purpose: Tables, Stored Procedures, and Seed Data for Invoices, Lines, and Payments

USE SmartCareHMS_3;
GO

-- 1. Create Tables if they don't exist yet
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
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
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceLines')
BEGIN
    CREATE TABLE dbo.InvoiceLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NOT NULL FOREIGN KEY REFERENCES dbo.Invoices(Id),
        Description NVARCHAR(200) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Quantity INT NOT NULL DEFAULT 1,
        LineTotal AS (UnitPrice * Quantity) PERSISTED
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE dbo.Payments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NOT NULL FOREIGN KEY REFERENCES dbo.Invoices(Id),
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        TransactionReference NVARCHAR(100) NOT NULL,
        PaidAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 2. Stored Procedures

-- Procedure: Get all unpaid/pending invoices
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

-- Procedure: Get single invoice by ID
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

-- Procedure: Get lines for an invoice
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

-- Procedure: Record a payment and update invoice status
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
        -- Insert payment receipt
        INSERT INTO dbo.Payments (InvoiceId, Amount, PaymentMethod, TransactionReference)
        VALUES (@InvoiceId, @Amount, @PaymentMethod, @TransactionReference);

        -- Update invoice paid amount
        UPDATE dbo.Invoices
        SET 
            PaidAmount = PaidAmount + @Amount,
            Status = CASE 
                WHEN (PaidAmount + @Amount) >= TotalAmount THEN 'Paid'
                ELSE 'Partial'
            END
        WHERE Id = @InvoiceId;

        COMMIT TRANSACTION;
        SELECT 1; -- Success
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 3. Seed Sample Invoices (if table is empty)
IF NOT EXISTS (SELECT 1 FROM dbo.Invoices)
BEGIN
    -- Invoice 1: Consultation + Lab
    INSERT INTO dbo.Invoices (PatientId, PatientName, PatientMrn, TotalAmount, PaidAmount, Status)
    VALUES (1, 'Eric Mugabe', 'MRN-2026-0012', 40.00, 0.00, 'Pending');

    DECLARE @Inv1 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.InvoiceLines (InvoiceId, Description, UnitPrice, Quantity)
    VALUES 
        (@Inv1, 'General Doctor Consultation', 25.00, 1),
        (@Inv1, 'Complete Blood Count (CBC) Lab Test', 15.00, 1);

    -- Invoice 2: Specialist + Pharmacy
    INSERT INTO dbo.Invoices (PatientId, PatientName, PatientMrn, TotalAmount, PaidAmount, Status)
    VALUES (2, 'Claire Umutoni', 'MRN-2026-0034', 65.00, 0.00, 'Pending');

    DECLARE @Inv2 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.InvoiceLines (InvoiceId, Description, UnitPrice, Quantity)
    VALUES 
        (@Inv2, 'Cardiology Consultation', 45.00, 1),
        (@Inv2, 'Amoxicillin 500mg (20 capsules)', 20.00, 1);
END;
GO
