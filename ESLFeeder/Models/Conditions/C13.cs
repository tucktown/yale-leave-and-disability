using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C13 : ICondition
    {
        public string Name => "C13";
        public string Description => "40% of PTO hours are less than or equal to usable PTO balance. PTO can be used to supplement leave.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // Calculate 40% of scheduled hours
            double fortyPercentOfScheduledHours = variables.ScheduledHours * 0.4;

            // Compare with PTO_USABLE
            return fortyPercentOfScheduledHours <= variables.PtoUsable;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            return Evaluate((DataRow)null, variables);
        }
    }
}

