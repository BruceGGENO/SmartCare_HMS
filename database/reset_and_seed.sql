IF DB_ID(N'SmartCareHMS_Elvin') IS NULL 
BEGIN
    CREATE DATABASE [SmartCareHMS_Elvin];
END
GO
USE [SmartCareHMS_Elvin];
GO

/* =====================================================================
   SmartCare HMS - Unified Database Setup & Seed Script
   Separated Architecture: ASP.NET Core Web API + React Frontend
   Permission-Based Authorization Model
   Generated for SmartCare Development Team (Elvin, Jonathan, Arthur, Bruce, Raissa)
   ===================================================================== */

/* ---------- BEGIN: 010_mukunzi_authentication.sql ---------- */

-- SmartCare HMS
-- 010_mukunzi_authentication.sql
-- Owner: Mukunzi Jonathan (Back-end Lead)
-- Include this file in reset_and_seed.sql (Elvin).
-- Do not add USE <database>; run on SmartCareHMS_<name>.
-- Contracts:
--   dbo.sp_GetUserByUserName, dbo.sp_GetUserRoles
--   dbo.sp_InsertUser, dbo.sp_InsertUserRole, dbo.sp_CountUsers
--   dbo.sp_UpdateLoginFailure, dbo.sp_ResetLoginFailure
--   dbo.sp_InsertOtp, dbo.sp_GetLatestOpenOtp, dbo.sp_ConsumeOtp
--   dbo.sp_InsertOutbox, dbo.sp_ListOutbox
-- Demo users are seeded by the API in Development (password Demo@12345).

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Role
    (
        RoleName NVARCHAR(50) NOT NULL
            CONSTRAINT PK_Role PRIMARY KEY
    );
END
GO

IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        UserId           UNIQUEIDENTIFIER  NOT NULL
            CONSTRAINT PK_User PRIMARY KEY,
        UserName         NVARCHAR(100)     NOT NULL,
        DisplayName      NVARCHAR(200)     NOT NULL,
        PasswordHash     NVARCHAR(200)     NOT NULL,
        PasswordSalt     NVARCHAR(200)     NOT NULL,
        IsActive         BIT               NOT NULL
            CONSTRAINT DF_User_IsActive DEFAULT (1),
        FailedLoginCount INT               NOT NULL
            CONSTRAINT DF_User_FailedLoginCount DEFAULT (0),
        LockoutEndUtc    DATETIMEOFFSET(7) NULL,
        CreatedAtUtc     DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_User_CreatedAtUtc DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT UQ_User_UserName UNIQUE (UserName)
    );
END
GO

IF OBJECT_ID(N'dbo.UserRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRole
    (
        UserId   UNIQUEIDENTIFIER NOT NULL,
        RoleName NVARCHAR(50)     NOT NULL,
        CONSTRAINT PK_UserRole PRIMARY KEY (UserId, RoleName),
        CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES dbo.[User] (UserId),
        CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleName) REFERENCES dbo.Role (RoleName)
    );
END
GO

IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OtpChallenge
    (
        OtpId         UNIQUEIDENTIFIER  NOT NULL
            CONSTRAINT PK_OtpChallenge PRIMARY KEY,
        UserId        UNIQUEIDENTIFIER  NOT NULL,
        CodeHash      NVARCHAR(200)     NOT NULL,
        CodeSalt      NVARCHAR(200)     NOT NULL,
        Purpose       NVARCHAR(50)      NOT NULL,
        ExpiresAtUtc  DATETIMEOFFSET(7) NOT NULL,
        ConsumedAtUtc DATETIMEOFFSET(7) NULL,
        CreatedAtUtc  DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Otp_CreatedAtUtc DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_Otp_User FOREIGN KEY (UserId) REFERENCES dbo.[User] (UserId)
    );

    CREATE INDEX IX_Otp_User_Open
        ON dbo.OtpChallenge (UserId, Purpose, ConsumedAtUtc, ExpiresAtUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.Outbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Outbox
    (
        OutboxId     BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Outbox PRIMARY KEY,
        Channel      NVARCHAR(20)         NOT NULL,
        Recipient    NVARCHAR(200)        NOT NULL,
        Subject      NVARCHAR(200)        NOT NULL,
        Body         NVARCHAR(1000)       NOT NULL,
        Status       NVARCHAR(20)         NOT NULL
            CONSTRAINT DF_Outbox_Status DEFAULT (N'Stubbed'),
        CreatedAtUtc DATETIMEOFFSET(7)    NOT NULL
            CONSTRAINT DF_Outbox_CreatedAtUtc DEFAULT (SYSDATETIMEOFFSET())
    );
END
GO

INSERT INTO dbo.Role (RoleName)
SELECT v.RoleName
FROM (VALUES
    (N'Super Admin'),
    (N'Hospital Admin'),
    (N'Doctor'),
    (N'Nurse'),
    (N'Receptionist'),
    (N'Lab Technician'),
    (N'Pharmacist'),
    (N'Billing Staff'),
    (N'Patient')
) AS v (RoleName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = v.RoleName);
GO

IF OBJECT_ID(N'dbo.sp_CountUsers', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_CountUsers;
GO
CREATE PROCEDURE dbo.sp_CountUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.[User];
END
GO

IF OBJECT_ID(N'dbo.sp_GetUserByUserName', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUserByUserName;
GO
CREATE PROCEDURE dbo.sp_GetUserByUserName
    @UserName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserId,
        UserName,
        DisplayName,
        PasswordHash,
        PasswordSalt,
        IsActive,
        FailedLoginCount,
        LockoutEndUtc
    FROM dbo.[User]
    WHERE UserName = @UserName;
END
GO

IF OBJECT_ID(N'dbo.sp_GetUserRoles', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUserRoles;
GO
CREATE PROCEDURE dbo.sp_GetUserRoles
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoleName
    FROM dbo.UserRole
    WHERE UserId = @UserId;
END
GO

IF OBJECT_ID(N'dbo.sp_InsertUser', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertUser;
GO
CREATE PROCEDURE dbo.sp_InsertUser
    @UserId       UNIQUEIDENTIFIER,
    @UserName     NVARCHAR(100),
    @DisplayName  NVARCHAR(200),
    @PasswordHash NVARCHAR(200),
    @PasswordSalt NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[User] (UserId, UserName, DisplayName, PasswordHash, PasswordSalt)
    VALUES (@UserId, @UserName, @DisplayName, @PasswordHash, @PasswordSalt);
END
GO

IF OBJECT_ID(N'dbo.sp_InsertUserRole', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertUserRole;
GO
CREATE PROCEDURE dbo.sp_InsertUserRole
    @UserId   UNIQUEIDENTIFIER,
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.UserRole (UserId, RoleName)
    VALUES (@UserId, @RoleName);
END
GO

IF OBJECT_ID(N'dbo.sp_UpdateLoginFailure', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateLoginFailure;
GO
CREATE PROCEDURE dbo.sp_UpdateLoginFailure
    @UserId           UNIQUEIDENTIFIER,
    @FailedLoginCount INT,
    @LockoutEndUtc    DATETIMEOFFSET = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.[User]
    SET FailedLoginCount = @FailedLoginCount,
        LockoutEndUtc = @LockoutEndUtc
    WHERE UserId = @UserId;
END
GO

IF OBJECT_ID(N'dbo.sp_ResetLoginFailure', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ResetLoginFailure;
GO
CREATE PROCEDURE dbo.sp_ResetLoginFailure
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.[User]
    SET FailedLoginCount = 0,
        LockoutEndUtc = NULL
    WHERE UserId = @UserId;
END
GO

IF OBJECT_ID(N'dbo.sp_InsertOtp', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertOtp;
GO
CREATE PROCEDURE dbo.sp_InsertOtp
    @OtpId        UNIQUEIDENTIFIER,
    @UserId       UNIQUEIDENTIFIER,
    @CodeHash     NVARCHAR(200),
    @CodeSalt     NVARCHAR(200),
    @Purpose      NVARCHAR(50),
    @ExpiresAtUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OtpChallenge (OtpId, UserId, CodeHash, CodeSalt, Purpose, ExpiresAtUtc)
    VALUES (@OtpId, @UserId, @CodeHash, @CodeSalt, @Purpose, @ExpiresAtUtc);
END
GO

IF OBJECT_ID(N'dbo.sp_GetLatestOpenOtp', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetLatestOpenOtp;
GO
CREATE PROCEDURE dbo.sp_GetLatestOpenOtp
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        OtpId,
        CodeHash,
        CodeSalt,
        ExpiresAtUtc
    FROM dbo.OtpChallenge
    WHERE UserId = @UserId
      AND Purpose = N'Login'
      AND ConsumedAtUtc IS NULL
    ORDER BY CreatedAtUtc DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_ConsumeOtp', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsumeOtp;
GO
CREATE PROCEDURE dbo.sp_ConsumeOtp
    @OtpId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OtpChallenge
    SET ConsumedAtUtc = SYSDATETIMEOFFSET()
    WHERE OtpId = @OtpId
      AND ConsumedAtUtc IS NULL;
END
GO

IF OBJECT_ID(N'dbo.sp_InsertOutbox', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertOutbox;
GO
CREATE PROCEDURE dbo.sp_InsertOutbox
    @Channel   NVARCHAR(20),
    @Recipient NVARCHAR(200),
    @Subject   NVARCHAR(200),
    @Body      NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Outbox (Channel, Recipient, Subject, Body, Status)
    VALUES (@Channel, @Recipient, @Subject, @Body, N'Stubbed');
END
GO

IF OBJECT_ID(N'dbo.sp_ListOutbox', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListOutbox;
GO
CREATE PROCEDURE dbo.sp_ListOutbox
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (50)
        OutboxId,
        Channel,
        Recipient,
        Subject,
        Body,
        Status,
        CreatedAtUtc
    FROM dbo.Outbox
    ORDER BY OutboxId DESC;
END
GO


/* ---------- END: 010_mukunzi_authentication.sql ---------- */
GO

/* ---------- BEGIN: 020_mukunzi_appointments.sql ---------- */

-- SmartCare HMS
-- 020_mukunzi_appointments.sql
-- Owner: Mukunzi Jonathan (covering Raissa's MOD-02 / MOD-10)
-- Requires 010_mukunzi_authentication.sql (dbo.[User] / dbo.UserRole).
-- Include in reset_and_seed.sql (Elvin).
-- Do not add USE <database>; run this on SmartCareHMS_<name>.`r`n-- If using sqlcmd, pass -I (QUOTED_IDENTIFIER ON) for filtered indexes.

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Patient', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Patient
    (
        PatientId   INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Patient PRIMARY KEY,
        UserId      UNIQUEIDENTIFIER  NULL,
        Mrn         NVARCHAR(20)      NOT NULL,
        FullName    NVARCHAR(200)     NOT NULL,
        DateOfBirth DATE              NULL,
        Phone       NVARCHAR(30)      NULL,
        CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Patient_CreatedAtUtc DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT UQ_Patient_Mrn UNIQUE (Mrn)
    );

    CREATE INDEX IX_Patient_UserId ON dbo.Patient (UserId);
END
GO

IF OBJECT_ID(N'dbo.Appointment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointment
    (
        AppointmentId  INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Appointment PRIMARY KEY,
        PublicId       UNIQUEIDENTIFIER  NOT NULL
            CONSTRAINT DF_Appointment_PublicId DEFAULT (NEWID()),
        PatientId      INT               NOT NULL,
        DoctorUserId   UNIQUEIDENTIFIER  NOT NULL,
        Department     NVARCHAR(100)     NOT NULL,
        ScheduledAtUtc DATETIME2(0)      NOT NULL,
        Status         NVARCHAR(30)      NOT NULL
            CONSTRAINT DF_Appointment_Status DEFAULT (N'Booked'),
        CancelReason   NVARCHAR(300)     NULL,
        CheckInCode    CHAR(8)           NOT NULL,
        CreatedAtUtc   DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Appointment_CreatedAtUtc DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_Appointment_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId),
        CONSTRAINT FK_Appointment_Doctor FOREIGN KEY (DoctorUserId) REFERENCES dbo.[User] (UserId)
    );

    CREATE UNIQUE INDEX UX_Appointment_DoctorSlot
        ON dbo.Appointment (DoctorUserId, ScheduledAtUtc)
        WHERE [Status] IN (N'Booked', N'CheckedIn');

    CREATE UNIQUE INDEX UX_Appointment_CheckInCode
        ON dbo.Appointment (CheckInCode);
END
GO

IF OBJECT_ID(N'dbo.ReleasedClinicalNote', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReleasedClinicalNote
    (
        NoteId       INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ReleasedClinicalNote PRIMARY KEY,
        PatientId    INT               NOT NULL,
        AuthorName   NVARCHAR(200)     NOT NULL,
        Body         NVARCHAR(1000)    NOT NULL,
        CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Note_CreatedAtUtc DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT FK_Note_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId)
    );
END
GO

IF OBJECT_ID(N'dbo.TreatmentPackage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TreatmentPackage
    (
        PackagePurchaseId  INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TreatmentPackage PRIMARY KEY,
        PatientId          INT               NOT NULL,
        Name               NVARCHAR(200)     NOT NULL,
        SessionsPurchased  INT               NOT NULL,
        SessionsConsumed   INT               NOT NULL
            CONSTRAINT DF_Package_Consumed DEFAULT (0),
        CONSTRAINT FK_Package_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (PatientId)
    );
END
GO

IF OBJECT_ID(N'dbo.sp_ListPatients', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListPatients;
GO
CREATE PROCEDURE dbo.sp_ListPatients
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PatientId, Mrn, FullName
    FROM dbo.Patient
    ORDER BY FullName;
END
GO

IF OBJECT_ID(N'dbo.sp_ListDoctors', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListDoctors;
GO
CREATE PROCEDURE dbo.sp_ListDoctors
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.DisplayName
    FROM dbo.[User] u
    INNER JOIN dbo.UserRole r ON r.UserId = u.UserId
    WHERE r.RoleName = N'Doctor' AND u.IsActive = 1
    ORDER BY u.DisplayName;
END
GO

IF OBJECT_ID(N'dbo.sp_FindPatientIdByUserId', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_FindPatientIdByUserId;
GO
CREATE PROCEDURE dbo.sp_FindPatientIdByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) PatientId
    FROM dbo.Patient
    WHERE UserId = @UserId;
END
GO

IF OBJECT_ID(N'dbo.sp_CountPatients', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_CountPatients;
GO
CREATE PROCEDURE dbo.sp_CountPatients
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Patient;
END
GO

IF OBJECT_ID(N'dbo.sp_InsertPatient', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertPatient;
GO
CREATE PROCEDURE dbo.sp_InsertPatient
    @UserId      UNIQUEIDENTIFIER = NULL,
    @Mrn         NVARCHAR(20),
    @FullName    NVARCHAR(200),
    @DateOfBirth DATE = NULL,
    @Phone       NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Patient (UserId, Mrn, FullName, DateOfBirth, Phone)
    VALUES (@UserId, @Mrn, @FullName, @DateOfBirth, @Phone);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS PatientId;
END
GO

IF OBJECT_ID(N'dbo.sp_ListAppointments', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListAppointments;
GO
CREATE PROCEDURE dbo.sp_ListAppointments
    @DoctorUserId UNIQUEIDENTIFIER = NULL,
    @PatientId    INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.PublicId,
        a.PatientId,
        p.FullName AS PatientName,
        p.Mrn AS PatientMrn,
        a.DoctorUserId,
        u.DisplayName AS DoctorName,
        a.Department,
        a.ScheduledAtUtc,
        a.Status,
        a.CancelReason,
        a.CheckInCode
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE (@DoctorUserId IS NULL OR a.DoctorUserId = @DoctorUserId)
      AND (@PatientId IS NULL OR a.PatientId = @PatientId)
    ORDER BY a.ScheduledAtUtc DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_GetAppointmentById', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAppointmentById;
GO
CREATE PROCEDURE dbo.sp_GetAppointmentById
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.PublicId,
        a.PatientId,
        p.FullName AS PatientName,
        p.Mrn AS PatientMrn,
        a.DoctorUserId,
        u.DisplayName AS DoctorName,
        a.Department,
        a.ScheduledAtUtc,
        a.Status,
        a.CancelReason,
        a.CheckInCode
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

IF OBJECT_ID(N'dbo.sp_InsertAppointment', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertAppointment;
GO
CREATE PROCEDURE dbo.sp_InsertAppointment
    @PublicId       UNIQUEIDENTIFIER,
    @PatientId      INT,
    @DoctorUserId   UNIQUEIDENTIFIER,
    @Department     NVARCHAR(100),
    @ScheduledAtUtc DATETIME2(0),
    @CheckInCode    CHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Appointment
        (PublicId, PatientId, DoctorUserId, Department, ScheduledAtUtc, Status, CheckInCode)
    VALUES
        (@PublicId, @PatientId, @DoctorUserId, @Department, @ScheduledAtUtc, N'Booked', @CheckInCode);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS AppointmentId;
END
GO

IF OBJECT_ID(N'dbo.sp_RescheduleAppointment', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RescheduleAppointment;
GO
CREATE PROCEDURE dbo.sp_RescheduleAppointment
    @AppointmentId  INT,
    @ScheduledAtUtc DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Appointment
    SET ScheduledAtUtc = @ScheduledAtUtc
    WHERE AppointmentId = @AppointmentId
      AND Status IN (N'Booked', N'CheckedIn');
END
GO

IF OBJECT_ID(N'dbo.sp_CancelAppointment', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_CancelAppointment;
GO
CREATE PROCEDURE dbo.sp_CancelAppointment
    @AppointmentId INT,
    @CancelReason  NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Appointment
    SET Status = N'Cancelled',
        CancelReason = @CancelReason
    WHERE AppointmentId = @AppointmentId
      AND Status IN (N'Booked', N'CheckedIn');
END
GO

IF OBJECT_ID(N'dbo.sp_CheckInAppointment', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_CheckInAppointment;
GO
CREATE PROCEDURE dbo.sp_CheckInAppointment
    @CheckInCode CHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id INT;
    SELECT @Id = AppointmentId
    FROM dbo.Appointment
    WHERE CheckInCode = @CheckInCode
      AND Status = N'Booked';

    IF @Id IS NULL
        RETURN;

    UPDATE dbo.Appointment
    SET Status = N'CheckedIn'
    WHERE AppointmentId = @Id;

    SELECT
        a.AppointmentId,
        a.PublicId,
        a.PatientId,
        p.FullName AS PatientName,
        p.Mrn AS PatientMrn,
        a.DoctorUserId,
        u.DisplayName AS DoctorName,
        a.Department,
        a.ScheduledAtUtc,
        a.Status,
        a.CancelReason,
        a.CheckInCode
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE a.AppointmentId = @Id;
END
GO

IF OBJECT_ID(N'dbo.sp_ListAppointmentHistory', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListAppointmentHistory;
GO
CREATE PROCEDURE dbo.sp_ListAppointmentHistory
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.PublicId,
        a.PatientId,
        p.FullName AS PatientName,
        p.Mrn AS PatientMrn,
        a.DoctorUserId,
        u.DisplayName AS DoctorName,
        a.Department,
        a.ScheduledAtUtc,
        a.Status,
        a.CancelReason,
        a.CheckInCode
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE a.PatientId = @PatientId
      AND a.Status IN (N'Completed', N'CheckedIn', N'Cancelled', N'NoShow', N'Booked')
    ORDER BY a.ScheduledAtUtc DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_ListPatientPackages', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListPatientPackages;
GO
CREATE PROCEDURE dbo.sp_ListPatientPackages
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PackagePurchaseId, Name, SessionsPurchased, SessionsConsumed
    FROM dbo.TreatmentPackage
    WHERE PatientId = @PatientId
    ORDER BY PackagePurchaseId DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_ListReleasedNotes', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListReleasedNotes;
GO
CREATE PROCEDURE dbo.sp_ListReleasedNotes
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NoteId, CreatedAtUtc, AuthorName, Body
    FROM dbo.ReleasedClinicalNote
    WHERE PatientId = @PatientId
    ORDER BY CreatedAtUtc DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_InsertTreatmentPackage', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertTreatmentPackage;
GO
CREATE PROCEDURE dbo.sp_InsertTreatmentPackage
    @PatientId         INT,
    @Name              NVARCHAR(200),
    @SessionsPurchased INT,
    @SessionsConsumed  INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TreatmentPackage (PatientId, Name, SessionsPurchased, SessionsConsumed)
    VALUES (@PatientId, @Name, @SessionsPurchased, @SessionsConsumed);
END
GO

IF OBJECT_ID(N'dbo.sp_InsertReleasedNote', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertReleasedNote;
GO
CREATE PROCEDURE dbo.sp_InsertReleasedNote
    @PatientId  INT,
    @AuthorName NVARCHAR(200),
    @Body       NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ReleasedClinicalNote (PatientId, AuthorName, Body)
    VALUES (@PatientId, @AuthorName, @Body);
END
GO


/* ---------- END: 020_mukunzi_appointments.sql ---------- */
GO

/* ---------- BEGIN: 025_mukunzi_permissions.sql ---------- */
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

/* ---------- END: 025_mukunzi_permissions.sql ---------- */
GO

/* ---------- BEGIN: 030_mukunzi_audit_log.sql ---------- */

-- SmartCare HMS
-- 030_mukunzi_audit_log.sql
-- Owner: Mukunzi Jonathan
-- Include in reset_and_seed.sql (Elvin).
-- Contract: dbo.sp_WriteAuditLog, dbo.sp_ListRecentAuditLogs

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        AuditLogId     BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AuditLog PRIMARY KEY,
        Action         NVARCHAR(100)       NOT NULL,
        EntityType     NVARCHAR(100)       NOT NULL,
        EntityId       NVARCHAR(64)        NULL,
        ActorUserId    NVARCHAR(64)        NULL,
        Outcome        NVARCHAR(50)        NOT NULL,
        OccurredAtUtc  DATETIMEOFFSET(7)   NOT NULL,
        CorrelationId  NVARCHAR(64)        NULL
    );

    CREATE INDEX IX_AuditLog_Entity
        ON dbo.AuditLog (EntityType, EntityId, OccurredAtUtc DESC);

    CREATE INDEX IX_AuditLog_Actor
        ON dbo.AuditLog (ActorUserId, OccurredAtUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.sp_WriteAuditLog', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_WriteAuditLog;
GO
CREATE PROCEDURE dbo.sp_WriteAuditLog
    @Action        NVARCHAR(100),
    @EntityType    NVARCHAR(100),
    @EntityId      NVARCHAR(64) = NULL,
    @ActorUserId   NVARCHAR(64) = NULL,
    @Outcome       NVARCHAR(50),
    @OccurredAtUtc DATETIMEOFFSET,
    @CorrelationId NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog
    (
        Action,
        EntityType,
        EntityId,
        ActorUserId,
        Outcome,
        OccurredAtUtc,
        CorrelationId
    )
    VALUES
    (
        @Action,
        @EntityType,
        @EntityId,
        @ActorUserId,
        @Outcome,
        @OccurredAtUtc,
        @CorrelationId
    );
END
GO

IF OBJECT_ID(N'dbo.sp_ListRecentAuditLogs', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ListRecentAuditLogs;
GO
CREATE PROCEDURE dbo.sp_ListRecentAuditLogs
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF @Take IS NULL OR @Take < 1 SET @Take = 50;
    IF @Take > 200 SET @Take = 200;

    SELECT TOP (@Take)
        AuditLogId,
        Action,
        EntityType,
        EntityId,
        ActorUserId,
        Outcome,
        OccurredAtUtc,
        CorrelationId
    FROM dbo.AuditLog
    ORDER BY AuditLogId DESC;
END
GO


/* ---------- END: 030_mukunzi_audit_log.sql ---------- */
GO

/* ---------- BEGIN: 040_mukunzi_document_storage.sql ---------- */

-- SmartCare HMS
-- 040_mukunzi_document_storage.sql
-- Owner: Mukunzi Jonathan
-- Include in reset_and_seed.sql (Elvin).
-- Contract: dbo.sp_InsertDocument, dbo.sp_GetDocumentById
-- Files live on disk under App_Data/uploads (GUID names), not in SQL.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Document', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Document
    (
        DocumentId        UNIQUEIDENTIFIER  NOT NULL
            CONSTRAINT PK_Document PRIMARY KEY,
        OriginalFileName  NVARCHAR(255)     NOT NULL,
        ContentType       NVARCHAR(100)     NOT NULL,
        SizeBytes         INT               NOT NULL,
        StoredFileName    NVARCHAR(260)     NOT NULL,
        EntityType        NVARCHAR(100)     NULL,
        EntityId          NVARCHAR(64)      NULL,
        UploadedByUserId  NVARCHAR(64)      NULL,
        UploadedAtUtc     DATETIMEOFFSET(7) NOT NULL
    );

    CREATE INDEX IX_Document_Entity
        ON dbo.Document (EntityType, EntityId, UploadedAtUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.sp_InsertDocument', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_InsertDocument;
GO
CREATE PROCEDURE dbo.sp_InsertDocument
    @DocumentId       UNIQUEIDENTIFIER,
    @OriginalFileName NVARCHAR(255),
    @ContentType      NVARCHAR(100),
    @SizeBytes        INT,
    @StoredFileName   NVARCHAR(260),
    @EntityType       NVARCHAR(100) = NULL,
    @EntityId         NVARCHAR(64) = NULL,
    @UploadedByUserId NVARCHAR(64) = NULL,
    @UploadedAtUtc    DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Document
    (
        DocumentId,
        OriginalFileName,
        ContentType,
        SizeBytes,
        StoredFileName,
        EntityType,
        EntityId,
        UploadedByUserId,
        UploadedAtUtc
    )
    VALUES
    (
        @DocumentId,
        @OriginalFileName,
        @ContentType,
        @SizeBytes,
        @StoredFileName,
        @EntityType,
        @EntityId,
        @UploadedByUserId,
        @UploadedAtUtc
    );
END
GO

IF OBJECT_ID(N'dbo.sp_GetDocumentById', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetDocumentById;
GO
CREATE PROCEDURE dbo.sp_GetDocumentById
    @DocumentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DocumentId,
        OriginalFileName,
        ContentType,
        SizeBytes,
        StoredFileName,
        EntityType,
        EntityId,
        UploadedByUserId,
        UploadedAtUtc
    FROM dbo.Document
    WHERE DocumentId = @DocumentId;
END
GO


/* ---------- END: 040_mukunzi_document_storage.sql ---------- */
GO

/* ---------- BEGIN: 050_arthur_billing_and_revenue.sql ---------- */

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
IF OBJECT_ID(N'dbo.sp_GetPendingInvoices', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetPendingInvoices;
GO
CREATE PROCEDURE dbo.sp_GetPendingInvoices
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

IF OBJECT_ID(N'dbo.sp_GetInvoiceById', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetInvoiceById;
GO
CREATE PROCEDURE dbo.sp_GetInvoiceById
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

IF OBJECT_ID(N'dbo.sp_GetInvoiceLines', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetInvoiceLines;
GO
CREATE PROCEDURE dbo.sp_GetInvoiceLines
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

IF OBJECT_ID(N'dbo.sp_RecordPayment', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RecordPayment;
GO
CREATE PROCEDURE dbo.sp_RecordPayment
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

IF OBJECT_ID(N'dbo.sp_GetRevenueSummary', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetRevenueSummary;
GO
CREATE PROCEDURE dbo.sp_GetRevenueSummary
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

IF OBJECT_ID(N'dbo.sp_GetAllPackages', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAllPackages;
GO
CREATE PROCEDURE dbo.sp_GetAllPackages
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

IF OBJECT_ID(N'dbo.sp_GetRecentPackageUsages', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetRecentPackageUsages;
GO
CREATE PROCEDURE dbo.sp_GetRecentPackageUsages
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

IF OBJECT_ID(N'dbo.sp_PurchasePackage', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_PurchasePackage;
GO
CREATE PROCEDURE dbo.sp_PurchasePackage
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

IF OBJECT_ID(N'dbo.sp_ConsumePackageSession', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsumePackageSession;
GO
CREATE PROCEDURE dbo.sp_ConsumePackageSession
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


/* ---------- END: 050_arthur_billing_and_revenue.sql ---------- */
GO

