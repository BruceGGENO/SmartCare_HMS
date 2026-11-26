-- SmartCare HMS — 070 Beds / admissions (demo-complete)
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Ward', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ward
    (
        WardId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ward PRIMARY KEY,
        Name   NVARCHAR(80) NOT NULL,
        Code   NVARCHAR(20) NOT NULL,
        CONSTRAINT UQ_Ward_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.Bed', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bed
    (
        BedId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bed PRIMARY KEY,
        WardId INT NOT NULL,
        Code   NVARCHAR(20) NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Bed_Status DEFAULT (N'Available'),
        CONSTRAINT FK_Bed_Ward FOREIGN KEY (WardId) REFERENCES dbo.Ward (WardId),
        CONSTRAINT UQ_Bed_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.Admission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Admission
    (
        AdmissionId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Admission PRIMARY KEY,
        PatientId      INT NOT NULL,
        BedId          INT NOT NULL,
        Status         NVARCHAR(30) NOT NULL CONSTRAINT DF_Admission_Status DEFAULT (N'Admitted'),
        AdmittedAtUtc  DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_Admission_At DEFAULT (SYSDATETIMEOFFSET()),
        DischargedAtUtc DATETIMEOFFSET(7) NULL,
        CONSTRAINT FK_Admission_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId),
        CONSTRAINT FK_Admission_Bed FOREIGN KEY (BedId) REFERENCES dbo.Bed (BedId)
    );
    CREATE INDEX IX_Admission_Status ON dbo.Admission (Status);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Ward)
BEGIN
    INSERT INTO dbo.Ward (Name, Code) VALUES (N'General Ward', N'GW'), (N'Maternity', N'MAT');
    DECLARE @Gw INT = (SELECT WardId FROM dbo.Ward WHERE Code = N'GW');
    DECLARE @Mat INT = (SELECT WardId FROM dbo.Ward WHERE Code = N'MAT');
    INSERT INTO dbo.Bed (WardId, Code, Status) VALUES
        (@Gw, N'GW-01', N'Available'), (@Gw, N'GW-02', N'Available'),
        (@Gw, N'GW-03', N'Available'), (@Gw, N'GW-04', N'Available'),
        (@Mat, N'MAT-01', N'Available'), (@Mat, N'MAT-02', N'Available');
END
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'beds.read', N'View wards, beds, and admissions'),
    (N'beds.assign', N'Admit, transfer, and free beds')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'beds.read'), (N'Super Admin', N'beds.assign'),
(N'Hospital Admin', N'beds.read'), (N'Hospital Admin', N'beds.assign'),
(N'Nurse', N'beds.read'), (N'Nurse', N'beds.assign'),
(N'Doctor', N'beds.read'),
(N'Receptionist', N'beds.read');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListWards AS
BEGIN
    SET NOCOUNT ON;
    SELECT WardId, Name, Code FROM dbo.Ward ORDER BY Name;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListBeds @WardId INT = NULL AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BedId, b.WardId, w.Name AS WardName, b.Code, b.Status
    FROM dbo.Bed b
    INNER JOIN dbo.Ward w ON w.WardId = b.WardId
    WHERE @WardId IS NULL OR b.WardId = @WardId
    ORDER BY w.Name, b.Code;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListAdmissions @Status NVARCHAR(30) = NULL AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.AdmissionId, a.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           a.BedId, b.Code AS BedCode, w.Name AS WardName, a.Status, a.AdmittedAtUtc, a.DischargedAtUtc
    FROM dbo.Admission a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.Bed b ON b.BedId = a.BedId
    INNER JOIN dbo.Ward w ON w.WardId = b.WardId
    WHERE @Status IS NULL OR a.Status = @Status
    ORDER BY a.AdmittedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAdmissionById @AdmissionId INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.AdmissionId, a.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           a.BedId, b.Code AS BedCode, w.Name AS WardName, a.Status, a.AdmittedAtUtc, a.DischargedAtUtc
    FROM dbo.Admission a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.Bed b ON b.BedId = a.BedId
    INNER JOIN dbo.Ward w ON w.WardId = b.WardId
    WHERE a.AdmissionId = @AdmissionId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AdmitPatient @PatientId INT, @BedId INT AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Patient WHERE PatientId = @PatientId)
        THROW 50041, N'Patient not found.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Admission WHERE PatientId = @PatientId AND Status = N'Admitted')
        THROW 50042, N'Patient already has an active admission.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Bed WHERE BedId = @BedId AND Status = N'Available')
        THROW 50043, N'Bed is not available.', 1;

    BEGIN TRANSACTION;
    UPDATE dbo.Bed SET Status = N'Occupied' WHERE BedId = @BedId;
    INSERT INTO dbo.Admission (PatientId, BedId, Status) VALUES (@PatientId, @BedId, N'Admitted');
    DECLARE @Id INT = SCOPE_IDENTITY();
    COMMIT TRANSACTION;
    EXEC dbo.sp_GetAdmissionById @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TransferAdmission @AdmissionId INT, @NewBedId INT AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @OldBedId INT, @Status NVARCHAR(30);
    SELECT @OldBedId = BedId, @Status = Status FROM dbo.Admission WHERE AdmissionId = @AdmissionId;
    IF @OldBedId IS NULL THROW 50044, N'Admission not found.', 1;
    IF @Status <> N'Admitted' THROW 50045, N'Admission is not active.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Bed WHERE BedId = @NewBedId AND Status = N'Available')
        THROW 50043, N'Bed is not available.', 1;

    BEGIN TRANSACTION;
    UPDATE dbo.Bed SET Status = N'Available' WHERE BedId = @OldBedId;
    UPDATE dbo.Bed SET Status = N'Occupied' WHERE BedId = @NewBedId;
    UPDATE dbo.Admission SET BedId = @NewBedId WHERE AdmissionId = @AdmissionId;
    COMMIT TRANSACTION;
    EXEC dbo.sp_GetAdmissionById @AdmissionId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_FreeAdmissionBed @AdmissionId INT AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @BedId INT, @Status NVARCHAR(30);
    SELECT @BedId = BedId, @Status = Status FROM dbo.Admission WHERE AdmissionId = @AdmissionId;
    IF @BedId IS NULL THROW 50044, N'Admission not found.', 1;
    IF @Status <> N'Admitted' THROW 50045, N'Admission is not active.', 1;

    BEGIN TRANSACTION;
    UPDATE dbo.Bed SET Status = N'Available' WHERE BedId = @BedId;
    UPDATE dbo.Admission SET Status = N'Discharged', DischargedAtUtc = SYSDATETIMEOFFSET()
    WHERE AdmissionId = @AdmissionId;
    COMMIT TRANSACTION;
    EXEC dbo.sp_GetAdmissionById @AdmissionId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOccupancySnapshot AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(*) AS TotalBeds,
        SUM(CASE WHEN Status = N'Occupied' THEN 1 ELSE 0 END) AS OccupiedBeds,
        SUM(CASE WHEN Status = N'Available' THEN 1 ELSE 0 END) AS AvailableBeds
    FROM dbo.Bed;
END
GO
