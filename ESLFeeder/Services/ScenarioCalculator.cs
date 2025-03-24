using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ESLFeeder.Models;

namespace ESLFeeder.Services
{
    /// <summary>
    /// Service for calculating scenario output values
    /// </summary>
    public class ScenarioCalculator : IScenarioCalculator
    {
        private readonly ILogger<ScenarioCalculator> _logger;

        public ScenarioCalculator(ILogger<ScenarioCalculator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Calculates the value of an operand using the provided leave variables
        /// </summary>
        public double GetOperandValue(OperandConfig operand, LeaveVariables variables)
        {
            if (operand == null)
            {
                _logger.LogWarning("Null operand provided");
                return 0.0;
            }

            if (operand.Constant.HasValue)
            {
                return operand.Constant.Value;
            }

            if (!string.IsNullOrEmpty(operand.Variable))
            {
                // Use a type-safe dictionary approach instead of reflection
                return GetVariableValue(operand.Variable, variables);
            }

            _logger.LogWarning("Operand has neither variable nor constant value");
            return 0.0;
        }

        /// <summary>
        /// Calculates the result of a calculation configuration
        /// </summary>
        public double CalculateValue(CalculationConfig calculation, LeaveVariables variables)
        {
            if (calculation == null)
            {
                _logger.LogWarning("Null calculation provided");
                return 0.0;
            }

            try
            {
                switch (calculation.Operation.ToLower())
                {
                    case CalculationConfig.Operations.Direct:
                        if (calculation.Operands.Length < 1)
                        {
                            _logger.LogWarning("Direct operation requires at least one operand");
                            return 0.0;
                        }
                        return GetOperandValue(calculation.Operands[0], variables);

                    case CalculationConfig.Operations.Multiply:
                        if (calculation.Operands.Length < 1)
                        {
                            _logger.LogWarning("Multiply operation requires at least one operand");
                            return 0.0;
                        }
                        var product = 1.0;
                        foreach (var operand in calculation.Operands)
                        {
                            product *= GetOperandValue(operand, variables);
                        }
                        return product;

                    case CalculationConfig.Operations.Divide:
                        if (calculation.Operands.Length != 2)
                        {
                            _logger.LogWarning("Division requires exactly two operands");
                            return 0.0;
                        }
                        var dividend = GetOperandValue(calculation.Operands[0], variables);
                        var divisor = GetOperandValue(calculation.Operands[1], variables);
                        
                        if (Math.Abs(divisor) < 0.0001)
                        {
                            _logger.LogWarning("Division by zero attempted");
                            return 0.0;
                        }
                        
                        return dividend / divisor;

                    case CalculationConfig.Operations.Add:
                        if (calculation.Operands.Length < 1)
                        {
                            _logger.LogWarning("Add operation requires at least one operand");
                            return 0.0;
                        }
                        return calculation.Operands.Sum(op => GetOperandValue(op, variables));

                    case CalculationConfig.Operations.Subtract:
                        if (calculation.Operands.Length != 2)
                        {
                            _logger.LogWarning("Subtraction requires exactly two operands");
                            return 0.0;
                        }
                        return GetOperandValue(calculation.Operands[0], variables) - 
                               GetOperandValue(calculation.Operands[1], variables);

                    default:
                        _logger.LogError("Unsupported operation: {Operation}", calculation.Operation);
                        return 0.0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing calculation with operation {Operation}", calculation.Operation);
                return 0.0;
            }
        }

        /// <summary>
        /// Gets a variable value from the LeaveVariables object in a type-safe manner
        /// </summary>
        private double GetVariableValue(string variableName, LeaveVariables variables)
        {
            if (variables == null || string.IsNullOrEmpty(variableName))
            {
                return 0.0;
            }

            // Add mappings for variable names to their property getters
            switch (variableName.ToLower())
            {
                case "basicpay":
                    return variables.BasicPay;
                case "pto_available":
                case "ptoavail":
                    return variables.PtoAvail;
                case "pto_last1week":
                case "ptolast1week":
                    return variables.PtoHrsLast1Week;
                case "pto_last2week":
                case "ptolast2week":
                    return variables.PtoHrsLast2Week;
                case "basicsick_available":
                    return variables.BasicSickAvail;
                case "basicsick_last1week":
                    return variables.BasicSickLast1Week;
                case "basicsick_last2week":
                    return variables.BasicSickLast2Week;
                case "hours_per_week":
                    return variables.HoursPerWeek;
                case "employee_status":
                    // Convert status string to numeric value (e.g., Active=1, LOA=2, etc.)
                    return ConvertStatusToNumeric(variables.EmployeeStatus);
                default:
                    _logger.LogWarning("Unknown variable name: {VariableName}", variableName);
                    return 0.0;
            }
        }

        public ProcessResult Calculate(LeaveScenario scenario, LeaveVariables variables)
        {
            if (scenario == null)
            {
                _logger.LogWarning("Null scenario provided for calculation");
                return new ProcessResult { Success = false, ErrorMessage = "Invalid scenario" };
            }

            if (variables == null)
            {
                _logger.LogWarning("Null variables provided for calculation");
                return new ProcessResult { Success = false, ErrorMessage = "Invalid variables" };
            }

            var result = new ProcessResult { Success = true };
            
            try
            {
                _logger.LogInformation("Calculating scenario {ScenarioId}: {ScenarioName}", scenario.Id, scenario.Name);
                
                // Set scenario information on the result
                result.ScenarioId = scenario.Id;
                result.ScenarioName = scenario.Name;
                
                // Process each output field in the scenario's Updates
                foreach (var outputEntry in scenario.Outputs)
                {
                    string outputId = outputEntry.Key;
                    ScenarioOutput output = outputEntry.Value;
                    
                    // If this is a calculated output
                    if (output.Type == ScenarioOutput.OutputType.Number && output is ScenarioOutput numberOutput)
                    {
                        double outputValue = numberOutput.NumberValue;
                        _logger.LogInformation("Using output {OutputId}: {OutputValue}", outputId, outputValue);
                        
                        // Add the output to the result
                        result.AddOutputValue(outputId, outputValue);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating scenario {ScenarioId}", scenario.Id);
                return new ProcessResult { Success = false, ErrorMessage = $"Error calculating scenario: {ex.Message}" };
            }
        }

        private double ConvertStatusToNumeric(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return 0.0;
            }

            // Standard status values used in the system
            switch (status.ToLowerInvariant().Trim())
            {
                case "active":
                    return 1.0;
                case "loa":
                    return 2.0;
                case "terminated":
                    return 3.0;
                default:
                    _logger.LogWarning("Unknown employee status: {Status}", status);
                    return 0.0;
            }
        }
    }
} 