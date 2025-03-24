using System;
using System.Data;
using ESLFeeder.Models;

namespace ESLFeeder.Services
{
    public interface IVariableCalculator
    {
        bool CalculateVariables(DataRow row, out LeaveVariables variables);
        bool ValidateInputVariables(DataRow row, out string errorMessage);
    }
} 