using ESLFeeder.Models;

namespace ESLFeeder.Services
{
    /// <summary>
    /// Interface for services that calculate scenario output values
    /// </summary>
    public interface IScenarioCalculator
    {
        /// <summary>
        /// Calculates the value of an operand using the provided leave variables
        /// </summary>
        double GetOperandValue(OperandConfig operand, LeaveVariables variables);

        /// <summary>
        /// Calculates the result of a calculation configuration
        /// </summary>
        double CalculateValue(CalculationConfig calculation, LeaveVariables variables);
    }
} 