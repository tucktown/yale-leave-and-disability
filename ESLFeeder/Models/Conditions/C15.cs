using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C15 : ICondition
    {
        public C15() { }
        public string Name => "C15";
        public string Description => "Calculates employee's available PTO vs. how much they want to keep for Return to Word. If PTO_USABLE is greater than 0, PTO can be applied to supplement leave.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            return variables.PtoUsable > 0;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            return Evaluate((DataRow)null, variables);
        }
    }
}

