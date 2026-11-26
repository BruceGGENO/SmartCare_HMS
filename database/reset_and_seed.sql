-- SmartCare HMS — full schema + permission seeds (Elvin bootstrap, owned by backend)
-- Prerequisites: database already exists (see scripts/000_create_database.sql).
--
--   cd database
--   sqlcmd -S localhost\SQLEXPRESS -d SmartCareHMS_YourName -E -I -i reset_and_seed.sql
--
-- Or use Reset-Database.ps1 (recommended on Windows).

SET NOCOUNT ON;
PRINT N'=== SmartCare HMS reset_and_seed starting ===';
GO

:r .\scripts\010_mukunzi_authentication.sql
:r .\scripts\025_mukunzi_permissions.sql
:r .\scripts\020_mukunzi_appointments.sql
:r .\scripts\030_mukunzi_audit_log.sql
:r .\scripts\040_mukunzi_document_storage.sql
:r .\scripts\045_mukunzi_patients.sql
:r .\scripts\050_mukunzi_billing.sql
:r .\scripts\055_mukunzi_revenue.sql
:r .\scripts\060_mukunzi_lab.sql
:r .\scripts\065_mukunzi_pharmacy.sql
:r .\scripts\070_mukunzi_beds.sql
:r .\scripts\075_mukunzi_discharge.sql
:r .\scripts\080_mukunzi_waiting.sql
:r .\scripts\085_mukunzi_forecast.sql

PRINT N'=== SmartCare HMS reset_and_seed finished ===';
PRINT N'Next: set ConnectionStrings:DefaultConnection and run the API (demo users/data seed on startup).';
GO
