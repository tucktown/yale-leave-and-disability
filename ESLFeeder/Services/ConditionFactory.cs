using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using ESLFeeder.Models.Conditions;
using Microsoft.Extensions.Logging;

namespace ESLFeeder.Services
{
    public class ConditionFactory : IConditionRegistry
    {
        private readonly Dictionary<string, ICondition> _conditions;
        private readonly ILogger<ConditionFactory> _logger;

        public ConditionFactory(IEnumerable<ICondition> conditions, ILogger<ConditionFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _conditions = conditions.ToDictionary(c => c.Name, c => c);
            _logger.LogInformation("Loaded {Count} conditions", _conditions.Count);
        }

        public ICondition GetCondition(string id)
        {
            if (!_conditions.ContainsKey(id))
                throw new ArgumentException($"Condition {id} not found");

            return _conditions[id];
        }

        public bool HasCondition(string name)
        {
            return _conditions.ContainsKey(name);
        }

        public IEnumerable<string> GetAvailableConditions()
        {
            return _conditions.Keys;
        }

        public bool EvaluateConditions(string[] conditionIds, DataRow row, LeaveVariables variables)
        {
            if (conditionIds == null || !conditionIds.Any())
                return true;

            return conditionIds.All(id => 
            {
                if (!_conditions.ContainsKey(id))
                    return false;
                return _conditions[id].Evaluate(row, variables);
            });
        }

        public void RegisterCondition(string id, ICondition condition)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Condition ID cannot be null or empty", nameof(id));
            
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            _conditions[id] = condition;
        }
    }
} 