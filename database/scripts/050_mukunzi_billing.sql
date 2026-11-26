-- SmartCare HMS
-- 050_mukunzi_billing.sql
-- Owner: Mukunzi Jonathan (Web API port of Arthur MOD-04)
-- Requires 010 (auth), 020 (Patient), 025 (permissions).
-- Include in reset_and_seed.sql (Elvin).

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Invoice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoice
    (
        InvoiceId    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Invoice PRIMARY KEY,
        PatientId    INT               NOT NULL,
        PatientName  NVARCHAR(200)     NOT NULL,
        PatientMrn   NVARCHAR(20)      NOT NULL,
        TotalAmount  DECIMAL(18,2)     NOT NULL
            CONSTRAINT DF_Invoice_Total DEFAULT (0),
        PaidAmount   DECIMAL(18,2)     NOT NULL
            CONSTRAINT DF_Invoice_Paid DEFAULT (0),
        Status       NVARCHAR(30)      NOT NULL
            CONSTRAINT DF_Invoice_Status DEFAULT (N'Pending'),
        CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Invoice_Created DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_Invoice_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId)
    );

    CREATE INDEX IX_Invoice_Status ON dbo.Invoice (Status);
    CREATE INDEX IX_Invoice_Patient ON dbo.Invoice (PatientId);
END
GO

IF OBJECT_ID(N'dbo.InvoiceLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceLine
    (
        InvoiceLineId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_InvoiceLine PRIMARY KEY,
        InvoiceId     INT               NOT NULL,
        Description   NVARCHAR(200)     NOT NULL,
        UnitPrice     DECIMAL(18,2)     NOT NULL
            CONSTRAINT DF_InvoiceLine_Price DEFAULT (0),
        Quantity      INT               NOT NULL
            CONSTRAINT DF_InvoiceLine_Qty DEFAULT (1),
        LineTotal     AS (UnitPrice * Quantity) PERSISTED,
        CONSTRAINT FK_InvoiceLine_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice (InvoiceId)
    );

    CREATE INDEX IX_InvoiceLine_Invoice ON dbo.InvoiceLine (InvoiceId);
END
GO

IF OBJECT_ID(N'dbo.Payment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payment
    (
        PaymentId             INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Payment PRIMARY KEY,
        InvoiceId             INT               NOT NULL,
        Amount                DECIMAL(18,2)     NOT NULL,
        PaymentMethod         NVARCHAR(50)      NOT NULL,
        TransactionReference  NVARCHAR(100)     NOT NULL,
        PaidAtUtc             DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Payment_PaidAt DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_Payment_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoice (InvoiceId)
    );

    CREATE INDEX IX_Payment_Invoice ON dbo.Payment (InvoiceId);
END
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'billing.read', N'List and view invoices'),
    (N'billing.create', N'Create invoices'),
    (N'billing.pay', N'Record invoice payments')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'billing.read'),
(N'Super Admin', N'billing.create'),
(N'Super Admin', N'billing.pay'),
(N'Hospital Admin', N'billing.read'),
(N'Hospital Admin', N'billing.create'),
(N'Hospital Admin', N'billing.pay'),
(N'Billing Staff', N'billing.read'),
(N'Billing Staff', N'billing.create'),
(N'Billing Staff', N'billing.pay'),
(N'Receptionist', N'billing.read'),
(N'Receptionist', N'billing.create');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode
FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListPendingInvoices
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        InvoiceId,
        PatientId,
        PatientName,
        PatientMrn,
        TotalAmount,
        PaidAmount,
        Status,
        CreatedAtUtc
    FROM dbo.Invoice
    WHERE Status IN (N'Pending', N'Partial')
       OR PaidAmount < TotalAmount
    ORDER BY CreatedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListInvoices
    @PatientId INT = NULL,
    @Status    NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        InvoiceId,
        PatientId,
        PatientName,
        PatientMrn,
        TotalAmount,
        PaidAmount,
        Status,
        CreatedAtUtc
    FROM dbo.Invoice
    WHERE (@PatientId IS NULL OR PatientId = @PatientId)
      AND (@Status IS NULL OR Status = @Status)
    ORDER BY CreatedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetInvoiceById
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        InvoiceId,
        PatientId,
        PatientName,
        PatientMrn,
        TotalAmount,
        PaidAmount,
        Status,
        CreatedAtUtc
    FROM dbo.Invoice
    WHERE InvoiceId = @InvoiceId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListInvoiceLines
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        InvoiceLineId,
        InvoiceId,
        Description,
        UnitPrice,
        Quantity,
        LineTotal
    FROM dbo.InvoiceLine
    WHERE InvoiceId = @InvoiceId
    ORDER BY InvoiceLineId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListInvoicePayments
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        PaymentId,
        InvoiceId,
        Amount,
        PaymentMethod,
        TransactionReference,
        PaidAtUtc
    FROM dbo.Payment
    WHERE InvoiceId = @InvoiceId
    ORDER BY PaidAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateInvoice
    @PatientId INT,
    @LinesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PatientName NVARCHAR(200);
    DECLARE @PatientMrn NVARCHAR(20);

    SELECT @PatientName = FullName, @PatientMrn = Mrn
    FROM dbo.Patient
    WHERE PatientId = @PatientId;

    IF @PatientName IS NULL
        THROW 50001, N'Patient not found.', 1;

    IF @LinesJson IS NULL OR LEN(LTRIM(RTRIM(@LinesJson))) = 0
        THROW 50002, N'Invoice must include at least one line.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM OPENJSON(@LinesJson)
        WITH (
            Description NVARCHAR(200) N'$.description',
            UnitPrice   DECIMAL(18,2) N'$.unitPrice',
            Quantity    INT           N'$.quantity'
        )
        WHERE Description IS NOT NULL AND LEN(LTRIM(RTRIM(Description))) > 0
          AND UnitPrice >= 0
          AND Quantity > 0)
        THROW 50002, N'Invoice must include at least one valid line.', 1;

    BEGIN TRANSACTION;

    DECLARE @InvoiceId INT;

    INSERT INTO dbo.Invoice (PatientId, PatientName, PatientMrn, TotalAmount, PaidAmount, Status)
    VALUES (@PatientId, @PatientName, @PatientMrn, 0, 0, N'Pending');

    SET @InvoiceId = SCOPE_IDENTITY();

    INSERT INTO dbo.InvoiceLine (InvoiceId, Description, UnitPrice, Quantity)
    SELECT
        @InvoiceId,
        LTRIM(RTRIM(Description)),
        UnitPrice,
        Quantity
    FROM OPENJSON(@LinesJson)
    WITH (
        Description NVARCHAR(200) N'$.description',
        UnitPrice   DECIMAL(18,2) N'$.unitPrice',
        Quantity    INT           N'$.quantity'
    )
    WHERE Description IS NOT NULL AND LEN(LTRIM(RTRIM(Description))) > 0
      AND UnitPrice >= 0
      AND Quantity > 0;

    UPDATE dbo.Invoice
    SET TotalAmount = (
            SELECT SUM(LineTotal) FROM dbo.InvoiceLine WHERE InvoiceId = @InvoiceId
        )
    WHERE InvoiceId = @InvoiceId;

    COMMIT TRANSACTION;

    SELECT
        InvoiceId,
        PatientId,
        PatientName,
        PatientMrn,
        TotalAmount,
        PaidAmount,
        Status,
        CreatedAtUtc
    FROM dbo.Invoice
    WHERE InvoiceId = @InvoiceId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RecordPayment
    @InvoiceId            INT,
    @Amount               DECIMAL(18,2),
    @PaymentMethod        NVARCHAR(50),
    @TransactionReference NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Total DECIMAL(18,2);
    DECLARE @Paid  DECIMAL(18,2);
    DECLARE @Status NVARCHAR(30);

    SELECT @Total = TotalAmount, @Paid = PaidAmount, @Status = Status
    FROM dbo.Invoice
    WHERE InvoiceId = @InvoiceId;

    IF @Total IS NULL
        THROW 50003, N'Invoice not found.', 1;

    IF @Status = N'Paid' OR @Paid >= @Total
        THROW 50004, N'Invoice is already paid.', 1;

    IF @Amount <= 0
        THROW 50005, N'Payment amount must be greater than zero.', 1;

    IF @Amount > (@Total - @Paid)
        THROW 50006, N'Payment exceeds remaining balance.', 1;

    BEGIN TRANSACTION;

    INSERT INTO dbo.Payment (InvoiceId, Amount, PaymentMethod, TransactionReference)
    VALUES (@InvoiceId, @Amount, @PaymentMethod, @TransactionReference);

    DECLARE @PaymentId INT = SCOPE_IDENTITY();

    UPDATE dbo.Invoice
    SET
        PaidAmount = PaidAmount + @Amount,
        Status = CASE
            WHEN (PaidAmount + @Amount) >= TotalAmount THEN N'Paid'
            ELSE N'Partial'
        END
    WHERE InvoiceId = @InvoiceId;

    COMMIT TRANSACTION;

    SELECT
        PaymentId,
        InvoiceId,
        Amount,
        PaymentMethod,
        TransactionReference,
        PaidAtUtc
    FROM dbo.Payment
    WHERE PaymentId = @PaymentId;
END
GO
