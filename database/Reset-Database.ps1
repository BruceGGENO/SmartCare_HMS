<#
.SYNOPSIS
  Applies all SmartCare HMS SQL scripts in order to an existing database.

.EXAMPLE
  .\Reset-Database.ps1 -Database SmartCareHMS_Mukunzi
  .\Reset-Database.ps1 -Server "localhost\SQLEXPRESS" -Database SmartCareHMS_Demo
#>
param(
    [string] $Server = "localhost\SQLEXPRESS",
    [Parameter(Mandatory = $true)]
    [string] $Database
)

$ErrorActionPreference = "Stop"
$scriptsRoot = Join-Path $PSScriptRoot "scripts"

$ordered = @(
    "010_mukunzi_authentication.sql",
    "025_mukunzi_permissions.sql",
    "020_mukunzi_appointments.sql",
    "030_mukunzi_audit_log.sql",
    "040_mukunzi_document_storage.sql",
    "045_mukunzi_patients.sql",
    "050_mukunzi_billing.sql",
    "055_mukunzi_revenue.sql",
    "060_mukunzi_lab.sql",
    "065_mukunzi_pharmacy.sql",
    "070_mukunzi_beds.sql",
    "075_mukunzi_discharge.sql",
    "080_mukunzi_waiting.sql",
    "085_mukunzi_forecast.sql"
)

Write-Host "Target: $Server / $Database" -ForegroundColor Cyan

foreach ($name in $ordered) {
    $path = Join-Path $scriptsRoot $name
    if (-not (Test-Path $path)) {
        Write-Host "SKIP (not present yet): $name" -ForegroundColor Yellow
        continue
    }
    Write-Host "Applying $name ..." -ForegroundColor Green
    & sqlcmd -S $Server -d $Database -E -I -b -i $path
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed on $name (exit $LASTEXITCODE)"
    }
}

Write-Host "Done. Point appsettings.Development.json at this database and run the API." -ForegroundColor Cyan
