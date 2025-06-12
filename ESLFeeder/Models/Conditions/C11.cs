using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C11 : ICondition
    {
        public C11() { }
        public string Name => "C11";
        public string Description => "CT PL not submitted, has expired, or in not active yet";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // Check if CTPL_FORM is null
            if (row == null || row["CTPL_FORM"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString()))
                return true;

            // Check if required dates exist
            if (row["CTPL_START_DATE"] == DBNull.Value || row["CTPL_END_DATE"] == DBNull.Value ||
                row["PAY_START_DATE"] == DBNull.Value || row["PAY_END_DATE"] == DBNull.Value)
                return false;

            var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
            var payEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);
            var ctplStartDate = Convert.ToDateTime(row["CTPL_START_DATE"]);
            var ctplEndDate = Convert.ToDateTime(row["CTPL_END_DATE"]);

            // Return true if any of the conditions are met
            return payStartDate > ctplEndDate || payEndDate < ctplStartDate;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // Check if CTPL_FORM is null
            if (data == null || !data.ContainsKey("CTPL_FORM") || 
                data["CTPL_FORM"] == null || string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString()))
                return true;

            // Check if all required dates exist
            if (!data.ContainsKey("CTPL_START_DATE") || !data.ContainsKey("CTPL_END_DATE") ||
                !data.ContainsKey("PAY_START_DATE") || !data.ContainsKey("PAY_END_DATE") ||
                data["CTPL_START_DATE"] == null || data["CTPL_END_DATE"] == null ||
                data["PAY_START_DATE"] == null || data["PAY_END_DATE"] == null)
                return false;

            var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
            var payEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);
            var ctplStartDate = Convert.ToDateTime(data["CTPL_START_DATE"]);
            var ctplEndDate = Convert.ToDateTime(data["CTPL_END_DATE"]);

            // Return true if any of the conditions are met
            return payStartDate > ctplEndDate || payEndDate < ctplStartDate;
        }
    }
}

