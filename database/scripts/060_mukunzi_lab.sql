-- SmartCare HMS — 060 Lab (demo-complete)
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.LabTest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LabTest
    (
        LabTestId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LabTest PRIMARY KEY,
        Code        NVARCHAR(20)  NOT NULL,
        Name        NVARCHAR(120) NOT NULL,
        Specimen    NVARCHAR(60)  NULL,
        CONSTRAINT UQ_LabTest_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.LabOrder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LabOrder
    (
        LabOrderId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LabOrder PRIMARY KEY,
        PatientId       INT NOT NULL,
        LabTestId       INT NOT NULL,
        OrderedByUserId UNIQUEIDENTIFIER NULL,
        Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_LabOrder_Status DEFAULT (N'Ordered'),
        ResultValue     NVARCHAR(200) NULL,
        ResultNotes     NVARCHAR(500) NULL,
        ResultedByUserId UNIQUEIDENTIFIER NULL,
        OrderedAtUtc    DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_LabOrder_Ordered DEFAULT (SYSDATETIMEOFFSET()),
        ResultedAtUtc   DATETIMEOFFSET(7) NULL,
        CONSTRAINT FK_LabOrder_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId),
        CONSTRAINT FK_LabOrder_Test FOREIGN KEY (LabTestId) REFERENCES dbo.LabTest (LabTestId)
    );
    CREATE INDEX IX_LabOrder_Patient ON dbo.LabOrder (PatientId);
    CREATE INDEX IX_LabOrder_Status ON dbo.LabOrder (Status);
END
GO

MERGE dbo.LabTest AS t
USING (VALUES
    (N'CBC', N'Complete Blood Count', N'Blood'),
    (N'GLU', N'Blood Glucose', N'Blood'),
    (N'MAL', N'Malaria smear', N'Blood'),
    (N'URIN', N'Urinalysis', N'Urine'),
    (N'LFT', N'Liver Function Panel', N'Blood'),
    (N'CXR', N'Chest X-Ray', N'Imaging'),
    (N'HIV', N'HIV rapid test', N'Blood'),
    (N'CRP', N'C-Reactive Protein', N'Blood')
) AS s (Code, Name, Specimen)
ON t.Code = s.Code
WHEN NOT MATCHED THEN INSERT (Code, Name, Specimen) VALUES (s.Code, s.Name, s.Specimen);
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'lab.read', N'View lab tests and orders'),
    (N'lab.order', N'Create lab orders'),
    (N'lab.result', N'File lab results')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'lab.read'), (N'Super Admin', N'lab.order'), (N'Super Admin', N'lab.result'),
(N'Hospital Admin', N'lab.read'), (N'Hospital Admin', N'lab.order'), (N'Hospital Admin', N'lab.result'),
(N'Lab Technician', N'lab.read'), (N'Lab Technician', N'lab.result'),
(N'Doctor', N'lab.read'), (N'Doctor', N'lab.order'),
(N'Nurse', N'lab.read'), (N'Nurse', N'lab.order');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListLabTests AS
BEGIN
    SET NOCOUNT ON;
    SELECT LabTestId, Code, Name, Specimen FROM dbo.LabTest ORDER BY Code;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListLabOrders
    @PatientId INT = NULL,
    @Status NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.LabOrderId, o.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           o.LabTestId, t.Code AS TestCode, t.Name AS TestName,
           o.OrderedByUserId, o.Status, o.ResultValue, o.ResultNotes,
           o.ResultedByUserId, o.OrderedAtUtc, o.ResultedAtUtc
    FROM dbo.LabOrder o
    INNER JOIN dbo.Patient p ON p.PatientId = o.PatientId
    INNER JOIN dbo.LabTest t ON t.LabTestId = o.LabTestId
    WHERE (@PatientId IS NULL OR o.PatientId = @PatientId)
      AND (@Status IS NULL OR o.Status = @Status)
    ORDER BY o.OrderedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetLabOrderById @LabOrderId INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.LabOrderId, o.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
           o.LabTestId, t.Code AS TestCode, t.Name AS TestName,
           o.OrderedByUserId, o.Status, o.ResultValue, o.ResultNotes,
           o.ResultedByUserId, o.OrderedAtUtc, o.ResultedAtUtc
    FROM dbo.LabOrder o
    INNER JOIN dbo.Patient p ON p.PatientId = o.PatientId
    INNER JOIN dbo.LabTest t ON t.LabTestId = o.LabTestId
    WHERE o.LabOrderId = @LabOrderId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateLabOrder
    @PatientId INT, @LabTestId INT, @OrderedByUserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Patient WHERE PatientId = @PatientId)
        THROW 50021, N'Patient not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.LabTest WHERE LabTestId = @LabTestId)
        THROW 50022, N'Lab test not found.', 1;

    INSERT INTO dbo.LabOrder (PatientId, LabTestId, OrderedByUserId, Status)
    VALUES (@PatientId, @LabTestId, @OrderedByUserId, N'Ordered');

    DECLARE @Id INT = SCOPE_IDENTITY();
    EXEC dbo.sp_GetLabOrderById @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_FileLabResult
    @LabOrderId INT,
    @ResultValue NVARCHAR(200),
    @ResultNotes NVARCHAR(500) = NULL,
    @ResultedByUserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.LabOrder WHERE LabOrderId = @LabOrderId)
        THROW 50023, N'Lab order not found.', 1;
    IF EXISTS (SELECT 1 FROM dbo.LabOrder WHERE LabOrderId = @LabOrderId AND Status = N'Completed')
        THROW 50024, N'Lab order already completed.', 1;

    UPDATE dbo.LabOrder
    SET Status = N'Completed',
        ResultValue = @ResultValue,
        ResultNotes = @ResultNotes,
        ResultedByUserId = @ResultedByUserId,
        ResultedAtUtc = SYSDATETIMEOFFSET()
    WHERE LabOrderId = @LabOrderId;

    EXEC dbo.sp_GetLabOrderById @LabOrderId;
END
GO
