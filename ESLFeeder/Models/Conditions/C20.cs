using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C20 : ICondition
    {
        public C20() { }
        public string Name => "C20";
        public string Description => "Employee has a Basic Sick balance (calculated)";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null || variables == null)
                return false;

            // Check if BASIC_SICK_AVAIL_CALC > 0
            return variables.BasicSickAvailCalc > 0;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null || variables == null)
                return false;

            // Check if BASIC_SICK_AVAIL_CALC > 0
            return variables.BasicSickAvailCalc > 0;
        }
    }
} 