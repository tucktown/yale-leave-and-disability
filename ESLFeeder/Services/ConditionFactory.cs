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
                { "C6", new C6() },    // STD is active
                { "C7", new C7() },    // STD is not approved or has expired
                { "C8", new C8() },    // Determines if STD hours can be applied
                { "C9", new C9() },    // CT PL is active in current week (Start)
                { "C10", new C10() },  // CT PL is active in current week (End)
                { "C11", new C11() },  // CT PL not submitted or has expired
                { "C12", new C12() },  // Employee indicated they would like to supplement leave with PTO
                { "C13", new C13() },  // 40% of PTO hours are less than or equal to usable PTO balance
                { "C14", new C14() },  // PTO hours that are usable in combination with CT PL
                { "C15", new C15() },  // Calculates employee's available PTO vs. how much they want to keep for Return to Work
                { "C16", new C16() },  // FMLA is approved and active
                { "C17", new C17() },  // FMLA and CT PL are inactive or expired
                { "C18", new C18() },  // STD, CT PL, and FMLA are not approved
                { "C19", new C19() },  // Calculates if usable PTO is greater than employee's weekly scheduled hours
                { "C20", new C20() },  // Employee has a Basic Sick balance (calculated)
                { "C21", new C21() },  // Basic Sick balance is greater than or equal to 40% of scheduled hours
                { "C22", new C22() }   // Basic Sick balance is greater than or equal PTO Supplement Hours
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