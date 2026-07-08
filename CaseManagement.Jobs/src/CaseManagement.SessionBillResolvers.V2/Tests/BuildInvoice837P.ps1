# BuildInvoice837P.ps1
# Reads an invoice build spec JSON and chains (P)(AM) pipeline steps to produce a 837P invoice.
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
$projectors = $spec.projectors
$sources    = $spec.sources

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

# Sessions — each appends a Loop 2400 entry
if ($sources.sessions -and $sources.sessions.Count -gt 0) {
    $i = 1
    foreach ($sessionDocId in $sources.sessions) {
        $invoice = Run-Step $sessionDocId $projectors.session $invoice "Session $i ($sessionDocId)"
        $i++
    }
}

# Provider
if ($sources.provider) {
    $invoice = Run-Step $sources.provider $projectors.provider $invoice "Provider"
}

# Payer
if ($sources.payer) {
    $invoice = Run-Step $sources.payer $projectors.payer $invoice "Payer"
}

# Authorization
if ($sources.authorization) {
    $invoice = Run-Step $sources.authorization $projectors.authorization $invoice "Authorization"
}

# Patient
if ($sources.patient) {
    $invoice = Run-Step $sources.patient $projectors.patient $invoice "Patient"
}

# Metadata — build from spec and (M) merge into final invoice
Write-Host ""
Write-Host "Metadata : building from spec..." -ForegroundColor Yellow

$pipelineRunId  = [System.Guid]::NewGuid().ToString()
$sessionSources = @($sources.sessions | ForEach-Object { @{ documentId = $_ } })

$metadataPatch = @{
    metadata = @{
        caseId        = $caseId
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
Write-Host "=== Final invoice ===" -ForegroundColor Cyan
Write-Host "  docId : $invoice" -ForegroundColor White
Write-Host "  Open  : http://localhost:5173/api/getDocument?docId=$invoice" -ForegroundColor Cyan