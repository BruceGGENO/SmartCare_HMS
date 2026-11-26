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

CREATE OR ALTER PROCEDURE dbo.sp_ListPatients
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PatientId, Mrn, FullName
    FROM dbo.Patient
    ORDER BY FullName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListDoctors
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

CREATE OR ALTER PROCEDURE dbo.sp_FindPatientIdByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) PatientId
    FROM dbo.Patient
    WHERE UserId = @UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CountPatients
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Patient;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InsertPatient
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

CREATE OR ALTER PROCEDURE dbo.sp_ListAppointments
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

CREATE OR ALTER PROCEDURE dbo.sp_GetAppointmentById
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

CREATE OR ALTER PROCEDURE dbo.sp_InsertAppointment
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

CREATE OR ALTER PROCEDURE dbo.sp_RescheduleAppointment
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

CREATE OR ALTER PROCEDURE dbo.sp_CancelAppointment
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

CREATE OR ALTER PROCEDURE dbo.sp_CheckInAppointment
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

CREATE OR ALTER PROCEDURE dbo.sp_ListAppointmentHistory
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

CREATE OR ALTER PROCEDURE dbo.sp_ListPatientPackages
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

CREATE OR ALTER PROCEDURE dbo.sp_ListReleasedNotes
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

CREATE OR ALTER PROCEDURE dbo.sp_InsertTreatmentPackage
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

CREATE OR ALTER PROCEDURE dbo.sp_InsertReleasedNote
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
