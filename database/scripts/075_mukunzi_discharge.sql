-- SmartCare HMS — 075 Discharge checklist (demo-complete)
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.DischargeCase', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DischargeCase
    (
        DischargeId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DischargeCase PRIMARY KEY,
        AdmissionId   INT NOT NULL,
        PatientId     INT NOT NULL,
        Status        NVARCHAR(30) NOT NULL CONSTRAINT DF_Discharge_Status DEFAULT (N'InProgress'),
        CreatedAtUtc  DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_Discharge_Created DEFAULT (SYSDATETIMEOFFSET()),
        CompletedAtUtc DATETIMEOFFSET(7) NULL,
        CONSTRAINT FK_Discharge_Admission FOREIGN KEY (AdmissionId) REFERENCES dbo.Admission (AdmissionId),
        CONSTRAINT FK_Discharge_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId)
    );
END
GO

IF OBJECT_ID(N'dbo.DischargeStep', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DischargeStep
    (
        DischargeStepId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DischargeStep PRIMARY KEY,
        DischargeId     INT NOT NULL,
        StepName        NVARCHAR(80) NOT NULL,
        SortOrder       INT NOT NULL,
        IsComplete      BIT NOT NULL CONSTRAINT DF_DischargeStep_Done DEFAULT (0),
        CompletedAtUtc  DATETIMEOFFSET(7) NULL,
        CONSTRAINT FK_DischargeStep_Case FOREIGN KEY (DischargeId) REFERENCES dbo.DischargeCase (DischargeId)
    );
END
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'discharge.read', N'View discharge cases'),
    (N'discharge.write', N'Initiate and complete discharge steps')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'discharge.read'), (N'Super Admin', N'discharge.write'),
(N'Hospital Admin', N'discharge.read'), (N'Hospital Admin', N'discharge.write'),
(N'Nurse', N'discharge.read'), (N'Nurse', N'discharge.write'),
(N'Doctor', N'discharge.read'), (N'Doctor', N'discharge.write'),
(N'Billing Staff', N'discharge.read'), (N'Billing Staff', N'discharge.write'),
(N'Pharmacist', N'discharge.read'), (N'Pharmacist', N'discharge.write');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListDischargeCases @Status NVARCHAR(30) = NULL AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.DischargeId, d.AdmissionId, d.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           d.Status, d.CreatedAtUtc, d.CompletedAtUtc
    FROM dbo.DischargeCase d
    INNER JOIN dbo.Patient p ON p.PatientId = d.PatientId
    WHERE @Status IS NULL OR d.Status = @Status
    ORDER BY d.CreatedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDischargeById @DischargeId INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.DischargeId, d.AdmissionId, d.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           d.Status, d.CreatedAtUtc, d.CompletedAtUtc
    FROM dbo.DischargeCase d
    INNER JOIN dbo.Patient p ON p.PatientId = d.PatientId
    WHERE d.DischargeId = @DischargeId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListDischargeSteps @DischargeId INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT DischargeStepId, DischargeId, StepName, SortOrder, IsComplete, CompletedAtUtc
    FROM dbo.DischargeStep
    WHERE DischargeId = @DischargeId
    ORDER BY SortOrder;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InitiateDischarge @AdmissionId INT AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @PatientId INT, @Status NVARCHAR(30);
    SELECT @PatientId = PatientId, @Status = Status FROM dbo.Admission WHERE AdmissionId = @AdmissionId;
    IF @PatientId IS NULL THROW 50051, N'Admission not found.', 1;
    IF @Status <> N'Admitted' THROW 50052, N'Admission is not active.', 1;
    IF EXISTS (SELECT 1 FROM dbo.DischargeCase WHERE AdmissionId = @AdmissionId AND Status = N'InProgress')
        THROW 50053, N'Discharge already in progress for this admission.', 1;

    BEGIN TRANSACTION;
    INSERT INTO dbo.DischargeCase (AdmissionId, PatientId, Status)
    VALUES (@AdmissionId, @PatientId, N'InProgress');
    DECLARE @Id INT = SCOPE_IDENTITY();

    INSERT INTO dbo.DischargeStep (DischargeId, StepName, SortOrder) VALUES
        (@Id, N'Pharmacy clearance', 1),
        (@Id, N'Lab clearance', 2),
        (@Id, N'Billing clearance', 3),
        (@Id, N'Ward nurse sign-off', 4);

    UPDATE dbo.Admission SET Status = N'DischargePending' WHERE AdmissionId = @AdmissionId;
    COMMIT TRANSACTION;

    EXEC dbo.sp_GetDischargeById @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompleteDischargeStep @DischargeStepId INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DischargeId INT, @IsComplete BIT;
    SELECT @DischargeId = DischargeId, @IsComplete = IsComplete
    FROM dbo.DischargeStep WHERE DischargeStepId = @DischargeStepId;
    IF @DischargeId IS NULL THROW 50054, N'Discharge step not found.', 1;
    IF @IsComplete = 1 THROW 50055, N'Step already complete.', 1;
    IF EXISTS (SELECT 1 FROM dbo.DischargeCase WHERE DischargeId = @DischargeId AND Status = N'Completed')
        THROW 50056, N'Discharge already completed.', 1;

    UPDATE dbo.DischargeStep
    SET IsComplete = 1, CompletedAtUtc = SYSDATETIMEOFFSET()
    WHERE DischargeStepId = @DischargeStepId;

    SELECT DischargeStepId, DischargeId, StepName, SortOrder, IsComplete, CompletedAtUtc
    FROM dbo.DischargeStep WHERE DischargeStepId = @DischargeStepId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompleteDischarge @DischargeId INT AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @AdmissionId INT, @Status NVARCHAR(30), @BedId INT;
    SELECT @AdmissionId = AdmissionId, @Status = Status
    FROM dbo.DischargeCase WHERE DischargeId = @DischargeId;
    IF @AdmissionId IS NULL THROW 50057, N'Discharge not found.', 1;
    IF @Status = N'Completed' THROW 50056, N'Discharge already completed.', 1;
    IF EXISTS (SELECT 1 FROM dbo.DischargeStep WHERE DischargeId = @DischargeId AND IsComplete = 0)
        THROW 50058, N'All discharge steps must be complete.', 1;

    SELECT @BedId = BedId FROM dbo.Admission WHERE AdmissionId = @AdmissionId;

    BEGIN TRANSACTION;
    UPDATE dbo.DischargeCase
    SET Status = N'Completed', CompletedAtUtc = SYSDATETIMEOFFSET()
    WHERE DischargeId = @DischargeId;
    UPDATE dbo.Admission
    SET Status = N'Discharged', DischargedAtUtc = SYSDATETIMEOFFSET()
    WHERE AdmissionId = @AdmissionId;
    IF @BedId IS NOT NULL
        UPDATE dbo.Bed SET Status = N'Available' WHERE BedId = @BedId;
    COMMIT TRANSACTION;

    EXEC dbo.sp_GetDischargeById @DischargeId;
END
GO
