# tree-clean.ps1 - ASCII directory tree respecting .filetreeignore
# Usage: .\tree-clean.ps1              (scans repo root)
#        .\tree-clean.ps1 WebAppMulti  (scans a subfolder)
param(
    [string]$Folder = "",
    [switch]$Help
)

if ($Help -or -not $Folder) {
    Write-Host ""
    Write-Host "tree-clean.ps1 - Generate an ASCII directory tree, respecting .filetreeignore"
    Write-Host ""
    Write-Host "USAGE:"
    Write-Host "  .\tree-clean.ps1 .                      Scan repo root  -> folder-structure.txt"
    Write-Host "  .\tree-clean.ps1 <Folder>               Scan subfolder  -> <leafname>-structure.txt"
    Write-Host "  .\tree-clean.ps1 -Help                  Show this help"
    Write-Host ""
    Write-Host "EXAMPLES:"
    Write-Host "  .\tree-clean.ps1 ."
    Write-Host "  .\tree-clean.ps1 WebAppMulti"
    Write-Host "  .\tree-clean.ps1 WebAppMulti\CaseManagement.ArchitectureStarter"
    Write-Host "  .\tree-clean.ps1 WebAppMulti\CaseManagement.ArchitectureStarter\src"
    Write-Host ""
    Write-Host "IGNORE FILE:"
    Write-Host "  Edit .filetreeignore at the repo root to exclude folders/files (same syntax as .gitignore)"
    Write-Host ""
    return
}

$repoRoot   = Get-Location
$targetPath = if ($Folder) { (Resolve-Path (Join-Path $repoRoot $Folder)).Path } else { "$repoRoot" }
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
