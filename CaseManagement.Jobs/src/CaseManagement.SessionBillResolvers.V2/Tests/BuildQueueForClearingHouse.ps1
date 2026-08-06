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
            SubmitterName, SubmitterIdentifier,
            SenderIdQualifier, SenderId,
            FunctionalIdentifierCode, VersionIdentifier, TestIndicator,
            BillingProviderName, BillingProviderNPI, BillingProviderTaxonomy, TaxId,
            Address1, Address2, City, State, Zip
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
        senderIdQualifier         = $row.SenderIdQualifier
        senderId                  = $row.SenderId
        functionalIdentifierCode  = $row.FunctionalIdentifierCode
        versionIdentifier         = $row.VersionIdentifier
        testIndicator             = $row.TestIndicator
        billingProviderName       = $row.BillingProviderName
        billingProviderNPI        = $row.BillingProviderNPI
        billingProviderTaxonomy   = $row.BillingProviderTaxonomy
        taxId                     = $row.TaxId
        address1                  = $row.Address1
        address2                  = $row.Address2
        city                      = $row.City
        state                     = $row.State
        zip                       = $row.Zip
    }
}

function Get-InsuranceCoverage {
    param([int]$CaseId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        SELECT TOP 1
            IC.InsuranceCoverageId, IC.PayerId, IC.MemberId, IC.GroupNumber, IC.SubscriberName,
            IC.RelationshipCode, IC.SubscriberFirstName, IC.SubscriberLastName, IC.SubscriberMiddleName,
            IC.SubscriberDateOfBirth, IC.SubscriberGender,
            PP.PayerName
        FROM   [cases].[InsuranceCoverage] IC
        LEFT JOIN [cases].[Payer] PP ON PP.Id = IC.PayerId
        WHERE  IC.CaseId = @CaseId
        ORDER  BY IC.InsuranceCoverageId DESC
    "
    $cmd.Parameters.AddWithValue("@CaseId", $CaseId) | Out-Null
    $reader = $cmd.ExecuteReader()
    $table  = New-Object System.Data.DataTable
    $table.Load($reader)
    $conn.Close()

    if ($table.Rows.Count -eq 0) { return $null }
    $row = $table.Rows[0]

    return @{
        insuranceCoverageId    = [int]$row.InsuranceCoverageId
        payerId                = [int]$row.PayerId
        payerName              = $row.PayerName
        memberId               = $row.MemberId
        groupNumber            = $row.GroupNumber
        subscriberName         = $row.SubscriberName
        relationshipCode       = $row.RelationshipCode
        subscriberFirstName    = $row.SubscriberFirstName
        subscriberLastName     = $row.SubscriberLastName
        subscriberMiddleName   = $row.SubscriberMiddleName
        subscriberDateOfBirth  = if ($row.SubscriberDateOfBirth -is [DBNull]) { $null } else { ([datetime]$row.SubscriberDateOfBirth).ToString("yyyyMMdd") }
        subscriberGender       = $row.SubscriberGender
    }
}

# Payer is a business entity, not a document (SourceDocumentId/JsonDocumentId on cases.Payer
# were renamed to *_REMOVEME) — queried directly by PayerId, same as InsuranceCoverage/PracticeConfiguration.
function Get-PayerEDI {
    param([int]$PayerId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    # Payer.PayerCode/CodeQualifier are legacy — PayerEDI is the authoritative source for the
    # payer's own EDI identifier. Receiver/clearinghouse fields were removed from this table
    # (renamed *REMOVE) — where that identity lives now (for ISA08/GS03/1000B) is still open.
    $cmd.CommandText = "
        SELECT TOP 1 PayerIdentifier, PayerIdentifierQualifier
        FROM   [cases].[PayerEDI]
        WHERE  PayerId = @PayerId AND IsActive = 1
        ORDER  BY PayerEDIId DESC
    "
    $cmd.Parameters.AddWithValue("@PayerId", $PayerId) | Out-Null
    $reader = $cmd.ExecuteReader()
    $table  = New-Object System.Data.DataTable
    $table.Load($reader)
    $conn.Close()

    if ($table.Rows.Count -eq 0) { return $null }
    $row = $table.Rows[0]

    return @{
        payerIdentifier          = $row.PayerIdentifier
        payerIdentifierQualifier = $row.PayerIdentifierQualifier
    }
}

# Rendering provider (loop 2310B) is resolved from the claimed session's own JSON content
# (session.provider.clinicianUsername), matched against cases.Provider — a business entity now
# (JsonDocumentId renamed *REMOVEME on this table too; cases.RenderingProvider was dropped —
# all providers, billing org and individual clinicians alike, live in cases.Provider).
function Get-RenderingProvider {
    param([int]$SessionJsonDocId)

    $sessionDoc = Invoke-RestMethod -Uri "http://localhost:5173/api/getDocument?docId=$SessionJsonDocId"
    $username = $sessionDoc.provider.clinicianUsername
    if ([string]::IsNullOrWhiteSpace($username)) { return $null }

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        SELECT TOP 1 Id, FirstName, LastName, NPI, TaxonomyCode
        FROM   [cases].[Provider]
        WHERE  ClinicianUsername = @Username AND IsActive = 1
    "
    $cmd.Parameters.AddWithValue("@Username", $username) | Out-Null
    $reader = $cmd.ExecuteReader()
    $table  = New-Object System.Data.DataTable
    $table.Load($reader)
    $conn.Close()

    if ($table.Rows.Count -eq 0) { return $null }
    $row = $table.Rows[0]

    return @{
        providerId   = [int]$row.Id
        firstName    = $row.FirstName
        lastName     = $row.LastName
        npi          = $row.NPI
        taxonomyCode = $row.TaxonomyCode
    }
}

# Patient is a business entity, not a document (SourceDocumentId/JsonDocumentId on cases.Patient
# were renamed *REMOVEME too). Feeds 2010CA (Patient) only — 2010BA (Subscriber) comes from
# InsuranceCoverage, never from here, even when patient and subscriber are the same person.
function Get-Patient {
    param([int]$CaseId)

    $settings = Get-Content "$projectDir\appsettings.json" | ConvertFrom-Json
    $connStr  = $settings.ConnectionStrings.DefaultConnection

    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "
        SELECT TOP 1 Id, FirstName, LastName, DateOfBirth, Gender
        FROM   [cases].[Patient]
        WHERE  CaseId = @CaseId AND IsActive = 1
        ORDER  BY Id DESC
    "
    $cmd.Parameters.AddWithValue("@CaseId", $CaseId) | Out-Null
    $reader = $cmd.ExecuteReader()
    $table  = New-Object System.Data.DataTable
    $table.Load($reader)
    $conn.Close()

    if ($table.Rows.Count -eq 0) { return $null }
    $row = $table.Rows[0]

    return @{
        patientId   = [int]$row.Id
        firstName   = $row.FirstName
        lastName    = $row.LastName
        dateOfBirth = if ($row.DateOfBirth -is [DBNull]) { $null } else { ([datetime]$row.DateOfBirth).ToString("yyyyMMdd") }
        gender      = $row.Gender
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

# Rendering Provider (loop 2310B only) — resolved from the first claimed session's own
# clinicianUsername. Billing provider (1000A/2010AA) now comes entirely from Practice
# Configuration, which already carried those fields — Provider837P is retired.
$renderingProviderId = $null
if ($spec.sessions -and $spec.sessions.Count -gt 0) {
    Write-Host ""
    Write-Host "Rendering Provider : loading for session $($spec.sessions[0])..." -ForegroundColor Yellow
    $renderingProvider = Get-RenderingProvider -SessionJsonDocId $spec.sessions[0]
    if ($renderingProvider) {
        $renderingProviderId = $renderingProvider.providerId
        $renderingProviderJson = $renderingProvider | ConvertTo-Json
        $renderingProviderSave = @{ json = $renderingProviderJson; name = "rendering-provider" } | ConvertTo-Json
        $renderingProviderResp = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $renderingProviderSave
        $renderingProviderDocId = $renderingProviderResp.docId
        $invoice = Run-Step $renderingProviderDocId (Get-ActiveProjectionDocId "RenderingProvider837P") $invoice "Rendering Provider ($renderingProviderDocId)"
    } else {
        Write-Host "  No clinicianUsername on session $($spec.sessions[0]), or no matching active Provider row." -ForegroundColor DarkYellow
    }
}

# Authorization
if ($spec.authorization) {
    $invoice = Run-Step $spec.authorization (Get-ActiveProjectionDocId "Authorization837P") $invoice "Authorization"
}

# Insurance Coverage — a business entity, not a document (same idea as Practice Configuration):
# queried directly by CaseId, LEFT JOINed to Payer for the name (so the payer's name doesn't
# depend on a PayerEDI row existing). Snapshotted into a transient doc to flow through (P)(AM).
# Drives SBR (2000B), the name portion of 2010BB.NM1 (Payer), and 2010BA (Subscriber). Resolved
# before Patient below because RelationshipCode decides whether 2010CA gets populated.
Write-Host ""
Write-Host "Insurance Coverage : loading for case $caseId..." -ForegroundColor Yellow
$insuranceCoverage = Get-InsuranceCoverage -CaseId $caseId
$insuranceCoverageDocId = $null
$payerId = $null
if ($insuranceCoverage) {
    $payerId = $insuranceCoverage.payerId
    $insuranceCoverageJson = $insuranceCoverage | ConvertTo-Json
    $insuranceCoverageSave = @{ json = $insuranceCoverageJson; name = "insurance-coverage" } | ConvertTo-Json
    $insuranceCoverageResp = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $insuranceCoverageSave
    $insuranceCoverageDocId = $insuranceCoverageResp.docId
    $invoice = Run-Step $insuranceCoverageDocId (Get-ActiveProjectionDocId "InsuranceCoverage837P") $invoice "Insurance Coverage ($insuranceCoverageDocId)"
} else {
    Write-Host "  No InsuranceCoverage row for case $caseId — SBR will be empty." -ForegroundColor DarkYellow
}

# Payer EDI — the payer's own EDI identifier (2010BB.NM1.NM108/NM109). Receiver/clearinghouse
# identity (1000B.NM1, ISA08/GS03) is unresolved right now — PayerEDI's Receiver* fields were
# removed and PayerSubmissionConfiguration doesn't carry a receiver id either. Not wired here
# until that's settled.
if ($insuranceCoverage -and $payerId) {
    Write-Host ""
    Write-Host "Payer EDI : loading for PayerId $payerId..." -ForegroundColor Yellow
    $payerEDI = Get-PayerEDI -PayerId $payerId
    if ($payerEDI) {
        $payerJson = $payerEDI | ConvertTo-Json
        $payerSave = @{ json = $payerJson; name = "payer-edi" } | ConvertTo-Json
        $payerResp = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $payerSave
        $payerDocId = $payerResp.docId
        $invoice = Run-Step $payerDocId (Get-ActiveProjectionDocId "Payer837P") $invoice "Payer EDI ($payerDocId)"
    } else {
        Write-Host "  No active PayerEDI row for PayerId $payerId." -ForegroundColor DarkYellow
    }
}

# Patient (loop 2010CA) — populated only when the patient isn't the subscriber. This is the
# only place RelationshipCode is interpreted; X12Writer just reacts to whether 2010CA has
# content and never re-derives dependent-vs-self on its own.
$patientId = $null
if ($insuranceCoverage -and $insuranceCoverage.relationshipCode -and $insuranceCoverage.relationshipCode -ne "18") {
    Write-Host ""
    Write-Host "Patient (2010CA) : relationshipCode=$($insuranceCoverage.relationshipCode), loading for case $caseId..." -ForegroundColor Yellow
    $patient = Get-Patient -CaseId $caseId
    if ($patient) {
        $patientId = $patient.patientId
        $patientJson = $patient | ConvertTo-Json
        $patientSave = @{ json = $patientJson; name = "patient" } | ConvertTo-Json
        $patientResp = Invoke-RestMethod -Uri "http://localhost:5173/api/saveWorkflow" -Method Post -ContentType "application/json" -Body $patientSave
        $patientDocId = $patientResp.docId
        $invoice = Run-Step $patientDocId (Get-ActiveProjectionDocId "Patient837P") $invoice "Patient ($patientDocId)"
    } else {
        Write-Host "  No Patient row for case $caseId." -ForegroundColor DarkYellow
    }
} else {
    Write-Host ""
    Write-Host "Patient (2010CA) : relationshipCode=18 (self) or no InsuranceCoverage — skipping." -ForegroundColor DarkGray
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
    renderingProviderId   = $renderingProviderId
    payerId               = $payerId
    authorization         = if ($spec.authorization)  { $spec.authorization } else { $null }
    patientId             = $patientId
    insuranceCoverageId   = if ($insuranceCoverage)   { $insuranceCoverage.insuranceCoverageId } else { $null }
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
            authorization = if ($spec.authorization) { $spec.authorization } else { $null }
            patientId     = $patientId
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
