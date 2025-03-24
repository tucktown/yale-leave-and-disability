using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C9 : ICondition
    {
        public string Name => "C9";
        public string Description => "CT PL is active in current week (Start)";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // If CTPL_START is null, CT PL is not active
            if (row == null || row["CTPL_START_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_START_DATE"]?.ToString()))
                return true;

            // Check if pay start date is on or after CTPL start date
            var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
            var ctplStartDate = Convert.ToDateTime(row["CTPL_START_DATE"]);

            // Return true if pay start date is on or after CTPL start date, or if CTPL start is null
            return payStartDate >= ctplStartDate;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // If CTPL_START is null, CT PL is not active
            if (data == null || !data.ContainsKey("CTPL_START_DATE") || 
                data["CTPL_START_DATE"] == null || string.IsNullOrEmpty(data["CTPL_START_DATE"]?.ToString()))
                return true;

            // Check for PAY_START_DATE
            if (!data.ContainsKey("PAY_START_DATE") || data["PAY_START_DATE"] == null)
                return false;

            // Check if pay start date is on or after CTPL start date
            var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
            var ctplStartDate = Convert.ToDateTime(data["CTPL_START_DATE"]);

            // Return true if pay start date is on or after CTPL start date
            return payStartDate >= ctplStartDate;
        }
    }
}

