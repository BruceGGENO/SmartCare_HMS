-- SmartCare HMS — 065 Pharmacy (demo-complete)
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Drug', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Drug
    (
        DrugId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Drug PRIMARY KEY,
        Code         NVARCHAR(20)  NOT NULL,
        Name         NVARCHAR(120) NOT NULL,
        Strength     NVARCHAR(60)  NULL,
        StockQty     INT NOT NULL CONSTRAINT DF_Drug_Stock DEFAULT (0),
        CONSTRAINT UQ_Drug_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.Prescription', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Prescription
    (
        PrescriptionId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Prescription PRIMARY KEY,
        PatientId       INT NOT NULL,
        DrugId          INT NOT NULL,
        Dose            NVARCHAR(80) NOT NULL,
        Quantity        INT NOT NULL,
        PrescribedByUserId UNIQUEIDENTIFIER NULL,
        Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Rx_Status DEFAULT (N'Active'),
        PrescribedAtUtc DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_Rx_At DEFAULT (SYSDATETIMEOFFSET()),
        DispensedAtUtc  DATETIMEOFFSET(7) NULL,
        DispensedByUserId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_Rx_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId),
        CONSTRAINT FK_Rx_Drug FOREIGN KEY (DrugId) REFERENCES dbo.Drug (DrugId)
    );
    CREATE INDEX IX_Rx_Status ON dbo.Prescription (Status);
END
GO

MERGE dbo.Drug AS t
USING (VALUES
    (N'AMOX500', N'Amoxicillin', N'500mg', 120),
    (N'PARA500', N'Paracetamol', N'500mg', 200),
    (N'IBU400', N'Ibuprofen', N'400mg', 80),
    (N'MET500', N'Metformin', N'500mg', 60),
    (N'ORS', N'ORS sachets', N'1 sachet', 150)
) AS s (Code, Name, Strength, StockQty)
ON t.Code = s.Code
WHEN NOT MATCHED THEN INSERT (Code, Name, Strength, StockQty) VALUES (s.Code, s.Name, s.Strength, s.StockQty);
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'pharmacy.read', N'View drugs and prescriptions'),
    (N'pharmacy.prescribe', N'Create prescriptions'),
    (N'pharmacy.dispense', N'Dispense prescriptions and decrement stock')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'pharmacy.read'), (N'Super Admin', N'pharmacy.prescribe'), (N'Super Admin', N'pharmacy.dispense'),
(N'Hospital Admin', N'pharmacy.read'), (N'Hospital Admin', N'pharmacy.prescribe'), (N'Hospital Admin', N'pharmacy.dispense'),
(N'Pharmacist', N'pharmacy.read'), (N'Pharmacist', N'pharmacy.dispense'),
(N'Doctor', N'pharmacy.read'), (N'Doctor', N'pharmacy.prescribe'),
(N'Nurse', N'pharmacy.read');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListDrugs AS
BEGIN
    SET NOCOUNT ON;
    SELECT DrugId, Code, Name, Strength, StockQty FROM dbo.Drug ORDER BY Name;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListPrescriptions
    @PatientId INT = NULL,
    @Status NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.PrescriptionId, r.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           r.DrugId, d.Code AS DrugCode, d.Name AS DrugName, d.Strength,
           r.Dose, r.Quantity, r.PrescribedByUserId, r.Status,
           r.PrescribedAtUtc, r.DispensedAtUtc, r.DispensedByUserId
    FROM dbo.Prescription r
    INNER JOIN dbo.Patient p ON p.PatientId = r.PatientId
    INNER JOIN dbo.Drug d ON d.DrugId = r.DrugId
    WHERE (@PatientId IS NULL OR r.PatientId = @PatientId)
      AND (@Status IS NULL OR r.Status = @Status)
    ORDER BY r.PrescribedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPrescriptionById @PrescriptionId INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.PrescriptionId, r.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           r.DrugId, d.Code AS DrugCode, d.Name AS DrugName, d.Strength,
           r.Dose, r.Quantity, r.PrescribedByUserId, r.Status,
           r.PrescribedAtUtc, r.DispensedAtUtc, r.DispensedByUserId
    FROM dbo.Prescription r
    INNER JOIN dbo.Patient p ON p.PatientId = r.PatientId
    INNER JOIN dbo.Drug d ON d.DrugId = r.DrugId
    WHERE r.PrescriptionId = @PrescriptionId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreatePrescription
    @PatientId INT, @DrugId INT, @Dose NVARCHAR(80), @Quantity INT,
    @PrescribedByUserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Patient WHERE PatientId = @PatientId)
        THROW 50031, N'Patient not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Drug WHERE DrugId = @DrugId)
        THROW 50032, N'Drug not found.', 1;
    IF @Quantity <= 0 THROW 50033, N'Quantity must be greater than zero.', 1;

    INSERT INTO dbo.Prescription (PatientId, DrugId, Dose, Quantity, PrescribedByUserId, Status)
    VALUES (@PatientId, @DrugId, @Dose, @Quantity, @PrescribedByUserId, N'Active');

    DECLARE @Id INT = SCOPE_IDENTITY();
    EXEC dbo.sp_GetPrescriptionById @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DispensePrescription
    @PrescriptionId INT,
    @DispensedByUserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;

    DECLARE @DrugId INT, @Qty INT, @Status NVARCHAR(30), @Stock INT;
    SELECT @DrugId = DrugId, @Qty = Quantity, @Status = Status
    FROM dbo.Prescription WHERE PrescriptionId = @PrescriptionId;

    IF @DrugId IS NULL THROW 50034, N'Prescription not found.', 1;
    IF @Status <> N'Active' THROW 50035, N'Prescription is not active.', 1;

    SELECT @Stock = StockQty FROM dbo.Drug WHERE DrugId = @DrugId;
    IF @Stock < @Qty THROW 50036, N'Insufficient stock.', 1;

    BEGIN TRANSACTION;
    UPDATE dbo.Drug SET StockQty = StockQty - @Qty WHERE DrugId = @DrugId;
    UPDATE dbo.Prescription
    SET Status = N'Dispensed',
        DispensedAtUtc = SYSDATETIMEOFFSET(),
        DispensedByUserId = @DispensedByUserId
    WHERE PrescriptionId = @PrescriptionId;
    COMMIT TRANSACTION;

    EXEC dbo.sp_GetPrescriptionById @PrescriptionId;
END
GO
