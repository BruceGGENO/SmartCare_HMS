-- SmartCare HMS — 085 Occupancy forecast (heuristic stub)
SET NOCOUNT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
GO

MERGE dbo.Permission AS t
USING (VALUES
    (N'forecast.read', N'View occupancy forecast')
) AS s (PermissionCode, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN INSERT (PermissionCode, Description) VALUES (s.PermissionCode, s.Description);
GO

DECLARE @Map TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(100));
INSERT INTO @Map VALUES
(N'Super Admin', N'forecast.read'),
(N'Hospital Admin', N'forecast.read'),
(N'Nurse', N'forecast.read');

INSERT INTO dbo.RolePermission (RoleName, PermissionCode)
SELECT m.RoleName, m.PermissionCode FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.Role r WHERE r.RoleName = m.RoleName)
  AND EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.PermissionCode = m.PermissionCode)
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleName = m.RoleName AND rp.PermissionCode = m.PermissionCode);
GO

-- Returns current snapshot + simple projected occupancy for the next @Days days.
CREATE OR ALTER PROCEDURE dbo.sp_GetOccupancyForecast
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    IF @Days < 1 SET @Days = 1;
    IF @Days > 30 SET @Days = 30;

    DECLARE @Total INT = (SELECT COUNT(*) FROM dbo.Bed);
    DECLARE @Occupied INT = (SELECT COUNT(*) FROM dbo.Bed WHERE Status = N'Occupied');
    IF @Total = 0 SET @Total = 1;

    DECLARE @Rate DECIMAL(9,4) = CAST(@Occupied AS DECIMAL(9,4)) / CAST(@Total AS DECIMAL(9,4));
    DECLARE @Admits7 INT = (
        SELECT COUNT(*) FROM dbo.Admission
        WHERE AdmittedAtUtc >= DATEADD(DAY, -7, SYSDATETIMEOFFSET())
    );
    DECLARE @Discharges7 INT = (
        SELECT COUNT(*) FROM dbo.Admission
        WHERE DischargedAtUtc IS NOT NULL
          AND DischargedAtUtc >= DATEADD(DAY, -7, SYSDATETIMEOFFSET())
    );
    DECLARE @NetDaily DECIMAL(9,4) =
        CAST((@Admits7 - @Discharges7) AS DECIMAL(9,4)) / 7.0 / CAST(@Total AS DECIMAL(9,4));

    ;WITH N AS (
        SELECT 0 AS DayOffset
        UNION ALL SELECT DayOffset + 1 FROM N WHERE DayOffset + 1 < @Days
    )
    SELECT
        DATEADD(DAY, n.DayOffset, CAST(SYSUTCDATETIME() AS DATE)) AS ForecastDate,
        @Total AS TotalBeds,
        @Occupied AS CurrentOccupiedBeds,
        CAST(ROUND(
            CASE
                WHEN (@Rate + (@NetDaily * n.DayOffset)) < 0 THEN 0
                WHEN (@Rate + (@NetDaily * n.DayOffset)) > 1 THEN 1
                ELSE @Rate + (@NetDaily * n.DayOffset)
            END * @Total, 0) AS INT) AS PredictedOccupiedBeds,
        CAST(ROUND(
            CASE
                WHEN (@Rate + (@NetDaily * n.DayOffset)) < 0 THEN 0
                WHEN (@Rate + (@NetDaily * n.DayOffset)) > 1 THEN 1
                ELSE @Rate + (@NetDaily * n.DayOffset)
            END * 100, 1) AS DECIMAL(5,1)) AS PredictedOccupancyPercent,
        N'Heuristic' AS Method
    FROM N
    OPTION (MAXRECURSION 30);
END
GO
