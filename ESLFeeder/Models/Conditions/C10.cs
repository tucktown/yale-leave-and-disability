using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C10 : ICondition
    {
        public string Name => "C10";
        public string Description => "CT PL is active in current week (End)";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // If CTPL_END is null, CT PL is not active
            if (row == null || row["CTPL_END"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_END"]?.ToString()))
                return true;

            var payEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);
            var ctplEndDate = Convert.ToDateTime(row["CTPL_END"]);

            // Return true if pay end date is before or equal to CTPL end date, or if CTPL end is null
            return payEndDate <= ctplEndDate;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // If CTPL_END is null, CT PL is not active
            if (data == null || !data.ContainsKey("CTPL_END") || 
                data["CTPL_END"] == null || string.IsNullOrEmpty(data["CTPL_END"]?.ToString()))
                return true;

            // Check for PAY_END_DATE
            if (!data.ContainsKey("PAY_END_DATE") || data["PAY_END_DATE"] == null)
                return false;

            var payEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);
            var ctplEndDate = Convert.ToDateTime(data["CTPL_END"]);

            // Return true if pay end date is before or equal to CTPL end date
            return payEndDate <= ctplEndDate;
        }
    }
}

