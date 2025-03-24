using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Models;

namespace ESLFeeder.Interfaces
{
    public interface IConditionEvaluator
    {
        bool Evaluate(string conditionId, LeaveVariables variables);
        bool EvaluateCondition(ICondition condition, DataRow row, LeaveVariables variables);
        bool EvaluateCondition(ICondition condition, Dictionary<string, object> data, LeaveVariables variables);
    }
} 