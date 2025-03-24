using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using ESLFeeder.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ESLFeeder.Services
{
    public class ScenarioProcessor : IScenarioProcessor
    {
        private readonly IVariableCalculator _calculator;
        private readonly IConditionRegistry _conditions;
        private readonly IScenarioConfiguration _scenarioConfiguration;
        private readonly ILogger<ScenarioProcessor> _logger;
        private readonly IScenarioCalculator _scenarioCalculator;
        private readonly IConditionEvaluator _conditionEvaluator;

        public ScenarioProcessor(
            IVariableCalculator calculator,
            IConditionRegistry conditionRegistry,
            IScenarioConfiguration scenarioConfiguration,
            IScenarioCalculator scenarioCalculator,
            ILogger<ScenarioProcessor> logger,
            IConditionEvaluator conditionEvaluator)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _conditions = conditionRegistry ?? throw new ArgumentNullException(nameof(conditionRegistry));
            _scenarioConfiguration = scenarioConfiguration ?? throw new ArgumentNullException(nameof(scenarioConfiguration));
            _scenarioCalculator = scenarioCalculator ?? throw new ArgumentNullException(nameof(scenarioCalculator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        }

        public ProcessResult ProcessLeaveRequest(DataRow leaveData)
        {
            _logger.LogInformation("Processing leave request");

            var result = new ProcessResult();

            try
            {
                // 1. Validate the request
                var validationResult = ValidateLeaveRequest(leaveData);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // 2. Calculate variables
                if (!_calculator.CalculateVariables(leaveData, out var variables))
                {
                    return result.WithError("Failed to calculate required variables");
                }

                // 3. Set scenario identification properties
                variables.ReasonCode = leaveData["REASON_CODE"].ToString().Trim();
                variables.GlCompany = leaveData.Table.Columns.Contains("PROCESS_LEVEL")
                    ? int.Parse(leaveData["PROCESS_LEVEL"].ToString().Trim())
                    : leaveData.Table.Columns.Contains("GLCOMPANY")
                        ? int.Parse(leaveData["GLCOMPANY"].ToString().Trim())
                        : 1;

                return ProcessLeaveRequest(variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave request");
                return result.WithError(ex);
            }
        }

        public ProcessResult ProcessLeaveRequest(LeaveVariables variables)
        {
            if (variables == null)
            {
                return new ProcessResult().WithError("Variables cannot be null");
            }

            try
            {
                _logger.LogInformation("Processing leave request with pre-populated variables");
                
                // Find matching scenario
                var scenario = FindMatchingScenario(variables);
                if (scenario == null)
                {
                    _logger.LogWarning("No matching scenario found for the given variables");
                    return new ProcessResult().WithNoScenarioFound(variables);
                }

                // Process the scenario
                _logger.LogInformation("Processing scenario {ScenarioId}: {ScenarioName}", scenario.Id, scenario.Name);
                
                var result = _scenarioCalculator.Calculate(scenario, variables);
                
                // Set the scenario info
                result.ScenarioId = scenario.Id;
                result.ScenarioName = scenario.Name;
                
                // Add evaluated conditions to the result
                if (variables.EvaluatedConditions.ContainsKey(scenario.Id))
                {
                    result.AddEvaluationDetail(scenario.Id, variables.EvaluatedConditions[scenario.Id]);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave request with variables object");
                return new ProcessResult().WithError($"Error processing leave request: {ex.Message}");
            }
        }

        public ProcessResult ValidateLeaveRequest(DataRow leaveData)
        {
            var result = new ProcessResult();

            try
            {
                if (leaveData == null)
                {
                    return result.WithError("Leave request data cannot be null");
                }

                // Validate required fields
                var requiredFields = new[] { "CLAIM_ID", "PAY_START_DATE", "PAY_END_DATE", "REASON_CODE" };
                foreach (var field in requiredFields)
                {
                    if (string.IsNullOrEmpty(leaveData[field]?.ToString()))
                    {
                        return result.WithError($"Required field {field} is missing or empty");
                    }
                }

                // Validate dates
                if (!DateTime.TryParse(leaveData["PAY_START_DATE"].ToString(), out var startDate) ||
                    !DateTime.TryParse(leaveData["PAY_END_DATE"].ToString(), out var endDate))
                {
                    return result.WithError("Invalid date format in PAY_START_DATE or PAY_END_DATE");
                }

                if (startDate > endDate)
                {
                    return result.WithError("PAY_START_DATE cannot be after PAY_END_DATE");
                }

                // Validate input variables
                if (!_calculator.ValidateInputVariables(leaveData, out string errorMessage))
                {
                    return result.WithError(errorMessage);
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating leave request");
                return result.WithError($"Error validating leave request: {ex.Message}");
            }
        }

        private List<LeaveScenario> GetApplicableScenarios(Dictionary<string, object> leaveData)
        {
            var reasonCode = leaveData.ContainsKey("REASON_CODE") ? leaveData["REASON_CODE"]?.ToString() : null;
            var processLevel = leaveData.ContainsKey("PROCESS_LEVEL") ? leaveData["PROCESS_LEVEL"]?.ToString() : 
                               leaveData.ContainsKey("GLCOMPANY") ? leaveData["GLCOMPANY"]?.ToString() : null;
            
            if (string.IsNullOrEmpty(reasonCode) || string.IsNullOrEmpty(processLevel))
            {
                _logger.LogWarning("Missing required fields in leave data. REASON_CODE: {ReasonCode}, PROCESS_LEVEL: {ProcessLevel}",
                    reasonCode, processLevel);
                return new List<LeaveScenario>();
            }
            
            int processLevelInt;
            if (!int.TryParse(processLevel, out processLevelInt))
            {
                _logger.LogWarning("Invalid process level: {ProcessLevel}", processLevel);
                processLevelInt = 1; // Default to 1 if invalid
            }
            
            var applicableScenarios = _scenarioConfiguration.GetScenariosForReasonCode(reasonCode, processLevelInt);
            
            _logger.LogInformation("Found {Count} applicable scenarios for reason code {ReasonCode} and process level {ProcessLevel}",
                applicableScenarios.Count, reasonCode, processLevelInt);
            
            return applicableScenarios;
        }

        private bool EvaluateScenarioConditions(DataRow row, LeaveVariables variables, List<EvaluatedCondition> evaluatedConditions)
        {
            bool allRequiredConditionsAreMet = true;
            bool noExcludedConditionsAreTriggered = true;

            foreach (var condition in evaluatedConditions)
            {
                bool isConditionTriggered = _conditionEvaluator.EvaluateCondition(
                    _conditions.GetCondition(condition.ConditionId), 
                    row, 
                    variables);

                condition.Result = isConditionTriggered;
                
                // Store the evaluation result in the variables object for logging purposes
                if (variables.EvaluatedConditions.ContainsKey(condition.ScenarioId))
                {
                    variables.EvaluatedConditions[condition.ScenarioId][condition.ConditionId] = isConditionTriggered;
                }
                else
                {
                    variables.EvaluatedConditions[condition.ScenarioId] = new Dictionary<string, bool>
                    {
                        { condition.ConditionId, isConditionTriggered }
                    };
                }

                if (condition.ConditionType == ConditionType.Required && !isConditionTriggered)
                {
                    _logger.LogDebug("Required condition {ConditionId} not met for scenario {ScenarioId}", 
                        condition.ConditionId, condition.ScenarioId);
                    allRequiredConditionsAreMet = false;
                }
                else if (condition.ConditionType == ConditionType.Excluded && isConditionTriggered)
                {
                    _logger.LogDebug("Excluded condition {ConditionId} triggered for scenario {ScenarioId}", 
                        condition.ConditionId, condition.ScenarioId);
                    noExcludedConditionsAreTriggered = false;
                }
            }

            return allRequiredConditionsAreMet && noExcludedConditionsAreTriggered;
        }

        private bool EvaluateScenarioConditions(Dictionary<string, object> data, LeaveVariables variables, List<EvaluatedCondition> evaluatedConditions)
        {
            bool allRequiredConditionsAreMet = true;
            bool noExcludedConditionsAreTriggered = true;

            foreach (var condition in evaluatedConditions)
            {
                bool isConditionTriggered = _conditionEvaluator.EvaluateCondition(
                    _conditions.GetCondition(condition.ConditionId), 
                    data, 
                    variables);

                condition.Result = isConditionTriggered;
                
                // Store the evaluation result in the variables object for logging purposes
                if (variables.EvaluatedConditions.ContainsKey(condition.ScenarioId))
                {
                    variables.EvaluatedConditions[condition.ScenarioId][condition.ConditionId] = isConditionTriggered;
                }
                else
                {
                    variables.EvaluatedConditions[condition.ScenarioId] = new Dictionary<string, bool>
                    {
                        { condition.ConditionId, isConditionTriggered }
                    };
                }

                if (condition.ConditionType == ConditionType.Required && !isConditionTriggered)
                {
                    _logger.LogDebug("Required condition {ConditionId} not met for scenario {ScenarioId}", 
                        condition.ConditionId, condition.ScenarioId);
                    allRequiredConditionsAreMet = false;
                }
                else if (condition.ConditionType == ConditionType.Excluded && isConditionTriggered)
                {
                    _logger.LogDebug("Excluded condition {ConditionId} triggered for scenario {ScenarioId}", 
                        condition.ConditionId, condition.ScenarioId);
                    noExcludedConditionsAreTriggered = false;
                }
            }

            return allRequiredConditionsAreMet && noExcludedConditionsAreTriggered;
        }

        private Dictionary<string, object> CalculateUpdates(LeaveScenario scenario, LeaveVariables variables)
        {
            var updates = new Dictionary<string, object>();
            var variableDict = new Dictionary<string, double>();

            // Convert LeaveVariables to dictionary
            foreach (var prop in typeof(LeaveVariables).GetProperties())
            {
                if (prop.GetValue(variables) is double value)
                {
                    variableDict[prop.Name] = value;
                }
            }

            foreach (var kvp in scenario.Outputs)
            {
                var fieldName = kvp.Key;
                var output = kvp.Value;

                try
                {
                    if (output.Type == ScenarioOutput.OutputType.Number)
                    {
                        var value = output.Calculate(variableDict);
                        updates[fieldName] = value;
                    }
                    else if (output.Type == ScenarioOutput.OutputType.Boolean)
                    {
                        var value = output.Calculate(variableDict);
                        // Convert the boolean value directly
                        updates[fieldName] = output.BooleanValue;
                    }
                    else if (output.Type == ScenarioOutput.OutputType.String)
                    {
                        if (fieldName == "AUTH_BY")
                        {
                            updates[fieldName] = "ESL";
                        }
                        else if (fieldName == "CHECK_KRONOS")
                        {
                            updates[fieldName] = "Y";
                        }
                        else if (fieldName == "ENTRY_DATE")
                        {
                            updates[fieldName] = DateTime.Now;
                        }
                        else if (fieldName == "EXEC_NOTE" || fieldName == "PHYS_NOTE")
                        {
                            updates[fieldName] = null;
                        }
                        else
                        {
                            updates[fieldName] = output.StringValue;
                        }
                    }
                    _logger.LogInformation($"Calculated {fieldName} = {updates[fieldName]}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error calculating output {fieldName}");
                    throw;
                }
            }

            return updates;
        }

        public LeaveScenario FindMatchingScenario(LeaveVariables variables)
        {
            if (variables == null)
            {
                _logger.LogWarning("Variables cannot be null");
                return null;
            }

            _logger.LogInformation("Finding matching scenario for variables");

            try
            {
                // Create a simple dictionary with the key fields needed for scenario lookup
                var dummyData = new Dictionary<string, object>
                {
                    { "REASON_CODE", variables.ReasonCode ?? string.Empty },
                    { "PROCESS_LEVEL", variables.GlCompany.ToString() }
                };

                // Get applicable scenarios
                var applicableScenarios = GetApplicableScenarios(dummyData);
                if (!applicableScenarios.Any())
                {
                    _logger.LogWarning("No applicable scenarios found for reason code {ReasonCode} and process level {ProcessLevel}",
                        variables.ReasonCode, variables.GlCompany);
                    return null;
                }

                _logger.LogInformation("Found {Count} applicable scenarios. Evaluating conditions...", applicableScenarios.Count());

                // Evaluate each scenario using the variables
                foreach (var scenario in applicableScenarios)
                {
                    bool isMatch = true;
                    var requiredConditionResults = new Dictionary<string, bool>();
                    var excludedConditionResults = new Dictionary<string, bool>();

                    // Evaluate required conditions
                    foreach (var condition in scenario.RequiredConditions)
                    {
                        if (string.IsNullOrEmpty(condition))
                            continue;

                        var conditionObj = _conditions.GetCondition(condition);
                        if (conditionObj == null)
                        {
                            _logger.LogWarning("Condition {ConditionId} not found", condition);
                            continue;
                        }

                        bool result = false;
                        try
                        {
                            result = conditionObj.Evaluate(null, variables);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error evaluating condition {ConditionId}", condition);
                        }

                        requiredConditionResults[condition] = result;
                        
                        if (!result)
                        {
                            isMatch = false;
                            _logger.LogDebug("Required condition {ConditionId} not met", condition);
                        }
                    }

                    // Store required condition results
                    if (requiredConditionResults.Count > 0)
                    {
                        if (!variables.EvaluatedConditions.ContainsKey(scenario.Id))
                        {
                            variables.EvaluatedConditions[scenario.Id] = new Dictionary<string, bool>();
                        }

                        foreach (var kvp in requiredConditionResults)
                        {
                            variables.EvaluatedConditions[scenario.Id][kvp.Key] = kvp.Value;
                        }
                    }

                    // If any required condition failed, skip to the next scenario
                    if (!isMatch)
                    {
                        _logger.LogDebug("Scenario {ScenarioId} does not match - required conditions not met", scenario.Id);
                        continue;
                    }

                    // Evaluate excluded conditions
                    foreach (var condition in scenario.ExcludedConditions)
                    {
                        if (string.IsNullOrEmpty(condition))
                            continue;

                        var conditionObj = _conditions.GetCondition(condition);
                        if (conditionObj == null)
                        {
                            _logger.LogWarning("Condition {ConditionId} not found", condition);
                            continue;
                        }

                        bool result = false;
                        try
                        {
                            result = conditionObj.Evaluate(null, variables);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error evaluating condition {ConditionId}", condition);
                        }

                        excludedConditionResults["Excluded: " + condition] = result;
                        
                        if (result)
                        {
                            isMatch = false;
                            _logger.LogDebug("Excluded condition {ConditionId} was triggered", condition);
                        }
                    }

                    // Store excluded condition results
                    if (excludedConditionResults.Count > 0)
                    {
                        if (!variables.EvaluatedConditions.ContainsKey(scenario.Id))
                        {
                            variables.EvaluatedConditions[scenario.Id] = new Dictionary<string, bool>();
                        }

                        foreach (var kvp in excludedConditionResults)
                        {
                            variables.EvaluatedConditions[scenario.Id][kvp.Key] = kvp.Value;
                        }
                    }

                    // If any excluded condition was triggered, skip to the next scenario
                    if (!isMatch)
                    {
                        _logger.LogDebug("Scenario {ScenarioId} does not match - excluded conditions triggered", scenario.Id);
                        continue;
                    }

                    // If we get here, all required conditions passed and no excluded conditions were triggered
                    _logger.LogInformation("Found matching scenario {ScenarioId}: {ScenarioName}", scenario.Id, scenario.Name);
                    return scenario;
                }

                _logger.LogWarning("No matching scenario found after evaluating all applicable scenarios");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding matching scenario");
                return null;
            }
        }

        public ProcessResult ProcessLeaveRequest(Dictionary<string, object> leaveData)
        {
            if (leaveData == null)
            {
                return new ProcessResult().WithError("LeaveData cannot be null");
            }

            try
            {
                // Convert the dictionary to a DataRow for compatibility with existing code
                var dt = new DataTable();
                foreach (var key in leaveData.Keys)
                {
                    dt.Columns.Add(key);
                }
                
                var dataRow = dt.NewRow();
                foreach (var kvp in leaveData)
                {
                    dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                }

                // Use the existing ProcessLeaveRequest method
                return ProcessLeaveRequest(dataRow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave request with dictionary data");
                return new ProcessResult().WithError($"Error processing leave request: {ex.Message}");
            }
        }

        public ProcessResult ValidateLeaveRequest(Dictionary<string, object> leaveData)
        {
            if (leaveData == null)
            {
                return new ProcessResult().WithError("LeaveData cannot be null");
            }

            try
            {
                // Convert the dictionary to a DataRow for compatibility with existing code
                var dt = new DataTable();
                foreach (var key in leaveData.Keys)
                {
                    dt.Columns.Add(key);
                }
                
                var dataRow = dt.NewRow();
                foreach (var kvp in leaveData)
                {
                    dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                }

                // Use the existing ValidateLeaveRequest method
                return ValidateLeaveRequest(dataRow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating leave request with dictionary data");
                return new ProcessResult().WithError($"Error validating leave request: {ex.Message}");
            }
        }

        public IEnumerable<LeaveScenario> GetApplicableScenarios(DataRow leaveData)
        {
            if (leaveData == null)
            {
                return Enumerable.Empty<LeaveScenario>();
            }

            try
            {
                // Extract reason code and process level from the DataRow
                string reasonCode = leaveData.Table.Columns.Contains("REASON_CODE") ? 
                    leaveData["REASON_CODE"]?.ToString()?.Trim() : string.Empty;
                
                int processLevel = 1;
                if (leaveData.Table.Columns.Contains("PROCESS_LEVEL"))
                {
                    int.TryParse(leaveData["PROCESS_LEVEL"]?.ToString(), out processLevel);
                }
                else if (leaveData.Table.Columns.Contains("GLCOMPANY"))
                {
                    int.TryParse(leaveData["GLCOMPANY"]?.ToString(), out processLevel);
                }

                // Get scenarios by reason code and process level
                return _scenarioConfiguration.GetScenariosForReasonCode(reasonCode, processLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable scenarios from DataRow");
                return Enumerable.Empty<LeaveScenario>();
            }
        }

        public IEnumerable<LeaveScenario> GetApplicableScenarios(Dictionary<string, object> leaveData)
        {
            if (leaveData == null)
            {
                return Enumerable.Empty<LeaveScenario>();
            }

            try
            {
                // Extract reason code and process level from the dictionary
                string reasonCode = leaveData.ContainsKey("REASON_CODE") ? 
                    leaveData["REASON_CODE"]?.ToString()?.Trim() : string.Empty;
                
                int processLevel = 1;
                if (leaveData.ContainsKey("PROCESS_LEVEL"))
                {
                    int.TryParse(leaveData["PROCESS_LEVEL"]?.ToString(), out processLevel);
                }
                else if (leaveData.ContainsKey("GLCOMPANY"))
                {
                    int.TryParse(leaveData["GLCOMPANY"]?.ToString(), out processLevel);
                }

                // Get scenarios by reason code and process level
                return _scenarioConfiguration.GetScenariosForReasonCode(reasonCode, processLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable scenarios with dictionary data");
                return Enumerable.Empty<LeaveScenario>();
            }
        }
    }
} 