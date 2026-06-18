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
nt --title "CaseManagementUI" `
   --tabColor "#FF69B4" `
   -d "$root\CaseManagementUI" `
   pwsh
 
# put path on a newline 
#prompt $P$_$G