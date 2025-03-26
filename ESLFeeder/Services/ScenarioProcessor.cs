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
                result.ScenarioDescription = scenario.Description;
                
                // Log the scenario details for debugging
                _logger.LogDebug("Setting scenario details - ID: {Id}, Name: {Name}, Description: {Description}", 
                    result.ScenarioId, result.ScenarioName, result.ScenarioDescription);
                
                // Get conditions from the ConditionSet
                if (scenario.Conditions != null)
                {
                    result.RequiredConditions = scenario.Conditions.RequiredConditions.ToList();
                    result.ForbiddenConditions = scenario.Conditions.ExcludedConditions.ToList();
                    
                    // Log conditions for debugging
                    _logger.LogDebug("Setting conditions - Required: {Required}, Forbidden: {Forbidden}", 
                        string.Join(", ", result.RequiredConditions),
                        string.Join(", ", result.ForbiddenConditions));
                }
                else
                {
                    _logger.LogWarning("No conditions found for scenario {ScenarioId}", scenario.Id);
                }
                
                // Log final ProcessResult state
                _logger.LogDebug("Final ProcessResult state - Success: {Success}, ScenarioId: {Id}, Name: {Name}, Description: {Description}, RequiredCount: {RequiredCount}, ForbiddenCount: {ForbiddenCount}",
                    result.Success,
                    result.ScenarioId,
                    result.ScenarioName,
                    result.ScenarioDescription,
                    result.RequiredConditions?.Count ?? 0,
                    result.ForbiddenConditions?.Count ?? 0);
                
                // Add evaluated conditions to the result
                if (variables != null && variables.EvaluatedConditions != null && variables.EvaluatedConditions.ContainsKey(scenario.Id))
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

        public ProcessResult ProcessLeaveRequest(Dictionary<string, object> leaveData)
        {
            _logger.LogInformation("Processing leave request from dictionary");

            var result = new ProcessResult();

            try
            {
                if (leaveData == null)
                {
                    return result.WithError("Leave data dictionary cannot be null");
                }

                // Validate required fields
                var requiredFields = new[] { "CLAIM_ID", "PAY_START_DATE", "PAY_END_DATE", "REASON_CODE" };
                foreach (var field in requiredFields)
                {
                    if (!leaveData.ContainsKey(field) || string.IsNullOrEmpty(leaveData[field]?.ToString()))
                    {
                        return result.WithError($"Required field {field} is missing or empty");
                    }
                }

                // Calculate variables
                if (!_calculator.CalculateVariables(leaveData, out var variables))
                {
                    return result.WithError("Failed to calculate required variables");
                }

                // Set scenario identification properties
                variables.ReasonCode = leaveData["REASON_CODE"].ToString().Trim();
                
                // Try to get the process level or GL company
                if (leaveData.ContainsKey("PROCESS_LEVEL") && !string.IsNullOrEmpty(leaveData["PROCESS_LEVEL"]?.ToString()))
                {
                    variables.GlCompany = int.Parse(leaveData["PROCESS_LEVEL"].ToString().Trim());
                }
                else if (leaveData.ContainsKey("GLCOMPANY") && !string.IsNullOrEmpty(leaveData["GLCOMPANY"]?.ToString()))
                {
                    variables.GlCompany = int.Parse(leaveData["GLCOMPANY"].ToString().Trim());
                }
                else
                {
                    variables.GlCompany = 1; // Default value if not found
                }

                return ProcessLeaveRequest(variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave request from dictionary");
                return result.WithError(ex);
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

                // Validate reason code
                var reasonCode = leaveData["REASON_CODE"].ToString().Trim();
                var validReasonCodes = _scenarioConfiguration.GetValidReasonCodes();
                if (!validReasonCodes.Contains(reasonCode, StringComparer.OrdinalIgnoreCase))
                {
                    return result.WithError($"Invalid reason code: {reasonCode}. Valid codes: {string.Join(", ", validReasonCodes)}");
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

        public ProcessResult ValidateLeaveRequest(Dictionary<string, object> leaveData)
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
                    if (!leaveData.ContainsKey(field) || string.IsNullOrEmpty(leaveData[field]?.ToString()))
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

                // Validate reason code
                var reasonCode = leaveData["REASON_CODE"].ToString().Trim();
                var validReasonCodes = _scenarioConfiguration.GetValidReasonCodes();
                if (!validReasonCodes.Contains(reasonCode, StringComparer.OrdinalIgnoreCase))
                {
                    return result.WithError($"Invalid reason code: {reasonCode}. Valid codes: {string.Join(", ", validReasonCodes)}");
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating leave request from dictionary");
                return result.WithError($"Error validating leave request: {ex.Message}");
            }
        }

        public IEnumerable<LeaveScenario> GetApplicableScenarios(DataRow row)
        {
            if (row == null)
            {
                _logger.LogWarning("Row data cannot be null");
                return Enumerable.Empty<LeaveScenario>();
            }

            try
            {
                string reasonCode = row["REASON_CODE"].ToString().Trim();
                int processLevelInt = 0;

                if (row.Table.Columns.Contains("PROCESS_LEVEL"))
                {
                    processLevelInt = int.Parse(row["PROCESS_LEVEL"].ToString().Trim());
                }
                else if (row.Table.Columns.Contains("GLCOMPANY"))
                {
                    processLevelInt = int.Parse(row["GLCOMPANY"].ToString().Trim());
                }
                
                if (processLevelInt == 0)
                {
                    _logger.LogWarning("Process level or GL Company not found in row data");
                    return Enumerable.Empty<LeaveScenario>();
                }
                
                // Get all scenarios from the configuration
                var allScenarios = _scenarioConfiguration.GetScenarios();
                
                // Filter to relevant scenarios
                var scenarios = allScenarios
                    .Where(s => s.ReasonCode.Trim().Equals(reasonCode, StringComparison.OrdinalIgnoreCase) && 
                                s.SupportsProcessLevel(processLevelInt) &&
                                s.IsActive)
                    .OrderBy(s => s.Id)
                    .ToList();
                
                _logger.LogInformation("Found {Count} scenarios for reason code {ReasonCode} and process level {ProcessLevel}",
                    scenarios.Count, reasonCode, processLevelInt);
                
                return scenarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applicable scenarios");
                return Enumerable.Empty<LeaveScenario>();
            }
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
                        if (variables != null && variables.EvaluatedConditions != null && !variables.EvaluatedConditions.ContainsKey(scenario.Id))
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
                        if (variables != null && variables.EvaluatedConditions != null && !variables.EvaluatedConditions.ContainsKey(scenario.Id))
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

        public IEnumerable<LeaveScenario> GetApplicableScenarios(Dictionary<string, object> data)
        {
            if (data == null)
            {
                _logger.LogWarning("Dictionary data cannot be null");
                return Enumerable.Empty<LeaveScenario>();
            }

            try
            {
                if (!data.ContainsKey("REASON_CODE") || string.IsNullOrEmpty(data["REASON_CODE"]?.ToString()))
                {
                    _logger.LogWarning("Reason code not found in dictionary data");
                    return Enumerable.Empty<LeaveScenario>();
                }

                string reasonCode = data["REASON_CODE"].ToString().Trim();
                int processLevelInt = 0;

                if (data.ContainsKey("PROCESS_LEVEL") && !string.IsNullOrEmpty(data["PROCESS_LEVEL"]?.ToString()))
                {
                    processLevelInt = int.Parse(data["PROCESS_LEVEL"].ToString().Trim());
                }
                else if (data.ContainsKey("GLCOMPANY") && !string.IsNullOrEmpty(data["GLCOMPANY"]?.ToString()))
                {
                    processLevelInt = int.Parse(data["GLCOMPANY"].ToString().Trim());
                }
                
                if (processLevelInt == 0)
                {
                    _logger.LogWarning("Process level or GL Company not found in dictionary data");
                    return Enumerable.Empty<LeaveScenario>();
                }
                
                // Get all scenarios from the configuration
                var allScenarios = _scenarioConfiguration.GetScenarios();
                
                // Filter to relevant scenarios
                var scenarios = allScenarios
                    .Where(s => s.ReasonCode.Trim().Equals(reasonCode, StringComparison.OrdinalIgnoreCase) && 
                                s.SupportsProcessLevel(processLevelInt) &&
                                s.IsActive)
                    .OrderBy(s => s.Id)
                    .ToList();
                
                _logger.LogInformation("Found {Count} scenarios for reason code {ReasonCode} and process level {ProcessLevel}",
                    scenarios.Count, reasonCode, processLevelInt);
                
                return scenarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applicable scenarios");
                return Enumerable.Empty<LeaveScenario>();
            }
        }
    }
} 