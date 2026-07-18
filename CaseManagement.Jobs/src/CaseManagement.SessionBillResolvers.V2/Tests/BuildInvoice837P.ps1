# BuildInvoice837P.ps1
# Reads an invoice build spec JSON, chains (P)(AM) pipeline steps to produce a 837P invoice,
# then applies the current 837P rule (Q) and serializes to X12 (W).
# Usage: .\BuildInvoice837P.ps1 -SpecFile ".\invoice-build.json"
# Analysis / test script — not used in application code.

param(
    [Parameter(Mandatory=$true)]
    [string]$SpecFile
)

$projectDir = "C:\Users\mastronardif\source\repos\CaseMangement\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2"

# Load spec
$spec     = Get-Content $SpecFile | ConvertFrom-Json
$caseId   = $spec.caseId
$invoice  = $spec.blankDocId
$sources  = $spec.sources

function Get-ActiveRuleDocId {
    param([string]$RuleName)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        SELECT TOP 1 RuleDocumentId
        FROM   [cases].[ProjectorRule]
        WHERE  Name = @Name AND IsActive = 1
        ORDER  BY ProjectorRuleId DESC
    "
    $cmd.Parameters.AddWithValue("@Name", $RuleName) | Out-Null

    $result = $cmd.ExecuteScalar()
    $conn.Close()

    if (-not $result -or $result -eq 0) {
        Write-Error "No active RuleDocumentId found in [cases].[ProjectorRule] for '$RuleName'."
        exit 1
    }
    return [int]$result
}

function Get-ActiveProjectionDocId {
    param([string]$ProjectionName)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        SELECT TOP 1 ProjectionDocumentId
        FROM   [cases].[ProjectorRule]
        WHERE  Name = @Name AND IsActive = 1
        ORDER  BY ProjectorRuleId DESC
    "
    $cmd.Parameters.AddWithValue("@Name", $ProjectionName) | Out-Null

    $result = $cmd.ExecuteScalar()
    $conn.Close()

    if (-not $result -or $result -eq 0) {
        Write-Error "No active ProjectionDocumentId found in [cases].[ProjectorRule] for '$ProjectionName'."
        exit 1
    }
    return [int]$result
}

function New-Claim {
    param([int]$CaseId, [int[]]$SessionDocumentIds)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = New-Object System.Data.SqlClient.SqlCommand("[cases].[usp_CreateClaim]", $conn)
    $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
    $cmd.Parameters.AddWithValue("@CaseId", $CaseId) | Out-Null
    $cmd.Parameters.AddWithValue("@SessionDocumentIds", ($SessionDocumentIds -join ",")) | Out-Null

    $claimIdParam = New-Object System.Data.SqlClient.SqlParameter("@ClaimId", [System.Data.SqlDbType]::Int)
    $claimIdParam.Direction = [System.Data.ParameterDirection]::Output
    $cmd.Parameters.Add($claimIdParam) | Out-Null

    $claimNumberParam = New-Object System.Data.SqlClient.SqlParameter("@ClaimNumber", [System.Data.SqlDbType]::VarChar, 50)
    $claimNumberParam.Direction = [System.Data.ParameterDirection]::Output
    $cmd.Parameters.Add($claimNumberParam) | Out-Null

    try {
        $cmd.ExecuteNonQuery() | Out-Null
    }
    catch {
        $conn.Close()
        Write-Error "usp_CreateClaim failed: $($_.Exception.Message)"
        exit 1
    }
    $conn.Close()

    return @{ ClaimId = [int]$claimIdParam.Value; ClaimNumber = [string]$claimNumberParam.Value }
}

function Set-ClaimEdiDocument {
    param([int]$ClaimId, [int]$EdiDocumentId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        UPDATE [cases].[Claim]
        SET    EdiDocumentId = @EdiDocumentId, Status = 'Generated'
        WHERE  ClaimId = @ClaimId
    "
    $cmd.Parameters.AddWithValue("@EdiDocumentId", $EdiDocumentId) | Out-Null
    $cmd.Parameters.AddWithValue("@ClaimId", $ClaimId) | Out-Null
    $cmd.ExecuteNonQuery() | Out-Null
    $conn.Close()
}

function Get-PracticeConfiguration {
    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        SELECT TOP 1
            SubmitterName, SubmitterIdentifier, ReceiverName,
            SenderIdQualifier, SenderId, ReceiverIdQualifier, ReceiverId,
            FunctionalIdentifierCode, VersionIdentifier, TestIndicator
        FROM   [cases].[PracticeConfiguration]
        WHERE  IsActive = 1
        ORDER  BY PracticeConfigurationId DESC
    "
    $reader = $cmd.ExecuteReader()
    $table  = New-Object System.Data.DataTable
    $table.Load($reader)
    $conn.Close()

    if ($table.Rows.Count -eq 0) {
        Write-Error "No active row found in [cases].[PracticeConfiguration]."
        exit 1
    }
    $row = $table.Rows[0]

    return @{
        submitterName            = $row.SubmitterName
        submitterIdentifier      = $row.SubmitterIdentifier
        receiverName              = $row.ReceiverName
        senderIdQualifier         = $row.SenderIdQualifier
        senderId                  = $row.SenderId
        receiverIdQualifier       = $row.ReceiverIdQualifier
        receiverId                = $row.ReceiverId
        functionalIdentifierCode  = $row.FunctionalIdentifierCode
        versionIdentifier         = $row.VersionIdentifier
        testIndicator             = $row.TestIndicator
    }
}

function Invoke-PipelineStep {
    param([string]$Expr, [int]$CaseId)

    Push-Location $projectDir
    $output = dotnet run -- --expression $Expr --case-id $CaseId 2>&1
    Pop-Location

    # Show last few lines (skip empty)
    $output | Where-Object { $_ -match '\S' } | Select-Object -Last 6 | ForEach-Object { Write-Host "  $_" }

    # Parse last output docId:  step N  docId NNN  http://...
    $hits = [regex]::Matches(($output -join "`n"), 'docId\s+(\d+)\s')
    if ($hits.Count -eq 0) {
        Write-Error "No output docId found.`nOutput:`n$($output -join "`n")"
        exit 1
    }
    return [int]$hits[$hits.Count - 1].Groups[1].Value
}

function Run-Step {
    param([int]$SourceDocId, [int]$ProjectorDocId, [int]$TargetDocId, [string]$Label)

    Write-Host ""
    Write-Host "$Label : $SourceDocId (P) $ProjectorDocId (AM) $TargetDocId" -ForegroundColor Yellow
    $result = Invoke-PipelineStep "$SourceDocId (P) $ProjectorDocId (AM) $TargetDocId" $caseId
    Write-Host "  => docId: $result" -ForegroundColor Green
    return $result
}

Write-Host ""
Write-Host "=== Build 837P Invoice ===" -ForegroundColor Cyan
Write-Host "  Spec     : $SpecFile" -ForegroundColor Gray
Write-Host "  CaseId   : $caseId  |  Blank: $invoice" -ForegroundColor Gray
Write-Host ""

# Claim — created first; fails fast if any session is already claimed
Write-Host "Claim : creating for sessions [$($sources.sessions -join ', ')]..." -ForegroundColor Yellow
$claim = New-Claim -CaseId $caseId -SessionDocumentIds $sources.sessions
Write-Host "  => claimId: $($claim.ClaimId)  claimNumber: $($claim.ClaimNumber)" -ForegroundColor Green

$claimJson  = @{ claimNumber = $claim.ClaimNumber } | ConvertTo-Json
$claimSave  = @{ json = $claimJson; name = "claim" } | ConvertTo-Json
$claimResp  = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $claimSave
$claimDocId = $claimResp.docId

$claimProjectorDocId = Get-ActiveProjectionDocId "Claim837P"
$invoice = Run-Step $claimDocId $claimProjectorDocId $invoice "Claim ($claimDocId)"

# Sessions — each appends a Loop 2400 entry
if ($sources.sessions -and $sources.sessions.Count -gt 0) {
    $sessionProjectorDocId = Get-ActiveProjectionDocId "Session837P"
    $i = 1
    foreach ($sessionDocId in $sources.sessions) {
        $invoice = Run-Step $sessionDocId $sessionProjectorDocId $invoice "Session $i ($sessionDocId)"
        $i++
    }
}

# Provider
if ($sources.provider) {
    $invoice = Run-Step $sources.provider (Get-ActiveProjectionDocId "Provider837P") $invoice "Provider"
}

# Payer
if ($sources.payer) {
    $invoice = Run-Step $sources.payer (Get-ActiveProjectionDocId "Payer837P") $invoice "Payer"
}

# Authorization
if ($sources.authorization) {
    $invoice = Run-Step $sources.authorization (Get-ActiveProjectionDocId "Authorization837P") $invoice "Authorization"
}

# Patient
if ($sources.patient) {
    $invoice = Run-Step $sources.patient (Get-ActiveProjectionDocId "Patient837P") $invoice "Patient"
}

# Practice Configuration — runs last among (P)(AM) sources so its submitter/receiver
# identity (1000A/1000B) wins over any overlapping values Provider837P may also set
Write-Host ""
Write-Host "Practice Configuration : loading active row..." -ForegroundColor Yellow
$practiceConfig     = Get-PracticeConfiguration
$practiceConfigJson = $practiceConfig | ConvertTo-Json
$practiceConfigSave = @{ json = $practiceConfigJson; name = "practice-configuration" } | ConvertTo-Json
$practiceConfigResp = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $practiceConfigSave
$practiceConfigDocId = $practiceConfigResp.docId

$invoice = Run-Step $practiceConfigDocId (Get-ActiveProjectionDocId "PracticeConfiguration837P") $invoice "Practice Configuration ($practiceConfigDocId)"

# Metadata — build from spec and (M) merge into final invoice
Write-Host ""
Write-Host "Metadata : building from spec..." -ForegroundColor Yellow

$pipelineRunId  = [System.Guid]::NewGuid().ToString()
$sessionSources = @($sources.sessions | ForEach-Object { @{ documentId = $_ } })

$metadataPatch = @{
    metadata = @{
        caseId        = $caseId
        claimNumber   = $claim.ClaimNumber
        pipelineRunId = $pipelineRunId
        sources       = @{
            sessions      = $sessionSources
            provider      = if ($sources.provider)      { $sources.provider }      else { $null }
            payer         = if ($sources.payer)         { $sources.payer }         else { $null }
            authorization = if ($sources.authorization) { $sources.authorization } else { $null }
            patient       = if ($sources.patient)       { $sources.patient }       else { $null }
        }
    }
} | ConvertTo-Json -Depth 6

# Save metadata patch doc via /api/saveWorkflow
$saveBody    = @{ json = $metadataPatch; name = "invoice-metadata" } | ConvertTo-Json
$saveResp    = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $saveBody
$metaDocId   = $saveResp.docId
Write-Host "  metadata doc: $metaDocId" -ForegroundColor Gray

# (M) merge metadata into invoice — replaces metadata block, leaves loops intact
Write-Host "  merging into invoice $invoice..." -ForegroundColor Gray
$invoice = Invoke-PipelineStep "$metaDocId (M) $invoice" $caseId
Write-Host "  => docId: $invoice" -ForegroundColor Green

Write-Host ""
Write-Host "=== Final invoice (pre-rule) ===" -ForegroundColor Cyan
Write-Host "  docId : $invoice" -ForegroundColor White
Write-Host "  Open  : http://localhost:5173/api/getDocument?docId=$invoice" -ForegroundColor Cyan

# Apply the current 837P rule, then serialize to X12
Write-Host ""
$ruleDocId = Get-ActiveRuleDocId "837P_LoopsSegments_X12"
Write-Host "Rule + Write : $invoice (Q) $ruleDocId (W)" -ForegroundColor Yellow
$ediDocId = Invoke-PipelineStep "$invoice (Q) $ruleDocId (W)" $caseId
Write-Host "  => docId: $ediDocId" -ForegroundColor Green

# Close the loop — link the claim row to its generated EDI document
Set-ClaimEdiDocument -ClaimId $claim.ClaimId -EdiDocumentId $ediDocId

Write-Host ""
Write-Host "=== Final EDI ===" -ForegroundColor Cyan
Write-Host "  claimId     : $($claim.ClaimId)" -ForegroundColor Gray
Write-Host "  claimNumber : $($claim.ClaimNumber)" -ForegroundColor Gray
Write-Host "  ruleDocId   : $ruleDocId" -ForegroundColor Gray
Write-Host "  docId       : $ediDocId" -ForegroundColor White
Write-Host "  Open        : http://localhost:5173/api/getDocument?docId=$ediDocId" -ForegroundColor Cyan