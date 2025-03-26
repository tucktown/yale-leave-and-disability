# ESL Scenario Update Scripts

This directory contains scripts for managing and updating ESL scenarios.

## Available Scripts

### run_update.bat

Simple batch file launcher for Windows users to easily run the PowerShell script without having to open a PowerShell window manually.

#### Usage

Simply double-click `run_update.bat` to execute the PowerShell script with the default settings.

### UpdateScenarios.ps1

This PowerShell script automates the process of updating the `scenarios.json` file based on data from the `ESLScenarios.csv` file.

#### Usage

```powershell
# Run with default settings
.\UpdateScenarios.ps1

# Run with custom file paths
.\UpdateScenarios.ps1 -jsonFilePath "../custom/path/scenarios.json" -csvFilePath "../custom/path/scenarios.csv"

# Run in test mode (doesn't save changes)
.\UpdateScenarios.ps1 -testMode
```

#### Parameters

- `jsonFilePath`: Path to the scenarios.json file (default: "../ESLFeeder/Config/scenarios.json")
- `csvFilePath`: Path to the ESLScenarios.csv file (default: "../ESLScenarios.csv")
- `testMode`: Run in test mode without writing changes to the JSON file

### update_scenarios.py

Python alternative for updating the `scenarios.json` file based on the `ESLScenarios.csv` file.

#### Requirements

- Python 3.6 or higher

#### Usage

```bash
# Run with default settings
python update_scenarios.py

# Run with custom file paths
python update_scenarios.py --json "../custom/path/scenarios.json" --csv "../custom/path/scenarios.csv"

# Run in test mode (doesn't save changes)
python update_scenarios.py --test
```

#### Parameters

- `--json`: Path to the scenarios.json file (default: "../ESLFeeder/Config/scenarios.json")
- `--csv`: Path to the ESLScenarios.csv file (default: "../ESLScenarios.csv")
- `--test`: Run in test mode without writing changes to the JSON file

## Features

- Creates automatic backups of the scenarios.json file before making changes
- Provides detailed logging of all changes
- Handles the conversion from process_level to process_levels array
- Updates scenario names, descriptions, reason codes, and conditions

## Notes

- The scripts currently do not update the "Updates" section of scenarios as it requires more complex parsing
- They will only update scenarios that exist in both the CSV and JSON file 