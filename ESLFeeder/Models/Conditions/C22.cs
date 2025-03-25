using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C22 : ICondition
    {
        public string Name => "C22";
        public string Description => "Basic Sick balance is greater than or equal PTO Supplement Hours";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null || variables == null)
                return false;

            // Check if BASIC_SICK_AVAIL_CALC >= PTO_SUPP_HRS
            return variables.BasicSickAvailCalc >= variables.PtoSuppHrs;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null || variables == null)
                return false;

            // Check if BASIC_SICK_AVAIL_CALC >= PTO_SUPP_HRS
            return variables.BasicSickAvailCalc >= variables.PtoSuppHrs;
        }
    }
} 