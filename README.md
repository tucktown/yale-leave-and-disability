# ESL Feeder

A C# application for processing leave and disability payment data using a flexible scenario-based rules engine.

## Overview

The ESL Feeder processes CSV files containing leave and disability payment data by applying business rules through a scenario-based framework. The application's key strength is its flexible scenario and conditions system, which allows for:

- **Declarative Business Rules**: Business logic is defined in JSON configuration, making it easier to maintain and modify without code changes
- **Composable Conditions**: Complex business rules are built from simple, reusable conditions
- **Process Level Support**: Rules can be customized based on GL Company/Process Level
- **Extensible Framework**: New conditions and scenarios can be added without modifying core logic
- **Comprehensive Logging**: Detailed logging and debug mode support for troubleshooting

## Technical Flow

### 1. Data Ingestion
- CSV files are loaded and validated. This differs from PRD workflow, which will connect to live databases
- Data is cleaned and normalized through `DataCleaningService`
- Required columns are added/validated
- Data types are standardized

### 2. Variable Calculation
- `VariableCalculator` processes raw data into calculated variables
- Handles various leave-related calculations (PTO, Sick Leave, etc.)
- Supports both DataRow and Dictionary input formats
- Calculates derived values needed for scenario evaluation

### 3. Scenario Processing
1. **Initial Validation**
   - Validates required fields (CLAIM_ID, PAY_START_DATE, etc.)
   - Ensures dates are valid and in correct order
   - Verifies reason codes against allowed values

2. **Scenario Matching**
   - Filters scenarios based on:
     - Reason code (case-insensitive)
     - Process level
     - Active status
   - Orders scenarios by ID for deterministic selection
   - Scenarios are designed to be written in a way that it mutually exclusive (i.e. a single claim should not be able to match to multiple scenarios)

3. **Condition Evaluation**
   - Evaluates required conditions (all must be true)
   - Evaluates excluded conditions (none must be true)
   - Stores evaluation results for debugging
   - First matching scenario is selected

4. **Scenario Calculation**
   - `ScenarioCalculator` processes the selected scenario
   - Applies configured updates to the record as outlined in scenarios.json
   - Handles various output types (numeric, string, etc.)
   - Supports variable references in calculations

### 4. Output Generation
- Processed records are saved back to CSV
- Output includes:
  - Original data
  - Scenario information
  - Calculated values
  - Condition evaluation results (in debug mode)

## Key Components

### Scenario Configuration
Scenarios are defined in `Config/scenarios.json`:
```json
{
  "scenarios": [
    {
      "id": 1,
      "name": "Scenario Name",
      "reasonCode": "REASON_CODE",
      "processLevels": [1, 2, 3],
      "conditions": {
        "requiredConditions": ["C6", "C7"],
        "excludedConditions": ["C8"]
      },
      "updates": {
        "output_field": {
          "type": "double",
          "source": "variables.WeeklyWage"
        }
      }
    }
  ]
}
```

### Conditions
Conditions are implemented as classes inheriting from `ICondition`:
```csharp
public class C6 : ICondition
{
    public string Name => "C6";
    public string Description => "STD is active";
    
    public bool Evaluate(DataRow row, LeaveVariables variables)
    {
        // Implementation
    }
}
```

### Variable Calculator
Handles various leave-related calculations:
- PTO availability and usage
- Basic sick leave calculations
- STD (Short Term Disability) calculations
- CTPL (Connecticut Paid Leave) calculations
- Wage calculations

## Development

### Prerequisites
- .NET 6.0 or later
- Visual Studio 2022 or later
- Access to scenarios.json configuration

### Building
```bash
dotnet build
```

### Running
```bash
dotnet run
```

### Testing
```bash
dotnet test
```

### Debug Mode
Enable debug mode for detailed logging:
- Condition evaluation results
- Scenario matching details
- Variable calculation steps
- Processing statistics

## Configuration

### scenarios.json
- Defines business rules and scenarios
- Supports multiple process levels
- Configurable conditions and updates
- Validates on load
- Scenario criteria and outputs can be updated without changes to main program code

### Logging
- Console logging by default
- Debug mode for detailed information
- Error tracking and reporting

## Error Handling
- Comprehensive input validation
- Graceful handling of missing/invalid data
- Detailed error messages
- Logging of processing steps