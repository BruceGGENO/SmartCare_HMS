-- SmartCare HMS
-- 045_mukunzi_patients.sql
-- Owner: Mukunzi Jonathan (MOD-01)
-- Requires 010 (auth), 020 (Patient base), 025 (permissions).
-- Include in reset_and_seed.sql (Elvin).

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.Patient', N'Sex') IS NULL
BEGIN
    ALTER TABLE dbo.Patient ADD Sex NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.PatientAllergy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientAllergy
    (
        AllergyId    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PatientAllergy PRIMARY KEY,
        PatientId    INT               NOT NULL,
        Substance    NVARCHAR(200)     NOT NULL,
        Reaction     NVARCHAR(300)     NULL,
        Severity     NVARCHAR(50)      NULL,
        NotedAtUtc   DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_PatientAllergy_Noted DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_PatientAllergy_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId)
    );

    CREATE INDEX IX_PatientAllergy_Patient ON dbo.PatientAllergy (PatientId);
END
GO

IF OBJECT_ID(N'dbo.IcdCode', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IcdCode
    (
        Code        NVARCHAR(16)  NOT NULL
            CONSTRAINT PK_IcdCode PRIMARY KEY,
        Description NVARCHAR(300) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.PatientDiagnosis', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientDiagnosis
    (
        DiagnosisId       INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PatientDiagnosis PRIMARY KEY,
        PatientId         INT               NOT NULL,
        IcdCode           NVARCHAR(16)      NOT NULL,
        Notes             NVARCHAR(500)     NULL,
        DiagnosedByUserId UNIQUEIDENTIFIER  NULL,
        DiagnosedAtUtc    DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_PatientDiagnosis_At DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_PatientDiagnosis_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId),
        CONSTRAINT FK_PatientDiagnosis_Icd FOREIGN KEY (IcdCode) REFERENCES dbo.IcdCode (Code)
    );

    CREATE INDEX IX_PatientDiagnosis_Patient ON dbo.PatientDiagnosis (PatientId);
END
GO

MERGE dbo.IcdCode AS t
USING (VALUES
    (N'J06.9', N'Acute upper respiratory infection, unspecified'),
    (N'J18.9', N'Pneumonia, unspecified'),
    (N'I10', N'Essential (primary) hypertension'),
    (N'E11.9', N'Type 2 diabetes mellitus without complications'),
    (N'A09', N'Infectious gastroenteritis and colitis, unspecified'),
    (N'R50.9', N'Fever, unspecified'),
    (N'R51', N'Headache'),
    (N'M54.5', N'Low back pain'),
    (N'N39.0', N'Urinary tract infection, site not specified'),
    (N'J45.909', N'Unspecified asthma, uncomplicated'),
    (N'K21.9', N'Gastro-esophageal reflux disease without esophagitis'),
    (N'B34.9', N'Viral infection, unspecified'),
    (N'S00.01XA', N'Abrasion of scalp, initial encounter'),
    (N'Z00.00', N'Encounter for general adult medical examination'),
    (N'Z23', N'Encounter for immunization')
) AS s (Code, Description)
ON t.Code = s.Code
WHEN NOT MATCHED THEN INSERT (Code, Description) VALUES (s.Code, s.Description);
GO

-- Permissions for MOD-01
MERGE dbo.Permission AS t
USING (VALUES
    (N'patients.read', N'List and view patient demographics'),
    (N'patients.register', N'Register a new patient'),
    (N'patients.clinical.read', N'View allergies and diagnoses'),
    (N'patients.clinical.write', N'Add allergies and diagnoses'),
    (N'patients.icd.read', N'Search ICD diagnosis codes')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'patients.read'),
(N'Super Admin', N'patients.register'),
(N'Super Admin', N'patients.clinical.read'),
(N'Super Admin', N'patients.clinical.write'),
(N'Super Admin', N'patients.icd.read'),
(N'Hospital Admin', N'patients.read'),
(N'Hospital Admin', N'patients.register'),
(N'Hospital Admin', N'patients.clinical.read'),
(N'Hospital Admin', N'patients.clinical.write'),
(N'Hospital Admin', N'patients.icd.read'),
(N'Receptionist', N'patients.read'),
(N'Receptionist', N'patients.register'),
(N'Doctor', N'patients.read'),
(N'Doctor', N'patients.clinical.read'),
(N'Doctor', N'patients.clinical.write'),
(N'Doctor', N'patients.icd.read'),
(N'Nurse', N'patients.read'),
(N'Nurse', N'patients.clinical.read'),
(N'Nurse', N'patients.clinical.write'),
(N'Nurse', N'patients.icd.read'),
-- Billing may see demographics only (no clinical notes)
(N'Billing Staff', N'patients.read');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode
FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientById
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PatientId, UserId, Mrn, FullName, DateOfBirth, Phone, Sex, CreatedAtUtc
    FROM dbo.Patient
    WHERE PatientId = @PatientId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPatientByMrn
    @Mrn NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PatientId, UserId, Mrn, FullName, DateOfBirth, Phone, Sex, CreatedAtUtc
    FROM dbo.Patient
    WHERE Mrn = @Mrn;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SearchPatients
    @Query NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (100)
        PatientId, UserId, Mrn, FullName, DateOfBirth, Phone, Sex, CreatedAtUtc
    FROM dbo.Patient
    WHERE @Query IS NULL
       OR @Query = N''
       OR Mrn LIKE N'%' + @Query + N'%'
       OR FullName LIKE N'%' + @Query + N'%'
       OR Phone LIKE N'%' + @Query + N'%'
    ORDER BY FullName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegisterPatient
    @UserId      UNIQUEIDENTIFIER = NULL,
    @Mrn         NVARCHAR(20),
    @FullName    NVARCHAR(200),
    @DateOfBirth DATE = NULL,
    @Phone       NVARCHAR(30) = NULL,
    @Sex         NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Patient (UserId, Mrn, FullName, DateOfBirth, Phone, Sex)
    VALUES (@UserId, @Mrn, @FullName, @DateOfBirth, @Phone, @Sex);

    SELECT PatientId, UserId, Mrn, FullName, DateOfBirth, Phone, Sex, CreatedAtUtc
    FROM dbo.Patient
    WHERE PatientId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListPatientAllergies
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AllergyId, PatientId, Substance, Reaction, Severity, NotedAtUtc
    FROM dbo.PatientAllergy
    WHERE PatientId = @PatientId
    ORDER BY NotedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InsertPatientAllergy
    @PatientId INT,
    @Substance NVARCHAR(200),
    @Reaction  NVARCHAR(300) = NULL,
    @Severity  NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.PatientAllergy (PatientId, Substance, Reaction, Severity)
    VALUES (@PatientId, @Substance, @Reaction, @Severity);

    SELECT AllergyId, PatientId, Substance, Reaction, Severity, NotedAtUtc
    FROM dbo.PatientAllergy
    WHERE AllergyId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SearchIcdCodes
    @Query NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (50) Code, Description
    FROM dbo.IcdCode
    WHERE @Query IS NULL
       OR @Query = N''
       OR Code LIKE N'%' + @Query + N'%'
       OR Description LIKE N'%' + @Query + N'%'
    ORDER BY Code;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListPatientDiagnoses
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        d.DiagnosisId,
        d.PatientId,
        d.IcdCode,
        i.Description AS IcdDescription,
        d.Notes,
        d.DiagnosedByUserId,
        d.DiagnosedAtUtc
    FROM dbo.PatientDiagnosis d
    INNER JOIN dbo.IcdCode i ON i.Code = d.IcdCode
    WHERE d.PatientId = @PatientId
    ORDER BY d.DiagnosedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InsertPatientDiagnosis
    @PatientId         INT,
    @IcdCode           NVARCHAR(16),
    @Notes             NVARCHAR(500) = NULL,
    @DiagnosedByUserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PatientDiagnosis (PatientId, IcdCode, Notes, DiagnosedByUserId)
    VALUES (@PatientId, @IcdCode, @Notes, @DiagnosedByUserId);

    DECLARE @Id INT = SCOPE_IDENTITY();

    SELECT
        d.DiagnosisId,
        d.PatientId,
        d.IcdCode,
        i.Description AS IcdDescription,
        d.Notes,
        d.DiagnosedByUserId,
        d.DiagnosedAtUtc
    FROM dbo.PatientDiagnosis d
    INNER JOIN dbo.IcdCode i ON i.Code = d.IcdCode
    WHERE d.DiagnosisId = @Id;
END
GO
