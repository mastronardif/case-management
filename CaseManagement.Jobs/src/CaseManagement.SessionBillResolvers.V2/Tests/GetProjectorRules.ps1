# GetProjectorRules.ps1
# Analysis only — query [cases].[ProjectorRule] and related doc info.
# DO NOT use in application code.

$settings = Get-Content "$PSScriptRoot\..\appsettings.json" | ConvertFrom-Json
$connStr  = $settings.ConnectionStrings.DefaultConnection

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$cmd = $conn.CreateCommand()
$cmd.CommandText = "
    SELECT
        pr.ProjectorRuleId,
        pr.Name,
        pr.Version,
        pr.Description,
        pr.ProjectionDocumentId,
        pr.RuleDocumentId,
        pr.IsActive,
        pr.CreatedDate,
        pr.CreatedBy
    FROM   [cases].[ProjectorRule] pr
    WHERE  pr.IsActive = 1
    ORDER  BY pr.Name
"

$reader = $cmd.ExecuteReader()
$table  = New-Object System.Data.DataTable
$table.Load($reader)
$conn.Close()

$table | Format-Table -AutoSize
