using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C8 : ICondition
    {
        public C8() { }
        public string Name => "C8";
        public string Description => "Determines if STD hours can be applied according to actual or estimated CT PL payments. If > 0, then STD can be applied.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // Ensure we have valid PayRate
            if (variables.PayRate <= 0)
                return false;

            // Calculate STD hours that can be applied
            double stdHours = variables.StdOrNot / variables.PayRate;

            return stdHours > 0;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // Use the same logic as DataRow version
            return Evaluate((DataRow)null, variables);
        }
    }
} 