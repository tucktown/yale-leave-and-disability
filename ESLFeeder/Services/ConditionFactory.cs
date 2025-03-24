using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using ESLFeeder.Models.Conditions;

namespace ESLFeeder.Services
{
    public class ConditionFactory : IConditionRegistry
    {
        private readonly Dictionary<string, ICondition> _conditions;

        public ConditionFactory()
        {
            _conditions = new Dictionary<string, ICondition>
            {
                { "C6", new C6() },
                { "C7", new C7() },
                { "C8", new C8() },
                { "C9", new C9() },
                { "C10", new C10() },
                { "C11", new C11() },
                { "C12", new C12() },
                { "C13", new C13() },
                { "C14", new C14() },
                { "C15", new C15() },
                { "C16", new C16() },
                { "C17", new C17() },
                { "C18", new C18() },
                { "C19", new C19() },
                { "C20", new C20() },
                { "C21", new C21() }
            };
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