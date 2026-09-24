# Per-tab reminders — kept here instead of inline between the `wt` continuation lines below,
# since a bare comment line there has no trailing backtick and silently truncates the whole
# chained command (bit this script twice now: once via a stray blank line, once via these).
#   ImportDocs           : dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\SQLEXPRESS;Database=CaseManagement;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"
#   UI                   : npm run dev                           (pre-typed at the prompt, press Enter)
#   Claims               : .\BuildQueueForClearingHouse.ps1 -QueueClaimId 1
#   Start core           : dotnet run --launch-profile VS44344   (pre-typed at the prompt, press Enter)
#   Jobs                 : streamlit run jobs_hub.py             (pre-typed at the prompt, press Enter)

$root = "C:\Users\mastronardif\source\repos\CaseMangement"

# Builds the -Command string that pre-types a command at a tab's first prompt WITHOUT running it:
# a one-shot PowerShell idle event calls PSReadLine's Insert once the prompt is up. The command
# must not contain single quotes. Built into variables up here, not inline in the wt chain below.
function New-PreType([string]$Command) {
    "Register-EngineEvent -SourceIdentifier PowerShell.OnIdle -MaxTriggerCount 1 -Action { [Microsoft.PowerShell.PSConsoleReadLine]::Insert('$Command') } | Out-Null"
}
$coreType = New-PreType 'dotnet run --launch-profile VS44344'
$uiType   = New-PreType 'npm run dev'
$jobsType = New-PreType 'streamlit run jobs_hub.py'

wt `
nt --title "Start core" `
   --tabColor "#36aeea" `
   -d "$root\WebAppMulti" `
   pwsh -NoExit -Command $coreType `
`; `
nt --title "ImportDocs" `
   --tabColor "#808080" `
   -d "$root\WebAppMulti\Database\Scripts\ImportDocs" `
   pwsh `
`; `
nt --title "SessionBillResolvers" `
   --tabColor "#00AA00" `
   -d "$root\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2" `
   pwsh `
`; `
nt --title "UI" `
   --tabColor "#FF69B4" `
   -d "$root\CaseManagementUI" `
   pwsh -NoExit -Command $uiType `
`; `
nt --title "Claims" `
   --tabColor "#5d73be" `
   -d "$root\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2\Tests" `
      pwsh `
`; `
nt --title "Jobs" `
   --tabColor "#5d73be" `
   --colorScheme "Campbell Powershell" `
   -d "$root\WebAppMulti\Database\Scripts" `
   pwsh -NoExit -Command $jobsType