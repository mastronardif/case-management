$root = "C:\Users\mastronardif\source\repos\CaseMangement"

wt `
#> dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\SQLEXPRESS;Database=CaseManagement;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"
nt --title "ImportDocs" `
   --tabColor "#808080" `
   -d "$root\WebAppMulti\Database\Scripts\ImportDocs" `
   pwsh `
`; `
#
nt --title "SessionBillResolvers" `
   --tabColor "#00AA00" `
   -d "$root\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2" `
   pwsh `
`; `
# C:\Users\mastronardif\source\repos\CaseMangement\CaseManagementUI> npm run dev  
nt --title "UI" `
   --tabColor "#FF69B4" `
   -d "$root\CaseManagementUI" `
   pwsh `
`; `
#> .\BuildQueueForClearingHouse.ps1 -QueueClaimId 1
nt --title "Claims" `
   --tabColor "#5d73be" `
   -d "$root\CaseManagement.Jobs\src\CaseManagement.SessionBillResolvers.V2\Tests" `
      pwsh `
`; `
#> dotnet run --launch-profile V
nt --title "Start" `
   --tabColor "#8c5dbe" `
   -d "$root\WebAppMulti" `
      pwsh `
`; `
   
# todo: C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti dotnet run --launch-profile V
#run --launch-profile VS44344" 
# put path on a newline 
#prompt $P$_$G

#pwsh -NoExit -Command "function prompt { ""$PWD`n> "" }"
# PS.\BuildQueueForClearingHouse.ps1 -QueueClaimId 4
