<#
Opens SSMS on the Case Management mock database on the OpenHost HP.

Fetches the password from ~/casemanagement/.env on the HP over SSH, puts it on the
clipboard, and starts SSMS with the server and login filled in. Paste the password
(Ctrl+V) and click Connect. The clipboard is cleared after 5 minutes.
SSMS 18 and later can't take a password on the command line.

  .\Open-SSMS.ps1            # as sa (admin)
  .\Open-SSMS.ps1 -App       # as cmapp (the app's own login)
  .\Open-SSMS.ps1 -Show      # also print the password in this window
#>
param(
    [switch]$App,
    [switch]$Show,
    [string]$HpHost = "frank@192.168.1.45",
    [string]$Server = "192.168.1.45,1433"
)
$ErrorActionPreference = "Stop"

$login = if ($App) { "cmapp" } else { "sa" }
$key   = if ($App) { "APP_DB_PASSWORD" } else { "MSSQL_SA_PASSWORD" }

$ssms = Get-ChildItem "C:\Program Files*\Microsoft SQL Server Management Studio*\Common7\IDE\ssms.exe",
                      "C:\Program Files*\Microsoft SQL Server Management Studio*\*\Common7\IDE\ssms.exe" -ErrorAction SilentlyContinue |
        Sort-Object { $_.VersionInfo.FileVersionRaw } -Descending | Select-Object -First 1
if (-not $ssms) { throw "SSMS not found under Program Files." }

$line = ssh -o ConnectTimeout=5 $HpHost "grep '^$key=' ~/casemanagement/.env"
if ($LASTEXITCODE -ne 0 -or -not $line) { throw "Couldn't read $key from the HP. Is it on, and does ~/casemanagement/.env exist?" }
$password = $line.Substring($line.IndexOf('=') + 1).Trim()

Set-Clipboard -Value $password

Start-Process $ssms.FullName -ArgumentList "-S", $Server, "-d", "CaseManagement", "-U", $login, "-nosplash"

Write-Host ""
Write-Host "SSMS is opening.  Server: $Server   Login: $login" -ForegroundColor Cyan
Write-Host "The password for '$login' is on the clipboard."
Write-Host "In SSMS: check Login says '$login', CLEAR the Password box, then Ctrl+V and Connect."
Write-Host "First time only: tick 'Trust server certificate' (and 'Remember password' if you like)."
if ($Show) { Write-Host "Password: $password" -ForegroundColor Yellow }
Write-Host "Clipboard clears in 5 minutes."
Remove-Variable password, line

# Clear the clipboard later without keeping this window open.
Start-Process powershell -WindowStyle Hidden -ArgumentList "-NoProfile", "-Command", "Start-Sleep 300; Set-Clipboard -Value ' '"
