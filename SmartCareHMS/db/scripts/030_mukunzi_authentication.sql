-- SmartCare HMS
-- 030_mukunzi_authentication.sql
-- Owner: Mukunzi Jonathan (covering Raissa's auth foundation)
-- Include this file in reset_and_seed.sql (Elvin).
-- Contracts:
--   dbo.sp_GetUserByUserName, dbo.sp_GetUserRoles
--   dbo.sp_InsertUser, dbo.sp_InsertUserRole, dbo.sp_CountUsers
--   dbo.sp_UpdateLoginFailure, dbo.sp_ResetLoginFailure
--   dbo.sp_InsertOtp, dbo.sp_GetLatestOpenOtp, dbo.sp_ConsumeOtp
--   dbo.sp_InsertOutbox, dbo.sp_ListOutbox
-- Demo users are created by the app seeder (password Demo@12345), not this script.

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
        OtpId        UNIQUEIDENTIFIER  NOT NULL
            CONSTRAINT PK_OtpChallenge PRIMARY KEY,
        UserId       UNIQUEIDENTIFIER  NOT NULL,
        CodeHash     NVARCHAR(200)     NOT NULL,
        CodeSalt     NVARCHAR(200)     NOT NULL,
        Purpose      NVARCHAR(50)      NOT NULL,
        ExpiresAtUtc DATETIMEOFFSET(7) NOT NULL,
        ConsumedAtUtc DATETIMEOFFSET(7) NULL,
        CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
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

CREATE OR ALTER PROCEDURE dbo.sp_CountUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.[User];
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUserByUserName
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

CREATE OR ALTER PROCEDURE dbo.sp_GetUserRoles
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoleName
    FROM dbo.UserRole
    WHERE UserId = @UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InsertUser
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

CREATE OR ALTER PROCEDURE dbo.sp_InsertUserRole
    @UserId   UNIQUEIDENTIFIER,
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.UserRole (UserId, RoleName)
    VALUES (@UserId, @RoleName);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateLoginFailure
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

CREATE OR ALTER PROCEDURE dbo.sp_ResetLoginFailure
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

CREATE OR ALTER PROCEDURE dbo.sp_InsertOtp
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

CREATE OR ALTER PROCEDURE dbo.sp_GetLatestOpenOtp
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

CREATE OR ALTER PROCEDURE dbo.sp_ConsumeOtp
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

CREATE OR ALTER PROCEDURE dbo.sp_InsertOutbox
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

CREATE OR ALTER PROCEDURE dbo.sp_ListOutbox
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
