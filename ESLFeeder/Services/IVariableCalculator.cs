using System;
using System.Data;
using System.Collections.Generic;
using ESLFeeder.Models;

namespace ESLFeeder.Services
{
    public interface IVariableCalculator
    {
        bool CalculateVariables(DataRow row, out LeaveVariables variables);
        bool CalculateVariables(Dictionary<string, object> data, out LeaveVariables variables);
        bool ValidateInputVariables(DataRow row, out string errorMessage);
    }
} 