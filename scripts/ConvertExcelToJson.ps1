param (
    [string]$excelFilePath = "$PSScriptRoot/../ESLScenarios-Excel.xlsx",
    [string]$outputFilePath = "$PSScriptRoot/../ESLScenarios.json"
)

Write-Host "Excel to JSON Converter" -ForegroundColor Green
Write-Host "---------------------" -ForegroundColor Green

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

# Helper function to parse conditions
function Get-Conditions {
    param ($value)
    try {
        return ($value -split '\s*,\s*' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    } catch {
        return @()
    }
}

# Helper function to parse updates
function Get-Updates {
    param ($value)
    try {
        $updates = @{}
        $lines = $value -split "`n"
        foreach ($line in $lines) {
            $line = $line.Trim()
            if ($line -match '^([^:]+):\s*(.+)$') {
                $key = $matches[1].Trim()
                $val = $matches[2].Trim()
                
                # Convert numeric values
                if ($val -match '^\d+$') {
                    $val = [int]$val
                }
                # Convert null values
                elseif ($val -eq 'NULL') {
                    $val = $null
                }
                # Convert special values
                elseif ($val -eq 'SCHED_HRS') {
                    $val = 'SCHED_HRS'
                }
                elseif ($val -eq 'CURRENT_DATE') {
                    $val = 'CURRENT_DATE'
                }
                # Handle expressions like "SCHED_HRS * 0.6"
                elseif ($val -match 'SCHED_HRS \* (\d+\.?\d*)') {
                    $val = @{
                        type = "expression"
                        base = "SCHED_HRS"
                        multiplier = [double]$matches[1]
                    }
                }
                
                $updates[$key] = $val
            }
        }
        return $updates
    } catch {
        Write-Host "Error parsing updates section: $_" -ForegroundColor Red
        return @{}
    }
}

try {
    # Create Excel COM object
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    
    Write-Host "Opening Excel file: $excelFilePath" -ForegroundColor Yellow
    $workbook = $excel.Workbooks.Open($excelFilePath)
    $sheet = $workbook.Sheets(1)
    $usedRange = $sheet.UsedRange
    
    # Create the JSON structure
    $jsonData = @{
        version = "1.0"
        metadata = @{
            last_updated = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            source_file = $excelFilePath
        }
        scenarios = @()
    }
    
    # Get the number of columns (scenarios)
    $colCount = $usedRange.Columns.Count
    
    Write-Host "Processing scenarios from Excel..." -ForegroundColor Yellow
    
    # Process each column (scenario) starting from column B (index 2)
    for ($col = 2; $col -le $colCount; $col++) {
        # Get scenario ID from first row
        $id = $sheet.Cells(1, $col).Text
        if ([int]::TryParse($id, [ref]$null)) {
            $scenario = @{
                id = [int]$id
                name = $sheet.Cells(2, $col).Text
                description = $sheet.Cells(3, $col).Text
                process_levels = Get-ProcessLevels ($sheet.Cells(4, $col).Text)
                reason_code = $sheet.Cells(5, $col).Text
                conditions = @{
                    required = Get-Conditions ($sheet.Cells(6, $col).Text)
                    forbidden = Get-Conditions ($sheet.Cells(7, $col).Text)
                }
                updates = Get-Updates ($sheet.Cells(8, $col).Text)
            }
            
            $jsonData.scenarios += $scenario
            
            # Debug output for scenario 16
            if ($id -eq 16) {
                Write-Host "`nDEBUG - Scenario 16 Data:"
                $scenario | ConvertTo-Json -Depth 10 | Write-Host
            }
        }
    }
    
    # Clean up Excel
    $workbook.Close($false)
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel)
    
    # Convert to JSON with proper formatting
    $jsonContent = $jsonData | ConvertTo-Json -Depth 10
    
    # Write to file
    $jsonContent | Out-File -FilePath $outputFilePath -Encoding UTF8
    Write-Host "`nSuccessfully created JSON file at: $outputFilePath" -ForegroundColor Green
    
} catch {
    Write-Host "Error processing Excel file: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    
    # Clean up Excel if it's still open
    if ($excel) {
        $workbook.Close($false)
        $excel.Quit()
        [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel)
    }
    
    exit 1
} 