using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C21 : ICondition
    {
        public string Name => "C21";
        public string Description => "Basic Sick balance is greater than or equal to 40% of scheduled hours";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null || variables == null)
                return false;

            // Check if BASIC_SICK_AVAIL_CALC >= SCHED_HRS * 0.4
            return variables.BasicSickAvailCalc >= variables.ScheduledHours * 0.4;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null || variables == null)
                return false;

            // Check if BASIC_SICK_AVAIL_CALC >= SCHED_HRS * 0.4
            return variables.BasicSickAvailCalc >= variables.ScheduledHours * 0.4;
        }
    }
} 