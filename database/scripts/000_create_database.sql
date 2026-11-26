-- SmartCare HMS
-- 000_create_database.sql
-- Example:
--   sqlcmd -S localhost\SQLEXPRESS -E -I -i database\scripts\000_create_database.sql -v DbName=SmartCareHMS_YourName

IF DB_ID(N'$(DbName)') IS NULL
BEGIN
    DECLARE @sql NVARCHAR(500) = N'CREATE DATABASE ' + QUOTENAME(N'$(DbName)');
    EXEC (@sql);
    PRINT N'Created database $(DbName)';
END
ELSE
    PRINT N'Database already exists: $(DbName)';
GO
