using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ESLFeeder.Services
{
    public class ConditionEvaluator : IConditionEvaluator
    {
        private readonly ILogger<ConditionEvaluator> _logger;
        private readonly Dictionary<string, ICondition> _conditions;

        public ConditionEvaluator(ILogger<ConditionEvaluator> logger, IEnumerable<ICondition> conditions)
        {
            _logger = logger;
            _conditions = new Dictionary<string, ICondition>();
            
            foreach (var condition in conditions)
            {
                _conditions.Add(condition.Name, condition);
            }
        }
        
        public bool EvaluateCondition(ICondition condition, DataRow row, LeaveVariables variables)
        {
            try
            {
                return condition.Evaluate(row, variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition {ConditionName}", condition.Name);
                return false;
            }
        }
        
        public bool EvaluateCondition(ICondition condition, Dictionary<string, object> data, LeaveVariables variables)
        {
            try
            {
                // Create a DataRow from the dictionary for compatibility with existing conditions
                DataTable dt = new DataTable();
                foreach (var key in data.Keys)
                {
                    dt.Columns.Add(key);
                }
                
                DataRow row = dt.NewRow();
                foreach (var kvp in data)
                {
                    row[kvp.Key] = kvp.Value ?? DBNull.Value;
                }
                
                return condition.Evaluate(row, variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition {ConditionName} with dictionary data", condition.Name);
                return false;
            }
        }

        public bool Evaluate(string conditionId, LeaveVariables variables)
        {
            try
            {
                // Get the condition from the registry
                var condition = _conditions.Values.FirstOrDefault(c => c.Name == conditionId);
                if (condition == null)
                {
                    _logger.LogWarning("Condition {ConditionId} not found", conditionId);
                    return false;
                }

                // Evaluate the condition with null data row (our conditions should handle this)
                return EvaluateCondition(condition, (DataRow)null, variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition {ConditionId}", conditionId);
                return false;
            }
        }
    }
} 