using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C7 : ICondition
    {
        public string Name => "C7";
        public string Description => "STD is not approved or has expired";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // Condition is true if STD_APPROVED_THROUGH is null (not approved)
            if (row == null || row["STD_APPROVED_THROUGH"] == DBNull.Value || string.IsNullOrEmpty(row["STD_APPROVED_THROUGH"]?.ToString()))
                return true;

            // Or if the pay start date is after STD_APPROVED_THROUGH (expired)
            var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
            var stdApprovedThrough = Convert.ToDateTime(row["STD_APPROVED_THROUGH"]);

            return payStartDate > stdApprovedThrough;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // Condition is true if STD_APPROVED_THROUGH is null (not approved)
            if (data == null || !data.ContainsKey("STD_APPROVED_THROUGH") || 
                data["STD_APPROVED_THROUGH"] == null || string.IsNullOrEmpty(data["STD_APPROVED_THROUGH"]?.ToString()))
                return true;
            
            // Check for PAY_START_DATE
            if (!data.ContainsKey("PAY_START_DATE") || data["PAY_START_DATE"] == null)
                return false;

            // Or if the pay start date is after STD_APPROVED_THROUGH (expired)
            var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
            var stdApprovedThrough = Convert.ToDateTime(data["STD_APPROVED_THROUGH"]);

            return payStartDate > stdApprovedThrough;
        }
    }
}

