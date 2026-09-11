# Per-tab reminders — kept here instead of inline between the `wt` continuation lines below,
# since a bare comment line there has no trailing backtick and silently truncates the whole
# chained command (bit this script twice now: once via a stray blank line, once via these).
#   ImportDocs           : dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\SQLEXPRESS;Database=CaseManagement;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"
#   UI                   : npm run dev
#   Claims               : .\BuildQueueForClearingHouse.ps1 -QueueClaimId 1
#   Start core           : dotnet run --launch-profile V

$root = "C:\Users\mastronardif\source\repos\CaseMangement"

wt `
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
   pwsh `
`; `
nt --title "Claims" `
   --tabColor "#5d73be" `
   -d "$root\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2\Tests" `
      pwsh `
`; `
nt --title "Start core" `
   --tabColor "#8c5dbe" `
   -d "$root\WebAppMulti" `
      pwsh
