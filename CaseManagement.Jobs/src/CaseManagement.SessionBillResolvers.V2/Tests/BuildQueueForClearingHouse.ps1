# BuildQueueForClearingHouse.ps1
# Takes a claim-queue-spec-shaped JSON ({caseId, sessions, provider, authorization, payer,
# patient} — the same shape ClaimPage's "Submit Claim" saves as a docId), either from a local
# file or fetched live via a Queue1 (queueClaimsToBeCreated) row id. Chains (P)(AM) pipeline
# steps to produce a 837P invoice, applies the current 837P rule (Q), serializes to X12 (W),
# and enqueues the result on Queue2 (queueClaimsToBeSubmitted).
# Usage: .\BuildQueueForClearingHouse.ps1 -SpecFile ".\clearinghouse-spec.json"
#    or: .\BuildQueueForClearingHouse.ps1 -QueueClaimId 1
# Manual stand-in for the not-yet-built worker that drains Queue1 automatically.
# Analysis / test script — not used in application code.

[CmdletBinding(DefaultParameterSetName = "File")]
param(
    [Parameter(Mandatory=$true, ParameterSetName="File")]
    [string]$SpecFile,

    [Parameter(Mandatory=$true, ParameterSetName="Queue")]
    [int]$QueueClaimId
)

$projectDir = "C:\Users\mastronardif\source\repos\CaseMangement\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2"

# The pipeline needs a starting doc to merge into — no catalog entry for this exists yet,
# so it's a fixed constant matching every other spec file used this session.
$BlankInvoiceDocId = 749

function Get-QueueClaimSpecDocId {
    param([int]$QueueClaimId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT SpecDocumentId FROM cases.queueClaimsToBeCreated WHERE QueueClaimId = @QueueClaimId"
    $cmd.Parameters.AddWithValue("@QueueClaimId", $QueueClaimId) | Out-Null

    $result = $cmd.ExecuteScalar()
    $conn.Close()

    if (-not $result -or $result -is [DBNull]) {
        Write-Error "Queue1 row $QueueClaimId not found, or has no SpecDocumentId (was it submitted before that column existed?)."
        exit 1
    }
    return [int]$result
}

function Set-QueueClaimStatus {
    param([int]$QueueClaimId, [string]$Status)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "UPDATE cases.queueClaimsToBeCreated SET Status = @Status WHERE QueueClaimId = @QueueClaimId"
    $cmd.Parameters.AddWithValue("@Status", $Status) | Out-Null
    $cmd.Parameters.AddWithValue("@QueueClaimId", $QueueClaimId) | Out-Null
    $cmd.ExecuteNonQuery() | Out-Null
    $conn.Close()
}

# Load spec — either from a local file, or fetched live from the Queue1 row's spec doc
if ($PSCmdlet.ParameterSetName -eq "Queue") {
    $specDocId = Get-QueueClaimSpecDocId -QueueClaimId $QueueClaimId
    $spec      = Invoke-RestMethod -Uri "http://localhost:5173/api/getDocument?docId=$specDocId"
} else {
    $spec = Get-Content $SpecFile | ConvertFrom-Json
}
$caseId  = $spec.caseId
$invoice = $BlankInvoiceDocId
$queueClaimIdForEvents = if ($PSCmdlet.ParameterSetName -eq "Queue") { $QueueClaimId } else { $null }

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
    param([int]$ClaimId, [int]$EdiDocumentId, [string]$Status, [int]$SourcesDocumentId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        UPDATE [cases].[Claim]
        SET    EdiDocumentId = @EdiDocumentId, Status = @Status, SourcesDocumentId = @SourcesDocumentId
        WHERE  ClaimId = @ClaimId
    "
    $cmd.Parameters.AddWithValue("@EdiDocumentId", $EdiDocumentId) | Out-Null
    $cmd.Parameters.AddWithValue("@Status", $Status) | Out-Null
    $cmd.Parameters.AddWithValue("@SourcesDocumentId", $SourcesDocumentId) | Out-Null
    $cmd.Parameters.AddWithValue("@ClaimId", $ClaimId) | Out-Null
    $cmd.ExecuteNonQuery() | Out-Null
    $conn.Close()
}

function Add-ClaimToSubmitQueue {
    param([int]$ClaimId, [int]$EdiDocumentId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        INSERT INTO [cases].[queueClaimsToBeSubmitted] (ClaimId, EdiDocumentId, Status)
        VALUES (@ClaimId, @EdiDocumentId, 'Pending')
    "
    $cmd.Parameters.AddWithValue("@ClaimId", $ClaimId) | Out-Null
    $cmd.Parameters.AddWithValue("@EdiDocumentId", $EdiDocumentId) | Out-Null
    $cmd.ExecuteNonQuery() | Out-Null
    $conn.Close()
}

# Paper trail — one row per meaningful pipeline transition. ClaimId/QueueClaimId/DocumentId
# are all optional so an event can anchor to whichever ids are known at that point.
function Add-ClaimPipelineEvent {
    param(
        [int]$CaseId,
        [Nullable[int]]$ClaimId = $null,
        [Nullable[int]]$QueueClaimId = $null,
        [Parameter(Mandatory=$true)]
        [string]$EventType,
        [Nullable[int]]$DocumentId = $null,
        [string]$Details = $null
    )

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        INSERT INTO [cases].[ClaimPipelineEvent] (CaseId, ClaimId, QueueClaimId, EventType, DocumentId, Details)
        VALUES (@CaseId, @ClaimId, @QueueClaimId, @EventType, @DocumentId, @Details)
    "
    $cmd.Parameters.AddWithValue("@CaseId", $CaseId) | Out-Null
    $cmd.Parameters.AddWithValue("@ClaimId", $(if ($ClaimId) { $ClaimId } else { [DBNull]::Value })) | Out-Null
    $cmd.Parameters.AddWithValue("@QueueClaimId", $(if ($QueueClaimId) { $QueueClaimId } else { [DBNull]::Value })) | Out-Null
    $cmd.Parameters.AddWithValue("@EventType", $EventType) | Out-Null
    $cmd.Parameters.AddWithValue("@DocumentId", $(if ($DocumentId) { $DocumentId } else { [DBNull]::Value })) | Out-Null
    $cmd.Parameters.AddWithValue("@Details", $(if ($Details) { $Details } else { [DBNull]::Value })) | Out-Null
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
Write-Host "=== Build Queue For Clearing House ===" -ForegroundColor Cyan
Write-Host "  Spec     : $SpecFile" -ForegroundColor Gray
Write-Host "  CaseId   : $caseId  |  Blank: $invoice" -ForegroundColor Gray
Write-Host ""

# Claim — created first; fails fast if any session is already claimed
Write-Host "Claim : creating for sessions [$($spec.sessions -join ', ')]..." -ForegroundColor Yellow
$claim = New-Claim -CaseId $caseId -SessionDocumentIds $spec.sessions
Write-Host "  => claimId: $($claim.ClaimId)  claimNumber: $($claim.ClaimNumber)" -ForegroundColor Green

$claimJson  = @{ claimNumber = $claim.ClaimNumber } | ConvertTo-Json
$claimSave  = @{ json = $claimJson; name = "claim" } | ConvertTo-Json
$claimResp  = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $claimSave
$claimDocId = $claimResp.docId

$claimProjectorDocId = Get-ActiveProjectionDocId "Claim837P"
$invoice = Run-Step $claimDocId $claimProjectorDocId $invoice "Claim ($claimDocId)"

# Sessions — each appends a Loop 2400 entry
if ($spec.sessions -and $spec.sessions.Count -gt 0) {
    $sessionProjectorDocId = Get-ActiveProjectionDocId "Session837P"
    $i = 1
    foreach ($sessionDocId in $spec.sessions) {
        $invoice = Run-Step $sessionDocId $sessionProjectorDocId $invoice "Session $i ($sessionDocId)"
        $i++
    }
}

# Provider
if ($spec.provider) {
    $invoice = Run-Step $spec.provider (Get-ActiveProjectionDocId "Provider837P") $invoice "Provider"
}

# Payer
if ($spec.payer) {
    $invoice = Run-Step $spec.payer (Get-ActiveProjectionDocId "Payer837P") $invoice "Payer"
}

# Authorization
if ($spec.authorization) {
    $invoice = Run-Step $spec.authorization (Get-ActiveProjectionDocId "Authorization837P") $invoice "Authorization"
}

# Patient
if ($spec.patient) {
    $invoice = Run-Step $spec.patient (Get-ActiveProjectionDocId "Patient837P") $invoice "Patient"
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

# Sources — paper trail of every reference doc used to build this claim. cases.ClaimSession
# remains the live/authoritative session link; sessions are included here too as a point-in-
# time audit snapshot, not a second source of truth for current linkage.
Write-Host ""
Write-Host "Sources : recording paper trail..." -ForegroundColor Yellow
$sourcesJson = @{
    sessions              = $spec.sessions
    provider              = if ($spec.provider)      { $spec.provider }      else { $null }
    payer                 = if ($spec.payer)          { $spec.payer }         else { $null }
    authorization         = if ($spec.authorization)  { $spec.authorization } else { $null }
    patient               = if ($spec.patient)        { $spec.patient }       else { $null }
    practiceConfiguration = $practiceConfigDocId
} | ConvertTo-Json
$sourcesSave  = @{ json = $sourcesJson; name = "claim-sources" } | ConvertTo-Json
$sourcesResp  = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $sourcesSave
$sourcesDocId = $sourcesResp.docId
Write-Host "  sources doc: $sourcesDocId" -ForegroundColor Gray

# Metadata — build from spec and (M) merge into final invoice
Write-Host ""
Write-Host "Metadata : building from spec..." -ForegroundColor Yellow

$pipelineRunId  = [System.Guid]::NewGuid().ToString()
$sessionSources = @($spec.sessions | ForEach-Object { @{ documentId = $_ } })

$metadataPatch = @{
    metadata = @{
        caseId        = $caseId
        claimNumber   = $claim.ClaimNumber
        pipelineRunId = $pipelineRunId
        sources       = @{
            sessions      = $sessionSources
            provider      = if ($spec.provider)      { $spec.provider }      else { $null }
            payer         = if ($spec.payer)         { $spec.payer }         else { $null }
            authorization = if ($spec.authorization) { $spec.authorization } else { $null }
            patient       = if ($spec.patient)       { $spec.patient }       else { $null }
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

# Apply the current 837P rule — split from (W) so we can inspect validation issues
# before deciding the claim's status
Write-Host ""
$ruleDocId = Get-ActiveRuleDocId "837P_LoopsSegments_X12"
Write-Host "Rule : $invoice (Q) $ruleDocId" -ForegroundColor Yellow
$ruledClaimDocId = Invoke-PipelineStep "$invoice (Q) $ruleDocId" $caseId
Write-Host "  => docId: $ruledClaimDocId" -ForegroundColor Green

$ruledClaim = Invoke-RestMethod -Uri "http://localhost:5173/api/getDocument?docId=$ruledClaimDocId"
$issues     = @($ruledClaim.metadata.validationIssues)
$claimStatus = if ($issues.Count -gt 0) { "HasErrors" } else { "ReadyToSubmit" }

$validationDetails = if ($issues.Count -gt 0) { ($issues -join "; ") } else { "Passed" }
Add-ClaimPipelineEvent -CaseId $caseId -ClaimId $claim.ClaimId -QueueClaimId $queueClaimIdForEvents `
    -EventType "ClaimValidated" -DocumentId $ruledClaimDocId -Details $validationDetails

Write-Host ""
Write-Host "Write : $ruledClaimDocId (W)" -ForegroundColor Yellow
$ediDocId = Invoke-PipelineStep "$ruledClaimDocId (W)" $caseId
Write-Host "  => docId: $ediDocId" -ForegroundColor Green

# Close the loop — link the claim row to its generated EDI document and mark
# whether it's actually submittable
Set-ClaimEdiDocument -ClaimId $claim.ClaimId -EdiDocumentId $ediDocId -Status $claimStatus -SourcesDocumentId $sourcesDocId

Add-ClaimPipelineEvent -CaseId $caseId -ClaimId $claim.ClaimId -QueueClaimId $queueClaimIdForEvents `
    -EventType "EdiGenerated" -DocumentId $ediDocId -Details "Status=$claimStatus"

# Queue2 — hand off to the (not-yet-built) clearinghouse submission job (QueueToClearingHouse.ps1),
# but only if the claim actually passed validation
if ($claimStatus -eq "ReadyToSubmit") {
    Add-ClaimToSubmitQueue -ClaimId $claim.ClaimId -EdiDocumentId $ediDocId
    Add-ClaimPipelineEvent -CaseId $caseId -ClaimId $claim.ClaimId -QueueClaimId $queueClaimIdForEvents `
        -EventType "QueuedForClearingHouse" -DocumentId $ediDocId
}

# Queue1 — this row has been processed; it's no longer "Pending"
if ($PSCmdlet.ParameterSetName -eq "Queue") {
    Set-QueueClaimStatus -QueueClaimId $QueueClaimId -Status "Claim for Clearing House"
    Add-ClaimPipelineEvent -CaseId $caseId -ClaimId $claim.ClaimId -QueueClaimId $QueueClaimId `
        -EventType "QueueClaimProcessed" -Details "Status -> Claim for Clearing House"
}

Write-Host ""
Write-Host "=== Final EDI ===" -ForegroundColor Cyan
Write-Host "  claimId     : $($claim.ClaimId)" -ForegroundColor Gray
Write-Host "  claimNumber : $($claim.ClaimNumber)" -ForegroundColor Gray
Write-Host "  ruleDocId   : $ruleDocId" -ForegroundColor Gray
Write-Host "  sourcesDocId: $sourcesDocId" -ForegroundColor Gray
Write-Host "  docId       : $ediDocId" -ForegroundColor White
Write-Host "  status      : $claimStatus" -ForegroundColor $(if ($claimStatus -eq "HasErrors") { "Red" } else { "Green" })
if ($claimStatus -eq "ReadyToSubmit") {
    Write-Host "  queue2      : queued for clearinghouse submission" -ForegroundColor Green
}
if ($PSCmdlet.ParameterSetName -eq "Queue") {
    Write-Host "  queue1      : row $QueueClaimId marked 'Claim for Clearing House'" -ForegroundColor Green
}
if ($issues.Count -gt 0) {
    Write-Host "  issues      :" -ForegroundColor Red
    foreach ($issue in $issues) { Write-Host "    - $issue" -ForegroundColor Red }
}
Write-Host "  Open        : http://localhost:5173/api/getDocument?docId=$ediDocId" -ForegroundColor Cyan
