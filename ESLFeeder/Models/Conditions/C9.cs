using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C9 : ICondition
    {
        public C9() { }
        public string Name => "C9";
        public string Description => "CT PL is active in current week (Start) and not denied";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // First part: PAY_START_DATE >= CTPL_START
            bool dateCondition = false;
            if (row["CTPL_START_DATE"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_START_DATE"]?.ToString()) &&
                row["PAY_START_DATE"] != DBNull.Value && !string.IsNullOrEmpty(row["PAY_START_DATE"]?.ToString()))
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var ctplStartDate = Convert.ToDateTime(row["CTPL_START_DATE"]);
                dateCondition = payStartDate >= ctplStartDate;
            }

            // Second part: AND(CTPL_START IS NULL, CTPL_FORM = Y, CTPL_DENIED_IND <> Y)
            bool nullStartCondition = false;
            if (row["CTPL_START_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_START_DATE"]?.ToString()))
            {
                // Check if CTPL_FORM = Y
                bool formIsY = false;
                if (row["CTPL_FORM"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString()))
                {
                    formIsY = row["CTPL_FORM"].ToString().ToUpper() == "Y";
                }

                // Check if CTPL_DENIED_IND <> Y
                bool notDenied = true;
                if (row["CTPL_DENIED_IND"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_DENIED_IND"]?.ToString()))
                {
                    notDenied = row["CTPL_DENIED_IND"].ToString().ToUpper() != "Y";
                }

                nullStartCondition = formIsY && notDenied;
            }

            // Return TRUE if either condition is met
            return dateCondition || nullStartCondition;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;

            // First part: PAY_START_DATE >= CTPL_START
            bool dateCondition = false;
            if (data.ContainsKey("CTPL_START_DATE") && data["CTPL_START_DATE"] != null && 
                !string.IsNullOrEmpty(data["CTPL_START_DATE"]?.ToString()) &&
                data.ContainsKey("PAY_START_DATE") && data["PAY_START_DATE"] != null)
            {
                var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                var ctplStartDate = Convert.ToDateTime(data["CTPL_START_DATE"]);
                dateCondition = payStartDate >= ctplStartDate;
            }

            // Second part: AND(CTPL_START IS NULL, CTPL_FORM = Y, CTPL_DENIED_IND <> Y)
            bool nullStartCondition = false;
            if (!data.ContainsKey("CTPL_START_DATE") || 
                data["CTPL_START_DATE"] == null || string.IsNullOrEmpty(data["CTPL_START_DATE"]?.ToString()))
            {
                // Check if CTPL_FORM = Y
                bool formIsY = false;
                if (data.ContainsKey("CTPL_FORM") && data["CTPL_FORM"] != null && 
                    !string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString()))
                {
                    formIsY = data["CTPL_FORM"].ToString().ToUpper() == "Y";
                }

                // Check if CTPL_DENIED_IND <> Y
                bool notDenied = true;
                if (data.ContainsKey("CTPL_DENIED_IND") && data["CTPL_DENIED_IND"] != null && 
                    !string.IsNullOrEmpty(data["CTPL_DENIED_IND"]?.ToString()))
                {
                    notDenied = data["CTPL_DENIED_IND"].ToString().ToUpper() != "Y";
                }

                nullStartCondition = formIsY && notDenied;
            }

            // Return TRUE if either condition is met
            return dateCondition || nullStartCondition;
        }
    }
}

