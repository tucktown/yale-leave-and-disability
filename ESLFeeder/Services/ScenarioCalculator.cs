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
            // This dictionary-based approach is more type-safe than reflection
            switch (variableName)
            {
                case nameof(variables.ScheduledHours): return variables.ScheduledHours;
                case nameof(variables.PayRate): return variables.PayRate;
                case nameof(variables.WeeklyWage): return variables.WeeklyWage;
                case nameof(variables.CTPLApprovedAmount): return variables.CTPLApprovedAmount;
                case nameof(variables.StdOrNot): return variables.StdOrNot;
                case nameof(variables.CtplPayment): return variables.CtplPayment;
                case nameof(variables.PtoSuppDollars): return variables.PtoSuppDollars;
                case nameof(variables.PtoSuppHrs): return variables.PtoSuppHrs;
                case nameof(variables.PtoReserve): return variables.PtoReserve;
                case nameof(variables.PtoAvailCalc): return variables.PtoAvailCalc;
                case nameof(variables.PtoUsable): return variables.PtoUsable;
                case nameof(variables.PtoUseHrs): return variables.PtoUseHrs;
                case nameof(variables.BasicSickAvailCalc): return variables.BasicSickAvailCalc;
                case nameof(variables.MinWage40): return variables.MinWage40;
                case nameof(variables.NinetyFiveCTMin40): return variables.NinetyFiveCTMin40;
                case nameof(variables.CtplCalcStar): return variables.CtplCalcStar;
                case nameof(variables.CtplCalc): return variables.CtplCalc;
                case nameof(variables.WeekOfPP): return variables.WeekOfPP;
                default:
                    _logger.LogWarning("Variable {VariableName} not found in LeaveVariables", variableName);
                    return 0.0;
            }
        }
    }
} 