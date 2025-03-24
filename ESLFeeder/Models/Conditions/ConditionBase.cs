using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public abstract class ConditionBase : ICondition
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        
        // Abstract method that derived classes will implement
        protected abstract bool EvaluateCore(LeaveVariables variables);
        
        // Default implementation for DataRow-based evaluation
        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            return EvaluateCore(variables);
        }
        
        // Default implementation for Dictionary-based evaluation
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            return EvaluateCore(variables);
        }
    }
} 