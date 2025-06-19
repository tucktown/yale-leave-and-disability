using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C19 : ICondition
    {
        public C19() { }
        public string Name => "C19";
        public string Description => "Calculates if usable PTO (current PTO balance - return to work reserve) is greater than employee's weekly scheduled hours.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            return EvaluateInternal(row, variables);
        }

        private bool EvaluateInternal(object row, LeaveVariables variables)
        {
            if (row == null)
            {
                System.Diagnostics.Debug.WriteLine("C19: row is null");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"C19: ptoUsable={variables.PtoUsable}, schedHrs={variables.ScheduledHours}, result={variables.PtoUsable >= variables.ScheduledHours}");
            
            // Return true if usable PTO is greater than or equal to scheduled hours
            return variables.PtoUsable >= variables.ScheduledHours;
        }
    }
} 