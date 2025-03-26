param (
    [string]$jsonFilePath = "../ESLFeeder/Config/scenarios.json",
    [string]$csvFilePath = "../ESLScenarios.csv",
    [switch]$testMode = $false,
    [switch]$previewMode = $true
)

Write-Host "ESL Scenarios Update Tool" -ForegroundColor Green
Write-Host "------------------------" -ForegroundColor Green

# Verify files exist
if (-not (Test-Path $jsonFilePath)) {
    Write-Host "Error: JSON file not found at $jsonFilePath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $csvFilePath)) {
    Write-Host "Error: CSV file not found at $csvFilePath" -ForegroundColor Red
    exit 1
}

# Function to show field differences
function Show-FieldDiff {
    param (
        [string]$fieldName,
        $oldValue,
        $newValue
    )
    
    $oldStr = if ($null -eq $oldValue) { "(none)" } elseif ($oldValue -is [array]) { $oldValue -join ", " } else { "$oldValue" }
    $newStr = if ($null -eq $newValue) { "(none)" } elseif ($newValue -is [array]) { $newValue -join ", " } else { "$newValue" }
    
    if ($oldStr -ne $newStr) {
        Write-Host "    ${fieldName}:" -ForegroundColor Yellow
        Write-Host "      - ${oldStr}" -ForegroundColor Red
        Write-Host "      + ${newStr}" -ForegroundColor Green
    }
}

# Create backup of JSON file
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "$jsonFilePath.backup.$timestamp"
Copy-Item -Path $jsonFilePath -Destination $backupPath
Write-Host "Created backup at $backupPath" -ForegroundColor Yellow

# 1. Read and parse the existing JSON
try {
    $jsonContent = Get-Content -Path $jsonFilePath -Raw | ConvertFrom-Json
    Write-Host "Successfully loaded JSON file" -ForegroundColor Green
} catch {
    Write-Host "Error parsing JSON file: $_" -ForegroundColor Red
    exit 1
}

# Helper function to safely get CSV value with proper parsing
function Get-SafeCsvValue {
    param ($row, $index)
    try {
        # First, handle any line breaks within quoted values
        $row = $row -replace "`r`n", " "
        $row = $row -replace "`n", " "
        
        # Split the row by comma, but respect quoted values
        $values = $row -replace '^"|"$' -split '","'
        if ($index -lt $values.Count) {
            $value = $values[$index].Trim()
            # Clean up any remaining quotes and whitespace
            $value = $value -replace '^"|"$', ''
            return $value
        }
        return ""
    } catch {
        Write-Host "Error parsing CSV value at index $index : $_" -ForegroundColor Red
        return ""
    }
}

# Helper function to parse process levels
function Get-ProcessLevels {
    param ($value)
    try {
        $levels = @()
        $parts = $value -split '\s*,\s*'
        foreach ($part in $parts) {
            $trimmed = $part.Trim()
            if ([int]::TryParse($trimmed, [ref]$null)) {
                $level = [int]$trimmed
                if ($level -gt 0) {
                    $levels += $level
                }
            }
        }
        return $levels
    } catch {
        return @()
    }
}

# 2. Read and parse the CSV
try {
    $csvRaw = Get-Content -Path $csvFilePath -Raw
    
    # Split into rows, handling CRLF and LF
    $rows = $csvRaw -split "(?:\r\n|\n)"
    
    # Clean up rows and remove empty ones
    $rows = $rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    
    # Parse header to get scenario IDs
    $header = $rows[0] -split ','
    $scenarioIds = $header[1..$header.Length] | ForEach-Object { $_.Trim() }
    
    Write-Host "Successfully loaded CSV file with $($scenarioIds.Length) scenarios" -ForegroundColor Green
    
    # Create mapping structure from CSV
    $scenarioMap = @{}
    
    for ($i = 0; $i -lt $scenarioIds.Length; $i++) {
        $id = $scenarioIds[$i]
        if ([int]::TryParse($id, [ref]$null)) {
            $scenarioMap[[int]$id] = @{
                Name = Get-SafeCsvValue $rows[1] ($i + 1)
                Description = Get-SafeCsvValue $rows[2] ($i + 1)
                ProcessLevels = Get-ProcessLevels (Get-SafeCsvValue $rows[3] ($i + 1))
                ReasonCode = Get-SafeCsvValue $rows[4] ($i + 1)
                RequiredConditions = (Get-SafeCsvValue $rows[5] ($i + 1) -split '\s*,\s*' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
                ForbiddenConditions = (Get-SafeCsvValue $rows[6] ($i + 1) -split '\s*,\s*' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
            }
            
            # Debug output for scenario 16
            if ($id -eq 16) {
                Write-Host "`nDEBUG - CSV Parsing for Scenario 16:"
                Write-Host "Row 1 (Name): '$($rows[1])'"
                Write-Host "Row 2 (Description): '$($rows[2])'"
                Write-Host "Row 3 (Process Levels): '$($rows[3])'"
                Write-Host "Row 4 (Reason Code): '$($rows[4])'"
                Write-Host "Row 5 (Required Conditions): '$($rows[5])'"
                Write-Host "Row 6 (Forbidden Conditions): '$($rows[6])'"
                Write-Host "Parsed Values:"
                Write-Host "Name: '$($scenarioMap[[int]$id].Name)'"
                Write-Host "Description: '$($scenarioMap[[int]$id].Description)'"
                Write-Host "ProcessLevels: '$($scenarioMap[[int]$id].ProcessLevels)'"
                Write-Host "ReasonCode: '$($scenarioMap[[int]$id].ReasonCode)'"
                Write-Host "RequiredConditions: '$($scenarioMap[[int]$id].RequiredConditions)'"
                Write-Host "ForbiddenConditions: '$($scenarioMap[[int]$id].ForbiddenConditions)'"
            }
        }
    }
} catch {
    Write-Host "Error parsing CSV file: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}

# 4. Update each scenario in the JSON
$updatedCount = 0
$skippedCount = 0
$previewCount = 0
$maxPreviews = 3  # Number of scenarios to preview

foreach ($scenario in $jsonContent.scenarios) {
    $id = $scenario.id
    if ($scenarioMap.ContainsKey($id)) {
        $csvScenario = $scenarioMap[$id]
        $scenarioName = $scenario.name
        
        # Debug output for scenario 16
        if ($id -eq 16) {
            Write-Host "`nDEBUG - Scenario 16 Comparison:"
            Write-Host "JSON Name: '$($scenario.name)'"
            Write-Host "CSV Name: '$($csvScenario.Name)'"
            Write-Host "JSON Description: '$($scenario.description)'"
            Write-Host "CSV Description: '$($csvScenario.Description)'"
            Write-Host "JSON Process Level: '$($scenario.process_level)'"
            Write-Host "JSON Process Levels: '$($scenario.process_levels)'"
            Write-Host "CSV Process Levels: '$($csvScenario.ProcessLevels)'"
            Write-Host "JSON Reason Code: '$($scenario.reason_code)'"
            Write-Host "CSV Reason Code: '$($csvScenario.ReasonCode)'"
            Write-Host "JSON Required Conditions: '$($scenario.conditions.required)'"
            Write-Host "CSV Required Conditions: '$($csvScenario.RequiredConditions)'"
            Write-Host "JSON Forbidden Conditions: '$($scenario.conditions.forbidden)'"
            Write-Host "CSV Forbidden Conditions: '$($csvScenario.ForbiddenConditions)'"
        }
        
        Write-Host "Processing scenario #${id}: ${scenarioName}" -ForegroundColor Cyan
        
        if ($previewMode -and $previewCount -lt $maxPreviews) {
            Write-Host "  Changes for scenario #${id}:" -ForegroundColor Yellow
            Show-FieldDiff "Name" $scenario.name $csvScenario.Name
            Show-FieldDiff "Description" $scenario.description $csvScenario.Description
            Show-FieldDiff "Reason Code" $scenario.reason_code $csvScenario.ReasonCode
            
            $oldProcessLevels = if ($scenario.PSObject.Properties.Name -contains "process_level") { 
                @($scenario.process_level) 
            } elseif ($scenario.PSObject.Properties.Name -contains "process_levels") { 
                $scenario.process_levels 
            } else { 
                @() 
            }
            Show-FieldDiff "Process Levels" $oldProcessLevels $csvScenario.ProcessLevels
            
            $oldRequired = if ($scenario.conditions) { $scenario.conditions.required } else { @() }
            $oldForbidden = if ($scenario.conditions) { $scenario.conditions.forbidden } else { @() }
            Show-FieldDiff "Required Conditions" $oldRequired $csvScenario.RequiredConditions
            Show-FieldDiff "Forbidden Conditions" $oldForbidden $csvScenario.ForbiddenConditions
            
            Write-Host ""
            $previewCount++
        }
        
        # Update the fields we care about
        $scenario.name = $csvScenario.Name
        $scenario.description = $csvScenario.Description
        $scenario.reason_code = $csvScenario.ReasonCode
        
        # If process_level exists, replace with process_levels
        if ($scenario.PSObject.Properties.Name -contains "process_level") {
            Write-Host "  Converting process_level to process_levels" -ForegroundColor Yellow
            $scenario.PSObject.Properties.Remove("process_level")
            $scenario | Add-Member -MemberType NoteProperty -Name "process_levels" -Value $csvScenario.ProcessLevels
        } elseif ($scenario.PSObject.Properties.Name -contains "process_levels") {
            $scenario.process_levels = $csvScenario.ProcessLevels
        }
        
        # Update conditions
        if (-not $scenario.conditions) {
            $scenario | Add-Member -MemberType NoteProperty -Name "conditions" -Value @{
                required = @()
                forbidden = @()
            }
        }
        $scenario.conditions.required = $csvScenario.RequiredConditions
        $scenario.conditions.forbidden = $csvScenario.ForbiddenConditions
        
        $updatedCount++
        Write-Host "  Updated scenario #${id}" -ForegroundColor Green
    } else {
        Write-Host "Skipping scenario #${id}: ${scenario.name} (not found in CSV)" -ForegroundColor Yellow
        $skippedCount++
    }
}

Write-Host "`nUpdate Summary:" -ForegroundColor Green
Write-Host "- Found $($scenarioMap.Count) scenarios in CSV" -ForegroundColor Cyan
Write-Host "- Updated $updatedCount scenarios in JSON" -ForegroundColor Cyan
Write-Host "- Skipped $skippedCount scenarios (not found in CSV)" -ForegroundColor Yellow

# 5. Write updated JSON back to file
if ($testMode) {
    Write-Host "Test mode enabled - not writing changes to file" -ForegroundColor Yellow
} else {
    try {
        $jsonContent | ConvertTo-Json -Depth 20 | Set-Content -Path $jsonFilePath
        Write-Host "Successfully wrote updated JSON to $jsonFilePath" -ForegroundColor Green
    } catch {
        Write-Host "Error writing to JSON file: $_" -ForegroundColor Red
        Write-Host "Changes not saved. Original file preserved." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "Script completed successfully" -ForegroundColor Green 