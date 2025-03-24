using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C11 : ICondition
    {
        public string Name => "C11";
        public string Description => "CT PL not submitted or has expired";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // If CTPL_FORM is null (not submitted)
            if (row == null || row["CTPL_FORM"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString()))
                return true;

            // Or if the pay start date is after CT PL end date (expired)
            if (row["CTPL_END"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_END"]?.ToString()))
                return false;

            var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
            var ctplEndDate = Convert.ToDateTime(row["CTPL_END"]);

            return payStartDate > ctplEndDate;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // If CTPL_FORM is null (not submitted)
            if (data == null || !data.ContainsKey("CTPL_FORM") || 
                data["CTPL_FORM"] == null || string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString()))
                return true;

            // Check if CTPL_END exists
            if (!data.ContainsKey("CTPL_END") || data["CTPL_END"] == null || string.IsNullOrEmpty(data["CTPL_END"]?.ToString()))
                return false;

            // Check if PAY_START_DATE exists
            if (!data.ContainsKey("PAY_START_DATE") || data["PAY_START_DATE"] == null)
                return false;

            // Or if the pay start date is after CT PL end date (expired)
            var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
            var ctplEndDate = Convert.ToDateTime(data["CTPL_END"]);

            return payStartDate > ctplEndDate;
        }
    }
}

