# Input file from SSMS
$input = "C:\Users\mastronardif\Documents\script.sql"
        # C:\Users\mastronardif\Documents\script.sql
# Output file sorted
$output = "C:\Users\mastronardif\Documents\CaseManagement_Sorted.sql"


# Read all lines
$lines = Get-Content $input

# Sort alphabetically
$sorted = $lines | Sort-Object

# Write sorted file
$sorted | Out-File $output -Encoding UTF8
