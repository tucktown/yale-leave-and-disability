# Testing Strategy for ESLFeeder Application

This document outlines a comprehensive testing strategy for the ESLFeeder application.

## 1. Types of Tests

### Unit Tests
- **Purpose**: Test individual components in isolation
- **Scope**: Single classes/methods
- **Best Practice**: Keep focused on one behavior per test, use mocks for dependencies

### Integration Tests
- **Purpose**: Test how components work together
- **Scope**: Multiple components interacting with each other
- **Best Practice**: Test realistic scenarios, minimize mocking

### End-to-End Tests
- **Purpose**: Test the full application flow
- **Scope**: From input to output, testing the entire process
- **Best Practice**: Focus on critical user journeys, keep these tests few but valuable

## 2. Framework Selection

You have both MSTest and xUnit in your project. We recommend:

- **Standardize on one framework** for consistency
- **MSTest** is simpler to start with and has good Visual Studio integration
- **xUnit** is more modern and flexible for advanced scenarios

**Recommendation**: Since you're new to testing, stick with MSTest initially for simplicity.

## 3. Project Structure

```
ESLFeeder.Tests/
├── UnitTests/            
│   ├── Services/         # Tests for service classes
│   ├── Models/           # Tests for model logic
│   └── Utilities/        # Tests for utility classes
├── IntegrationTests/     # Tests that combine multiple components
├── E2ETests/             # Full workflow tests
├── TestData/             # Test data files
├── TestHelpers/          # Shared test utilities
│   ├── Fixtures/         # Reusable test setups
│   └── Mocks/            # Common mock setups
└── TestSetup.cs          # DI and global configuration
```

## 4. Test Implementation Plan

### Phase 1: Unit Tests for Core Services

1. **DataCleaningService**
   - Test each cleaning method (CleanDateTime, CleanNumeric, etc.)
   - Test handling of null/invalid values
   - Test single row vs. table cleaning

2. **VariableCalculator**
   - Test calculation logic
   - Test edge cases (min/max values, nulls)

3. **ScenarioProcessor**
   - Test scenario matching
   - Test condition evaluation
   - Test result generation

### Phase 2: Integration Tests

1. **DataFlow Tests**
   - Test data flow from cleaning to processing
   - Test scenario selection with real configurations

2. **Service Interaction Tests**
   - Test how services interact when combined

### Phase 3: End-to-End Tests

1. **Process File Test**
   - Test processing a complete file
   - Verify output format and values

## 5. Best Practices

### Naming Convention
```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange, Act, Assert
}
```

### Arrange-Act-Assert Pattern
```csharp
// Arrange - Set up the test
var service = new DataCleaningService(mockLogger.Object);
var inputData = new DataTable();
// ... setup test data

// Act - Perform the action being tested
var result = service.CleanData(inputData);

// Assert - Verify the results
Assert.IsNotNull(result);
Assert.AreEqual(expectedValue, actualValue);
```

### Mocking Best Practices
```csharp
// Setup a mock with expected behavior
var mockLogger = new Mock<ILogger<DataCleaningService>>();
mockLogger.Setup(l => l.LogInformation(It.IsAny<string>())); 

// Verify the mock was called as expected
mockLogger.Verify(l => l.LogInformation(It.IsAny<string>()), Times.Once);
```

### Test Data Management
- Use small, focused test data
- Create helper methods to generate test data
- Keep test data separate from production data

## 6. Sample Test Implementation

Here's a sample test for the `DataCleaningService`:

```csharp
[TestClass]
public class DataCleaningServiceTests
{
    private Mock<ILogger<DataCleaningService>> _mockLogger;
    private DataCleaningService _service;

    [TestInitialize]
    public void Setup()
    {
        // Common setup for all tests
        _mockLogger = new Mock<ILogger<DataCleaningService>>();
        _service = new DataCleaningService(_mockLogger.Object);
    }

    [TestMethod]
    public void CleanData_WithValidRow_ReturnsCleanedRow()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Date", typeof(DateTime));
        
        var row = table.NewRow();
        row["ID"] = 123;
        row["Name"] = " Test Value ";  // Extra spaces to test cleaning
        row["Date"] = "2025-01-01";    // String date to test conversion
        table.Rows.Add(row);

        // Act
        var result = _service.CleanData(row);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(123, result["ID"]);
        Assert.AreEqual("Test Value", result["Name"]); // Spaces trimmed
        Assert.AreEqual(new DateTime(2025, 1, 1), result["Date"]); // Converted to DateTime
    }

    [TestMethod]
    public async Task CleanData_WithMultipleRows_ReturnsCleanedTable()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        // Add rows...

        // Act
        var result = await _service.CleanData(table);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(table.Rows.Count, result.Rows.Count);
        // Verify cleaned values...
    }
}
```

## 7. Implementation Timeline

1. **Week 1**: Set up test structure, implement unit tests for DataCleaningService
2. **Week 2**: Implement unit tests for remaining core services
3. **Week 3**: Implement integration tests
4. **Week 4**: Implement end-to-end tests, ensure CI/CD integration

## 8. Next Steps

1. Create the basic folder structure
2. Implement the first unit tests for DataCleaningService
3. Set up a test data generator for consistent test data
4. Review and iterate on test strategy as you learn more 