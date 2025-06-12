using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C6 : ICondition
    {
        public C6() { }
        public string Name => "C6";
        public string Description => "STD is active";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // If STD_APPROVED_THROUGH is empty, STD is not active
            if (row == null || string.IsNullOrEmpty(row["STD_APPROVED_THROUGH"]?.ToString()))
                return false;

            var payEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);
            var stdApprovedThrough = Convert.ToDateTime(row["STD_APPROVED_THROUGH"]);

            // Return true if pay end date is before or equal to STD approved through date
            return payEndDate <= stdApprovedThrough;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null || !data.ContainsKey("STD_APPROVED_THROUGH") || string.IsNullOrEmpty(data["STD_APPROVED_THROUGH"]?.ToString()))
                return false;

            if (!data.ContainsKey("PAY_END_DATE") || data["PAY_END_DATE"] == null)
                return false;

            var payEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);
            var stdApprovedThrough = Convert.ToDateTime(data["STD_APPROVED_THROUGH"]);

            // Return true if pay end date is before or equal to STD approved through date
            return payEndDate <= stdApprovedThrough;
        }
    }
}

