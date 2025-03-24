using System.Collections.Generic;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Represents a set of conditions for a leave scenario
    /// </summary>
    public class ConditionSet
    {
        /// <summary>
        /// Conditions that must be met for the scenario to apply
        /// </summary>
        public List<string> RequiredConditions { get; set; } = new List<string>();
        
        /// <summary>
        /// Conditions that must NOT be met for the scenario to apply
        /// </summary>
        public List<string> ExcludedConditions { get; set; } = new List<string>();
        
        /// <summary>
        /// Conditions that may optionally be considered
        /// </summary>
        public List<string> OptionalConditions { get; set; } = new List<string>();
        
        /// <summary>
        /// Check if a condition ID is required
        /// </summary>
        public bool IsRequired(string conditionId)
        {
            return RequiredConditions.Contains(conditionId);
        }
        
        /// <summary>
        /// Check if a condition ID is excluded
        /// </summary>
        public bool IsExcluded(string conditionId)
        {
            return ExcludedConditions.Contains(conditionId);
        }
        
        /// <summary>
        /// Validates that all condition IDs match a valid pattern (e.g., "C1", "C12")
        /// </summary>
        public bool Validate()
        {
            // Validate all conditions have proper format (Cxx)
            foreach (var condition in RequiredConditions)
            {
                if (!IsValidConditionId(condition))
                    return false;
            }
            
            foreach (var condition in ExcludedConditions)
            {
                if (!IsValidConditionId(condition))
                    return false;
            }
            
            foreach (var condition in OptionalConditions)
            {
                if (!IsValidConditionId(condition))
                    return false;
            }
            
            return true;
        }
        
        private bool IsValidConditionId(string conditionId)
        {
            // Simple validation: must start with "C" followed by numbers
            return !string.IsNullOrEmpty(conditionId) && 
                   conditionId.StartsWith("C") && 
                   conditionId.Length > 1 &&
                   int.TryParse(conditionId.Substring(1), out _);
        }
    }
} 