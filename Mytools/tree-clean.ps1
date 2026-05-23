# tree-clean.ps1 - ASCII directory tree respecting .filetreeignore
# Run from anywhere. Folder is relative to the repo root, or pass an absolute path.
param(
    [string]$Folder = "",
    [switch]$Help
)

if ($Help -or -not $Folder) {
    Write-Host ""
    Write-Host "tree-clean.ps1 - Generate an ASCII directory tree, respecting .filetreeignore"
    Write-Host ""
    Write-Host "USAGE (run from anywhere, including Mytools):"
    Write-Host "  .\tree-clean.ps1 .                           Scan repo root"
    Write-Host "  .\tree-clean.ps1 WebAppMulti                 Scan subfolder by name"
    Write-Host "  .\tree-clean.ps1 C:\full\path\to\folder      Scan by absolute path"
    Write-Host "  .\tree-clean.ps1 -Help                       Show this help"
    Write-Host ""
    Write-Host "OUTPUT: written to current directory as <foldername>-structure.txt"
    Write-Host ""
    Write-Host "IGNORE FILE: .filetreeignore at repo root"
    Write-Host ""
    return
}

$repoRoot   = Split-Path $PSScriptRoot -Parent
$targetPath = if ($Folder) {
    if ([System.IO.Path]::IsPathRooted($Folder)) { (Resolve-Path $Folder).Path }
    else { (Resolve-Path (Join-Path $repoRoot $Folder)).Path }
} else { "$repoRoot" }
$leafName   = Split-Path $Folder -Leaf
$outputFile = if ($Folder -and $Folder -ne '.') { "$leafName-structure.txt" } else { "folder-structure.txt" }
$ignoreFile = Join-Path $repoRoot ".filetreeignore"

$ignorePatterns = @()
if (Test-Path $ignoreFile) {
    $ignorePatterns = Get-Content $ignoreFile |
        Where-Object { $_ -and $_ -notmatch '^#' -and $_.Trim() -ne '' } |
        ForEach-Object { $_.Trim().TrimEnd('/') }
}

function Test-Ignored([string]$name) {
    foreach ($pattern in $ignorePatterns) {
        if ($name -like $pattern) { return $true }
    }
    return $false
}

$lines = [System.Collections.Generic.List[string]]::new()

function Add-Tree([string]$path, [string]$prefix = '') {
    $children = Get-ChildItem -Path $path -Force -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-Ignored $_.Name) } |
        Sort-Object @{ Expression = { -not $_.PSIsContainer } }, Name

    for ($i = 0; $i -lt $children.Count; $i++) {
        $item   = $children[$i]
        $isLast = ($i -eq $children.Count - 1)

        if ($isLast) {
            $lines.Add("$prefix\---$($item.Name)")
            $childPrefix = "$prefix    "
        } else {
            $lines.Add("$prefix+---$($item.Name)")
            $childPrefix = "$prefix|   "
        }

        if ($item.PSIsContainer) {
            Add-Tree -path $item.FullName -prefix $childPrefix
        }
    }
}

$lines.Add($targetPath)
Add-Tree -path $targetPath

$lines | Set-Content $outputFile -Encoding UTF8
Write-Host "Done. Output saved to $outputFile"
