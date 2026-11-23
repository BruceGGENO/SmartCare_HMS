/* =====================================================================
   SmartCare HMS - complete database script for SQL Server (SSMS)
   Run the WHOLE file once on an empty server: press Execute (F5).
   Part 1: database + all tables + indexes (same SQL that EF Core generated)
   Part 2A: Mukunzi's 010 audit log + 020 document storage (dbo.AuditLog, dbo.Document, their procs)
   Part 2: roles and departments
   Part 3: demo users, staff, test patient and visit (optional, delete if not wanted)
   Scope: Elvin's part only - MOD-08 Discharge, MOD-09 Waiting Room, MOD-03 staff/roster seed,
          plus login (Identity) and the shared tables they depend on.
   Demo password for every demo user:  ChangeMe-Dev#2026   (development only!)
   ===================================================================== */

IF DB_ID(N'SmartCareHms') IS NULL CREATE DATABASE [SmartCareHms];
GO
USE [SmartCareHms];
GO
IF OBJECT_ID(N'dbo.Patients', N'U') IS NOT NULL
BEGIN
    PRINT 'Tables already exist: Part 1 (structure) is skipped.';
    SET NOEXEC ON;
END
GO

/* ---------- PART 1: TABLES ---------- */
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
GO
CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(256) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(256) NOT NULL,
    [IsActive] bit NOT NULL,
    [StaffId] uniqueidentifier NULL,
    [PatientId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(256) NULL,
    [SecurityStamp] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(256) NULL,
    [PhoneNumber] nvarchar(256) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
CREATE TABLE [Departments] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NOT NULL,
    [IsDischargeClearanceDepartment] bit NOT NULL,
    [IsMandatoryClearance] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);
CREATE TABLE [Patients] (
    [Id] uniqueidentifier NOT NULL,
    [Mrn] nvarchar(256) NOT NULL,
    [FirstName] nvarchar(256) NOT NULL,
    [LastName] nvarchar(256) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(40) NOT NULL,
    [Phone] nvarchar(256) NOT NULL,
    [Email] nvarchar(256) NULL,
    [NationalId] nvarchar(256) NULL,
    [Address] nvarchar(256) NULL,
    [EmergencyContactName] nvarchar(256) NULL,
    [EmergencyContactPhone] nvarchar(256) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
);
CREATE TABLE [Shifts] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NOT NULL,
    [Type] nvarchar(40) NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id])
);
CREATE TABLE [Wards] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NOT NULL,
    [UnitType] nvarchar(256) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Wards] PRIMARY KEY ([Id])
);
CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(256) NULL,
    [ClaimValue] nvarchar(256) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(256) NULL,
    [ClaimValue] nvarchar(256) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(256) NOT NULL,
    [ProviderKey] nvarchar(256) NOT NULL,
    [ProviderDisplayName] nvarchar(256) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(256) NOT NULL,
    [Name] nvarchar(256) NOT NULL,
    [Value] nvarchar(256) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [DutyRosters] (
    [Id] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NOT NULL,
    [PeriodStart] date NOT NULL,
    [PeriodEnd] date NOT NULL,
    [IsPublished] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_DutyRosters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DutyRosters_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Staff] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [FullName] nvarchar(256) NOT NULL,
    [RoleName] nvarchar(256) NOT NULL,
    [DepartmentId] uniqueidentifier NOT NULL,
    [Specialty] nvarchar(256) NULL,
    [LicenseNumber] nvarchar(256) NULL,
    [LicenseExpiry] date NULL,
    [Phone] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Staff] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Staff_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [PatientJourneyEvents] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [AppointmentId] uniqueidentifier NULL,
    [EncounterId] uniqueidentifier NULL,
    [Station] nvarchar(40) NOT NULL,
    [EventType] nvarchar(40) NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [RecordedByUserId] uniqueidentifier NULL,
    [IsManualCorrection] bit NOT NULL,
    [CorrectionReason] nvarchar(256) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PatientJourneyEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PatientJourneyEvents_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Rooms] (
    [Id] uniqueidentifier NOT NULL,
    [WardId] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rooms_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [Wards] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Attendances] (
    [Id] uniqueidentifier NOT NULL,
    [StaffId] uniqueidentifier NOT NULL,
    [CheckInUtc] datetime2 NOT NULL,
    [CheckOutUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendances_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Encounters] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [DoctorId] uniqueidentifier NOT NULL,
    [Type] nvarchar(40) NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [EndedAtUtc] datetime2 NULL,
    [ChiefComplaint] nvarchar(256) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Encounters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Encounters_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Encounters_Staff_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [LeaveRequests] (
    [Id] uniqueidentifier NOT NULL,
    [StaffId] uniqueidentifier NOT NULL,
    [FromDate] date NOT NULL,
    [ToDate] date NOT NULL,
    [Reason] nvarchar(256) NULL,
    [Status] nvarchar(40) NOT NULL,
    [DecidedByUserId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveRequests_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [RosterAssignments] (
    [Id] uniqueidentifier NOT NULL,
    [RosterId] uniqueidentifier NOT NULL,
    [StaffId] uniqueidentifier NOT NULL,
    [ShiftId] uniqueidentifier NOT NULL,
    [Date] date NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_RosterAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RosterAssignments_DutyRosters_RosterId] FOREIGN KEY ([RosterId]) REFERENCES [DutyRosters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RosterAssignments_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RosterAssignments_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Beds] (
    [Id] uniqueidentifier NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [Code] nvarchar(256) NOT NULL,
    [BedType] nvarchar(256) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Beds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Beds_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Admissions] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [EncounterId] uniqueidentifier NOT NULL,
    [CurrentBedId] uniqueidentifier NULL,
    [Status] nvarchar(40) NOT NULL,
    [AdmittedAtUtc] datetime2 NOT NULL,
    [DischargedAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Admissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Admissions_Beds_CurrentBedId] FOREIGN KEY ([CurrentBedId]) REFERENCES [Beds] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Admissions_Encounters_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [Encounters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Admissions_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [Discharges] (
    [Id] uniqueidentifier NOT NULL,
    [AdmissionId] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [DoctorId] uniqueidentifier NOT NULL,
    [Stage] nvarchar(40) NOT NULL,
    [Summary] nvarchar(max) NOT NULL,
    [Medications] nvarchar(max) NULL,
    [FollowUpInstructions] nvarchar(max) NULL,
    [FinalizedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Discharges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Discharges_Admissions_AdmissionId] FOREIGN KEY ([AdmissionId]) REFERENCES [Admissions] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [DischargeAddenda] (
    [Id] uniqueidentifier NOT NULL,
    [DischargeId] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [Text] nvarchar(max) NOT NULL,
    [AuthorStaffId] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_DischargeAddenda] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DischargeAddenda_Discharges_DischargeId] FOREIGN KEY ([DischargeId]) REFERENCES [Discharges] ([Id]) ON DELETE NO ACTION
);
CREATE TABLE [DischargeClearanceItems] (
    [Id] uniqueidentifier NOT NULL,
    [DischargeId] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [BlockReason] nvarchar(256) NULL,
    [IsMandatory] bit NOT NULL,
    [ClearedByUserId] uniqueidentifier NULL,
    [ClearedAtUtc] datetime2 NULL,
    [LastReminderAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_DischargeClearanceItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DischargeClearanceItems_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DischargeClearanceItems_Discharges_DischargeId] FOREIGN KEY ([DischargeId]) REFERENCES [Discharges] ([Id]) ON DELETE NO ACTION
);
GO

/* ---------- PART 1: INDEXES ---------- */
CREATE INDEX [IX_Admissions_CurrentBedId] ON [Admissions] ([CurrentBedId]);
CREATE INDEX [IX_Admissions_EncounterId] ON [Admissions] ([EncounterId]);
CREATE INDEX [IX_Admissions_PatientId] ON [Admissions] ([PatientId]);
CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
CREATE INDEX [IX_Attendances_StaffId] ON [Attendances] ([StaffId]);
CREATE UNIQUE INDEX [IX_Beds_Code] ON [Beds] ([Code]);
CREATE INDEX [IX_Beds_RoomId] ON [Beds] ([RoomId]);
CREATE INDEX [IX_DischargeAddenda_DischargeId] ON [DischargeAddenda] ([DischargeId]);
CREATE INDEX [IX_DischargeClearanceItems_DepartmentId] ON [DischargeClearanceItems] ([DepartmentId]);
CREATE INDEX [IX_DischargeClearanceItems_DischargeId] ON [DischargeClearanceItems] ([DischargeId]);
CREATE UNIQUE INDEX [IX_Discharges_AdmissionId] ON [Discharges] ([AdmissionId]);
CREATE INDEX [IX_DutyRosters_DepartmentId] ON [DutyRosters] ([DepartmentId]);
CREATE INDEX [IX_Encounters_DoctorId] ON [Encounters] ([DoctorId]);
CREATE INDEX [IX_Encounters_PatientId] ON [Encounters] ([PatientId]);
CREATE INDEX [IX_LeaveRequests_StaffId] ON [LeaveRequests] ([StaffId]);
CREATE INDEX [IX_PatientJourneyEvents_PatientId] ON [PatientJourneyEvents] ([PatientId]);
CREATE INDEX [IX_PatientJourneyEvents_Station_OccurredAtUtc] ON [PatientJourneyEvents] ([Station], [OccurredAtUtc]);
CREATE INDEX [IX_Patients_LastName_FirstName_DateOfBirth_Phone] ON [Patients] ([LastName], [FirstName], [DateOfBirth], [Phone]);
CREATE UNIQUE INDEX [IX_Patients_Mrn] ON [Patients] ([Mrn]);
CREATE INDEX [IX_Rooms_WardId] ON [Rooms] ([WardId]);
CREATE INDEX [IX_RosterAssignments_RosterId] ON [RosterAssignments] ([RosterId]);
CREATE INDEX [IX_RosterAssignments_ShiftId] ON [RosterAssignments] ([ShiftId]);
CREATE INDEX [IX_RosterAssignments_StaffId] ON [RosterAssignments] ([StaffId]);
CREATE INDEX [IX_Staff_DepartmentId] ON [Staff] ([DepartmentId]);
GO

/* Tell the application that the tables already exist (so it does not try to create them again) */
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261122105712_InitialCreate', N'10.0.15');
GO
SET NOEXEC OFF;
GO

/* ---------- PART 2A: MUKUNZI SCRIPTS (included as written; safe to re-run) ---------- */
-- SmartCare HMS
-- 010_mukunzi_audit_log.sql
-- Owner: Mukunzi Jonathan
-- Include this file in reset_and_seed.sql (Elvin).
-- Contract used by SmartCare.Infrastructure.Auditing.AuditLogger:
--   dbo.sp_WriteAuditLog

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

CREATE OR ALTER PROCEDURE dbo.sp_WriteAuditLog
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

-- SmartCare HMS
-- 020_mukunzi_document_storage.sql
-- Owner: Mukunzi Jonathan
-- Include this file in reset_and_seed.sql (Elvin).
-- Contract used by SmartCare.Infrastructure.FileStorage.LocalFileStorage:
--   dbo.sp_InsertDocument
--   dbo.sp_GetDocumentById
-- Files live on disk under App_Data/uploads (GUID names), not in this table.
-- When the Patient table exists, add a FK from EntityId where EntityType = 'Patient'.

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

CREATE OR ALTER PROCEDURE dbo.sp_InsertDocument
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

CREATE OR ALTER PROCEDURE dbo.sp_GetDocumentById
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


/* ---------- PART 2: ROLES AND DEPARTMENTS (needed by the application) ---------- */
SET NOCOUNT ON;

INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
SELECT NEWID(), v.RoleName, UPPER(v.RoleName), CONVERT(nvarchar(50), NEWID())
FROM (VALUES (N'SuperAdmin'), (N'HospitalAdmin'), (N'Doctor'), (N'Nurse'), (N'Receptionist'),
             (N'LabTechnician'), (N'Pharmacist'), (N'BillingStaff'), (N'Patient')) AS v(RoleName)
WHERE NOT EXISTS (SELECT 1 FROM [AspNetRoles] r WHERE r.[NormalizedName] = UPPER(v.RoleName));
GO

IF NOT EXISTS (SELECT 1 FROM [Departments])
BEGIN
    INSERT INTO [Departments] ([Id], [Name], [IsDischargeClearanceDepartment], [IsMandatoryClearance], [CreatedAtUtc])
    VALUES (NEWID(), N'Nursing',        1, 1, SYSUTCDATETIME()),
           (NEWID(), N'Pharmacy',       1, 1, SYSUTCDATETIME()),
           (NEWID(), N'Billing',        1, 1, SYSUTCDATETIME()),
           (NEWID(), N'Laboratory',     1, 0, SYSUTCDATETIME()),
           (NEWID(), N'Radiology',      0, 0, SYSUTCDATETIME()),
           (NEWID(), N'Outpatient',     0, 0, SYSUTCDATETIME()),
           (NEWID(), N'Emergency',      0, 0, SYSUTCDATETIME()),
           (NEWID(), N'Administration', 0, 0, SYSUTCDATETIME());
END
GO

/* ---------- PART 3: DEMO DATA (development only; skip or delete this part for a clean database) ----------
   Creates: 6 users (password for all: ChangeMe-Dev#2026), staff profiles, test patient MRN-0001
   with an open visit.
   It does nothing if patient MRN-0001 already exists. */
IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Mrn] = N'MRN-0001')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @now datetime2 = SYSUTCDATETIME();
    DECLARE @today date = CAST(@now AS date);
    DECLARE @pw nvarchar(256) = N'AQAAAAEAAYagAAAAEFWmh9eu/Rzu8aIvwN3b6Ud14X9svC3LX1B2so5SqWs8xOsKARGHqBHR/d8kZQx9iA==';

    DECLARE @dOutpatient uniqueidentifier = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Outpatient');
    DECLARE @dNursing    uniqueidentifier = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Nursing');
    DECLARE @dPharmacy   uniqueidentifier = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Pharmacy');
    DECLARE @dLab        uniqueidentifier = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Laboratory');

    DECLARE @uAdmin uniqueidentifier = NEWID(), @uDoctor uniqueidentifier = NEWID(), @uNurse uniqueidentifier = NEWID(),
            @uPharm uniqueidentifier = NEWID(), @uLab1 uniqueidentifier = NEWID(), @uLab2 uniqueidentifier = NEWID();
    DECLARE @sDoctor uniqueidentifier = NEWID(), @sNurse uniqueidentifier = NEWID(), @sPharm uniqueidentifier = NEWID(),
            @sLab1 uniqueidentifier = NEWID(), @sLab2 uniqueidentifier = NEWID();

    /* staff profiles */
    INSERT INTO [Staff] ([Id], [UserId], [FullName], [RoleName], [DepartmentId], [Email], [IsActive], [CreatedAtUtc]) VALUES
      (@sDoctor, @uDoctor, N'Dr. Demo Doctor',    N'Doctor',        @dOutpatient, N'doctor@smartcare.local',     1, @now),
      (@sNurse,  @uNurse,  N'Demo Nurse',         N'Nurse',         @dNursing,    N'nurse@smartcare.local',      1, @now),
      (@sPharm,  @uPharm,  N'Demo Pharmacist',    N'Pharmacist',    @dPharmacy,   N'pharmacist@smartcare.local', 1, @now),
      (@sLab1,   @uLab1,   N'Demo Lab Tech One',  N'LabTechnician', @dLab,        N'lab@smartcare.local',        1, @now),
      (@sLab2,   @uLab2,   N'Demo Lab Tech Two',  N'LabTechnician', @dLab,        N'lab2@smartcare.local',       1, @now);

    /* login accounts */
    INSERT INTO [AspNetUsers]
      ([Id],[FullName],[IsActive],[StaffId],[PatientId],[CreatedAtUtc],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],
       [EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
    SELECT u.Id, u.FullName, 1, u.StaffId, NULL, @now, u.Email, UPPER(u.Email), u.Email, UPPER(u.Email),
           1, @pw, CONVERT(nvarchar(50), NEWID()), CONVERT(nvarchar(50), NEWID()), 0, 0, 1, 0
    FROM (VALUES
      (@uAdmin,  N'System Administrator', CAST(NULL AS uniqueidentifier), N'admin@smartcare.local'),
      (@uDoctor, N'Dr. Demo Doctor',      @sDoctor, N'doctor@smartcare.local'),
      (@uNurse,  N'Demo Nurse',           @sNurse,  N'nurse@smartcare.local'),
      (@uPharm,  N'Demo Pharmacist',      @sPharm,  N'pharmacist@smartcare.local'),
      (@uLab1,   N'Demo Lab Tech One',    @sLab1,   N'lab@smartcare.local'),
      (@uLab2,   N'Demo Lab Tech Two',    @sLab2,   N'lab2@smartcare.local')
    ) AS u(Id, FullName, StaffId, Email);

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT x.UserId, r.[Id]
    FROM (VALUES (@uAdmin, N'SUPERADMIN'), (@uDoctor, N'DOCTOR'), (@uNurse, N'NURSE'),
                 (@uPharm, N'PHARMACIST'), (@uLab1, N'LABTECHNICIAN'), (@uLab2, N'LABTECHNICIAN')) AS x(UserId, RoleKey)
    JOIN [AspNetRoles] r ON r.[NormalizedName] = x.RoleKey;

    /* test patient and an open visit */
    DECLARE @pat uniqueidentifier = NEWID();
    INSERT INTO [Patients] ([Id],[Mrn],[FirstName],[LastName],[DateOfBirth],[Gender],[Phone],[IsActive],[CreatedAtUtc])
    VALUES (@pat, N'MRN-0001', N'Test', N'Patient', '1990-05-12', N'Female', N'+250780000001', 1, @now);
    INSERT INTO [Encounters] ([Id],[PatientId],[DoctorId],[Type],[StartedAtUtc],[ChiefComplaint],[CreatedAtUtc])
    VALUES (NEWID(), @pat, @sDoctor, N'Outpatient', @now, N'Fever and cough', @now);

    COMMIT TRANSACTION;
    PRINT 'Demo data created. Sign in with the demo emails and password ChangeMe-Dev#2026';
END
ELSE PRINT 'Demo data already present: Part 3 skipped.';
GO

/* ---------- CHECK ---------- */
SELECT 'Tables' AS What, COUNT(*) AS Total FROM sys.tables
UNION ALL SELECT 'Roles', COUNT(*) FROM [AspNetRoles]
UNION ALL SELECT 'Departments', COUNT(*) FROM [Departments]
UNION ALL SELECT 'Users', COUNT(*) FROM [AspNetUsers]
UNION ALL SELECT 'Staff', COUNT(*) FROM [Staff]
UNION ALL SELECT 'Patients', COUNT(*) FROM [Patients];
GO
