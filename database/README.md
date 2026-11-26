# SmartCare HMS — Database

ADO.NET + stored procedures. Each numbered script under `scripts/` is idempotent.

## 1. Create your database

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -E -I -i scripts\000_create_database.sql -v DbName=SmartCareHMS_YourName
```

Or create `SmartCareHMS_YourName` in SSMS.

## 2. Apply schema + permission seeds

From this folder (`database/`):

```powershell
.\Reset-Database.ps1 -Database SmartCareHMS_YourName
```

Equivalent (scripts that already exist):

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d SmartCareHMS_YourName -E -I -i reset_and_seed.sql
```

Always pass **`-I`** (QUOTED_IDENTIFIER ON) for filtered indexes.

### Script order

| Script | Module |
|--------|--------|
| 010 | Auth / roles / users |
| 025 | Permission catalog + role map |
| 020 | Patients (base), appointments, portal packages |
| 030 | Audit log |
| 040 | Document storage |
| 045 | Patients clinical (allergies, ICD) |
| 050 | Billing |
| 055 | Revenue / prepaid packages |
| 060 | Lab |
| 065 | Pharmacy |
| 070 | Beds / admissions |
| 075 | Discharge |
| 080 | Waiting room (appointment queue columns) |
| 085 | Occupancy forecast procs |

`Reset-Database.ps1` skips scripts that are not in the repo yet.

## 3. Connection string

In `back-end/SmartCare.API/appsettings.Development.json` (local only — do not commit):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SmartCareHMS_YourName;Trusted_Connection=True;TrustServerCertificate=True"
}
```

## 4. Run API

```powershell
cd ..\back-end\SmartCare.API
dotnet run
```

On Development startup the API seeds demo users (`Demo@12345`) and demo domain data when tables are empty.

Scalar: `http://localhost:5284/scalar`

## Smoke checklist (after full seed)

1. Login as `admin` / `Demo@12345` (re-login after new permission scripts so JWT claims refresh)
2. Hit one GET per module:
   - `GET /api/patients`
   - `GET /api/appointments`
   - `GET /api/billing/invoices/pending`
   - `GET /api/revenue/summary`
   - `GET /api/lab/tests`
   - `GET /api/pharmacy/drugs`
   - `GET /api/beds`
   - `GET /api/discharge`
   - `GET /api/waiting/queue`
   - `GET /api/forecast/occupancy?days=7`
3. Demo password for all seeded users: `Demo@12345`
