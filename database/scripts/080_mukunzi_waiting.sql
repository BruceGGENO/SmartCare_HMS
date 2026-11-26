-- SmartCare HMS — 080 Waiting room (extends Appointment)
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.Appointment', N'QueueStation') IS NULL
    ALTER TABLE dbo.Appointment ADD QueueStation NVARCHAR(30) NULL;
GO
IF COL_LENGTH(N'dbo.Appointment', N'QueueEnteredAtUtc') IS NULL
    ALTER TABLE dbo.Appointment ADD QueueEnteredAtUtc DATETIME2(0) NULL;
GO
IF COL_LENGTH(N'dbo.Appointment', N'CalledAtUtc') IS NULL
    ALTER TABLE dbo.Appointment ADD CalledAtUtc DATETIME2(0) NULL;
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'waiting.read', N'View waiting-room queue'),
    (N'waiting.manage', N'Call patients and update queue station')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'waiting.read'), (N'Super Admin', N'waiting.manage'),
(N'Hospital Admin', N'waiting.read'), (N'Hospital Admin', N'waiting.manage'),
(N'Receptionist', N'waiting.read'), (N'Receptionist', N'waiting.manage'),
(N'Nurse', N'waiting.read'), (N'Nurse', N'waiting.manage'),
(N'Doctor', N'waiting.read'), (N'Doctor', N'waiting.manage');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ListWaitingQueue
    @Department NVARCHAR(100) = NULL,
    @Station NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId, a.PublicId, a.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
        a.DoctorUserId, u.DisplayName AS DoctorName, a.Department, a.ScheduledAtUtc, a.Status,
        a.CheckInCode, a.QueueStation, a.QueueEnteredAtUtc, a.CalledAtUtc
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE a.Status IN (N'CheckedIn', N'InQueue', N'Called')
      AND (@Department IS NULL OR a.Department = @Department)
      AND (@Station IS NULL OR a.QueueStation = @Station)
    ORDER BY ISNULL(a.QueueEnteredAtUtc, a.ScheduledAtUtc);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EnterWaitingQueue
    @AppointmentId INT,
    @Station NVARCHAR(30) = N'Triage'
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Appointment WHERE AppointmentId = @AppointmentId)
        THROW 50061, N'Appointment not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Appointment WHERE AppointmentId = @AppointmentId AND Status IN (N'CheckedIn', N'InQueue', N'Called'))
        THROW 50062, N'Patient must be checked in before joining the queue.', 1;

    UPDATE dbo.Appointment
    SET Status = N'InQueue',
        QueueStation = @Station,
        QueueEnteredAtUtc = ISNULL(QueueEnteredAtUtc, SYSUTCDATETIME()),
        CalledAtUtc = NULL
    WHERE AppointmentId = @AppointmentId;

    SELECT
        a.AppointmentId, a.PublicId, a.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
        a.DoctorUserId, u.DisplayName AS DoctorName, a.Department, a.ScheduledAtUtc, a.Status,
        a.CheckInCode, a.QueueStation, a.QueueEnteredAtUtc, a.CalledAtUtc
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CallWaitingPatient @AppointmentId INT AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Appointment WHERE AppointmentId = @AppointmentId)
        THROW 50061, N'Appointment not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Appointment WHERE AppointmentId = @AppointmentId AND Status IN (N'CheckedIn', N'InQueue'))
        THROW 50063, N'Appointment is not waiting to be called.', 1;

    UPDATE dbo.Appointment
    SET Status = N'Called',
        CalledAtUtc = SYSUTCDATETIME(),
        QueueStation = ISNULL(QueueStation, N'Doctor')
    WHERE AppointmentId = @AppointmentId;

    SELECT
        a.AppointmentId, a.PublicId, a.PatientId, p.FullName AS PatientName, p.Mrn AS PatientMrn,
        a.DoctorUserId, u.DisplayName AS DoctorName, a.Department, a.ScheduledAtUtc, a.Status,
        a.CheckInCode, a.QueueStation, a.QueueEnteredAtUtc, a.CalledAtUtc
    FROM dbo.Appointment a
    INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
    INNER JOIN dbo.[User] u ON u.UserId = a.DoctorUserId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

-- After check-in, set queue defaults (extend existing proc)
CREATE OR ALTER PROCEDURE dbo.sp_CheckInAppointment
    @CheckInCode CHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Appointment
    SET Status = N'CheckedIn',
        QueueStation = ISNULL(QueueStation, N'Registration'),
        QueueEnteredAtUtc = ISNULL(QueueEnteredAtUtc, SYSUTCDATETIME())
    WHERE CheckInCode = @CheckInCode
      AND Status = N'Booked';

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
    WHERE a.CheckInCode = @CheckInCode
      AND a.Status = N'CheckedIn';
END
GO
