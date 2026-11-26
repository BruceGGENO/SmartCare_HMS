-- SmartCare HMS
-- 025_mukunzi_permissions.sql
-- Owner: Mukunzi Jonathan
-- Requires 010_mukunzi_authentication.sql (Role / User / UserRole).
-- Permission-based authorization: roles grant permissions; APIs check permissions.
-- Include in reset_and_seed.sql (Elvin).

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Permission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permission
    (
        PermissionCode NVARCHAR(100) NOT NULL
            CONSTRAINT PK_Permission PRIMARY KEY,
        Description    NVARCHAR(200) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermission
    (
        RoleName       NVARCHAR(50)  NOT NULL,
        PermissionCode NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_RolePermission PRIMARY KEY (RoleName, PermissionCode),
        CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleName) REFERENCES dbo.Role (RoleName),
        CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionCode) REFERENCES dbo.Permission (PermissionCode)
    );
END
GO

-- Catalog
MERGE dbo.Permission AS t
USING (VALUES
    (N'audit.read', N'View audit log'),
    (N'outbox.read', N'View outbox stub messages'),
    (N'documents.upload', N'Upload documents'),
    (N'documents.read', N'Read/download documents'),
    (N'appointments.read', N'List/view appointments'),
    (N'appointments.book', N'Book appointments'),
    (N'appointments.reschedule', N'Reschedule appointments'),
    (N'appointments.cancel', N'Cancel appointments'),
    (N'appointments.checkin', N'Check in patients'),
    (N'appointments.patients.read', N'List patients for booking'),
    (N'appointments.doctors.read', N'List doctors for booking'),
    (N'portal.appointments.read', N'Patient: own appointments'),
    (N'portal.history.read', N'Patient: treatment history'),
    (N'portal.packages.read', N'Patient: package balances'),
    (N'portal.otp.login', N'Patient: OTP login')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

-- Role → permission map (roles remain; APIs authorize on permissions)
DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));

INSERT INTO @Map (RoleName, PermissionCode)
VALUES
-- Super Admin / Hospital Admin: broad access
(N'Super Admin', N'audit.read'),
(N'Super Admin', N'outbox.read'),
(N'Super Admin', N'documents.upload'),
(N'Super Admin', N'documents.read'),
(N'Super Admin', N'appointments.read'),
(N'Super Admin', N'appointments.book'),
(N'Super Admin', N'appointments.reschedule'),
(N'Super Admin', N'appointments.cancel'),
(N'Super Admin', N'appointments.checkin'),
(N'Super Admin', N'appointments.patients.read'),
(N'Super Admin', N'appointments.doctors.read'),

(N'Hospital Admin', N'audit.read'),
(N'Hospital Admin', N'outbox.read'),
(N'Hospital Admin', N'documents.upload'),
(N'Hospital Admin', N'documents.read'),
(N'Hospital Admin', N'appointments.read'),
(N'Hospital Admin', N'appointments.book'),
(N'Hospital Admin', N'appointments.reschedule'),
(N'Hospital Admin', N'appointments.cancel'),
(N'Hospital Admin', N'appointments.checkin'),
(N'Hospital Admin', N'appointments.patients.read'),
(N'Hospital Admin', N'appointments.doctors.read'),

-- Receptionist
(N'Receptionist', N'appointments.read'),
(N'Receptionist', N'appointments.book'),
(N'Receptionist', N'appointments.reschedule'),
(N'Receptionist', N'appointments.cancel'),
(N'Receptionist', N'appointments.checkin'),
(N'Receptionist', N'appointments.patients.read'),
(N'Receptionist', N'appointments.doctors.read'),
(N'Receptionist', N'documents.upload'),
(N'Receptionist', N'documents.read'),

-- Doctor / Nurse: clinic list + documents
(N'Doctor', N'appointments.read'),
(N'Doctor', N'documents.upload'),
(N'Doctor', N'documents.read'),

(N'Nurse', N'appointments.read'),
(N'Nurse', N'documents.upload'),
(N'Nurse', N'documents.read'),

-- Lab / Pharmacy / Billing: documents for now (module APIs later)
(N'Lab Technician', N'documents.upload'),
(N'Lab Technician', N'documents.read'),
(N'Pharmacist', N'documents.upload'),
(N'Pharmacist', N'documents.read'),
(N'Billing Staff', N'documents.upload'),
(N'Billing Staff', N'documents.read'),

-- Patient portal
(N'Patient', N'portal.appointments.read'),
(N'Patient', N'portal.history.read'),
(N'Patient', N'portal.packages.read'),
(N'Patient', N'portal.otp.login'),
(N'Patient', N'documents.read');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode
FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUserPermissions
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT rp.PermissionCode
    FROM dbo.UserRole ur
    INNER JOIN dbo.RolePermission rp ON rp.RoleName = ur.RoleName
    WHERE ur.UserId = @UserId
    ORDER BY rp.PermissionCode;
END
GO
