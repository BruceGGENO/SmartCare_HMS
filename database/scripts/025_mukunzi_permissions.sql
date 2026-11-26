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

-- Catalog for all 12 modules
MERGE dbo.Permission AS t
USING (VALUES
    (N'audit.read', N'View audit log'),
    (N'outbox.read', N'View outbox stub messages'),
    (N'documents.upload', N'Upload documents'),
    (N'documents.read', N'Read/download documents'),
    (N'patients.read', N'View patient records and EHR'),
    (N'patients.manage', N'Register patients, record allergies, diagnoses, treatment plans'),
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
    (N'portal.otp.login', N'Patient: OTP login'),
    (N'staff.read', N'View staff profiles and rosters'),
    (N'billing.read', N'View billing and pending invoices'),
    (N'billing.pay', N'Process and record invoice payments'),
    (N'revenue.read', N'View revenue vs usage dashboard'),
    (N'revenue.manage', N'Manage prepaid packages and consume sessions'),
    (N'pharmacy.read', N'View drug catalog and stock batches'),
    (N'pharmacy.dispense', N'Dispense medications and manage inventory'),
    (N'lab.read', N'View lab test catalog and orders'),
    (N'lab.manage', N'Manage lab orders, enter results and verify tests'),
    (N'beds.read', N'View bed inventory and transfer requests'),
    (N'beds.manage', N'Manage bed transfers, assign beds and update status'),
    (N'discharge.read', N'View patient discharge records and turnaround'),
    (N'discharge.manage', N'Initiate, finalize and clear discharges'),
    (N'waitingroom.read', N'View waiting room queue and analytics'),
    (N'waitingroom.manage', N'Record journey events and manual corrections'),
    (N'occupancy.read', N'View predictive bed occupancy forecasts and alerts')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

-- Role -> permission map (roles remain; APIs authorize on permissions)
DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));

INSERT INTO @Map (RoleName, PermissionCode)
VALUES
-- Super Admin: all permissions
(N'Super Admin', N'audit.read'),
(N'Super Admin', N'outbox.read'),
(N'Super Admin', N'documents.upload'),
(N'Super Admin', N'documents.read'),
(N'Super Admin', N'patients.read'),
(N'Super Admin', N'patients.manage'),
(N'Super Admin', N'appointments.read'),
(N'Super Admin', N'appointments.book'),
(N'Super Admin', N'appointments.reschedule'),
(N'Super Admin', N'appointments.cancel'),
(N'Super Admin', N'appointments.checkin'),
(N'Super Admin', N'appointments.patients.read'),
(N'Super Admin', N'appointments.doctors.read'),
(N'Super Admin', N'staff.read'),
(N'Super Admin', N'billing.read'),
(N'Super Admin', N'billing.pay'),
(N'Super Admin', N'revenue.read'),
(N'Super Admin', N'revenue.manage'),
(N'Super Admin', N'pharmacy.read'),
(N'Super Admin', N'pharmacy.dispense'),
(N'Super Admin', N'lab.read'),
(N'Super Admin', N'lab.manage'),
(N'Super Admin', N'beds.read'),
(N'Super Admin', N'beds.manage'),
(N'Super Admin', N'discharge.read'),
(N'Super Admin', N'discharge.manage'),
(N'Super Admin', N'waitingroom.read'),
(N'Super Admin', N'waitingroom.manage'),
(N'Super Admin', N'occupancy.read'),

-- Hospital Admin
(N'Hospital Admin', N'audit.read'),
(N'Hospital Admin', N'outbox.read'),
(N'Hospital Admin', N'documents.upload'),
(N'Hospital Admin', N'documents.read'),
(N'Hospital Admin', N'patients.read'),
(N'Hospital Admin', N'patients.manage'),
(N'Hospital Admin', N'appointments.read'),
(N'Hospital Admin', N'appointments.book'),
(N'Hospital Admin', N'appointments.reschedule'),
(N'Hospital Admin', N'appointments.cancel'),
(N'Hospital Admin', N'appointments.checkin'),
(N'Hospital Admin', N'appointments.patients.read'),
(N'Hospital Admin', N'appointments.doctors.read'),
(N'Hospital Admin', N'staff.read'),
(N'Hospital Admin', N'billing.read'),
(N'Hospital Admin', N'billing.pay'),
(N'Hospital Admin', N'revenue.read'),
(N'Hospital Admin', N'revenue.manage'),
(N'Hospital Admin', N'pharmacy.read'),
(N'Hospital Admin', N'pharmacy.dispense'),
(N'Hospital Admin', N'lab.read'),
(N'Hospital Admin', N'lab.manage'),
(N'Hospital Admin', N'beds.read'),
(N'Hospital Admin', N'beds.manage'),
(N'Hospital Admin', N'discharge.read'),
(N'Hospital Admin', N'discharge.manage'),
(N'Hospital Admin', N'waitingroom.read'),
(N'Hospital Admin', N'waitingroom.manage'),
(N'Hospital Admin', N'occupancy.read'),

-- Doctor
(N'Doctor', N'documents.upload'),
(N'Doctor', N'documents.read'),
(N'Doctor', N'patients.read'),
(N'Doctor', N'patients.manage'),
(N'Doctor', N'appointments.read'),
(N'Doctor', N'appointments.patients.read'),
(N'Doctor', N'appointments.doctors.read'),
(N'Doctor', N'lab.read'),
(N'Doctor', N'lab.manage'),
(N'Doctor', N'pharmacy.read'),
(N'Doctor', N'beds.read'),
(N'Doctor', N'beds.manage'),
(N'Doctor', N'discharge.read'),
(N'Doctor', N'discharge.manage'),
(N'Doctor', N'waitingroom.read'),
(N'Doctor', N'occupancy.read'),

-- Nurse
(N'Nurse', N'documents.upload'),
(N'Nurse', N'documents.read'),
(N'Nurse', N'patients.read'),
(N'Nurse', N'patients.manage'),
(N'Nurse', N'appointments.read'),
(N'Nurse', N'appointments.checkin'),
(N'Nurse', N'appointments.patients.read'),
(N'Nurse', N'beds.read'),
(N'Nurse', N'beds.manage'),
(N'Nurse', N'discharge.read'),
(N'Nurse', N'discharge.manage'),
(N'Nurse', N'waitingroom.read'),
(N'Nurse', N'waitingroom.manage'),
(N'Nurse', N'lab.read'),
(N'Nurse', N'occupancy.read'),

-- Receptionist
(N'Receptionist', N'patients.read'),
(N'Receptionist', N'patients.manage'),
(N'Receptionist', N'appointments.read'),
(N'Receptionist', N'appointments.book'),
(N'Receptionist', N'appointments.reschedule'),
(N'Receptionist', N'appointments.cancel'),
(N'Receptionist', N'appointments.checkin'),
(N'Receptionist', N'appointments.patients.read'),
(N'Receptionist', N'appointments.doctors.read'),
(N'Receptionist', N'waitingroom.read'),
(N'Receptionist', N'waitingroom.manage'),
(N'Receptionist', N'staff.read'),

-- Lab Technician
(N'Lab Technician', N'lab.read'),
(N'Lab Technician', N'lab.manage'),
(N'Lab Technician', N'patients.read'),
(N'Lab Technician', N'documents.upload'),
(N'Lab Technician', N'documents.read'),
(N'Lab Technician', N'discharge.read'),
(N'Lab Technician', N'discharge.manage'),

-- Pharmacist
(N'Pharmacist', N'pharmacy.read'),
(N'Pharmacist', N'pharmacy.dispense'),
(N'Pharmacist', N'patients.read'),
(N'Pharmacist', N'discharge.read'),
(N'Pharmacist', N'discharge.manage'),

-- Billing Staff
(N'Billing Staff', N'billing.read'),
(N'Billing Staff', N'billing.pay'),
(N'Billing Staff', N'revenue.read'),
(N'Billing Staff', N'revenue.manage'),
(N'Billing Staff', N'patients.read'),
(N'Billing Staff', N'discharge.read'),
(N'Billing Staff', N'discharge.manage'),

-- Patient
(N'Patient', N'portal.appointments.read'),
(N'Patient', N'portal.history.read'),
(N'Patient', N'portal.packages.read'),
(N'Patient', N'portal.otp.login'),
(N'Patient', N'appointments.read'),
(N'Patient', N'appointments.book');

MERGE dbo.RolePermission AS target
USING @Map AS source
ON target.RoleName = source.RoleName AND target.PermissionCode = source.PermissionCode
WHEN NOT MATCHED THEN
    INSERT (RoleName, PermissionCode) VALUES (source.RoleName, source.PermissionCode);
GO
