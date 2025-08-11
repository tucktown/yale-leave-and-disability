using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C10 : ICondition
    {
        public C10() { }
        public string Name => "C10";
        public string Description => "CT PL is active in current week (End) and not denied";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // First part: PAY_END_DATE <= CTPL_END
            bool dateCondition = false;
            if (row["CTPL_END_DATE"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_END_DATE"]?.ToString()) &&
                row["PAY_END_DATE"] != DBNull.Value && !string.IsNullOrEmpty(row["PAY_END_DATE"]?.ToString()))
            {
                var payEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);
                var ctplEndDate = Convert.ToDateTime(row["CTPL_END_DATE"]);
                dateCondition = payEndDate <= ctplEndDate;
            }

            // Second part: AND(CTPL_END IS NULL, CTPL_FORM = Y, CTPL_DENIED_IND <> Y)
            bool nullEndCondition = false;
            if (row["CTPL_END_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_END_DATE"]?.ToString()))
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

                nullEndCondition = formIsY && notDenied;
            }

            // Return TRUE if either condition is met
            return dateCondition || nullEndCondition;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;

            // First part: PAY_END_DATE <= CTPL_END
            bool dateCondition = false;
            if (data.ContainsKey("CTPL_END_DATE") && data["CTPL_END_DATE"] != null && 
                !string.IsNullOrEmpty(data["CTPL_END_DATE"]?.ToString()) &&
                data.ContainsKey("PAY_END_DATE") && data["PAY_END_DATE"] != null)
            {
                var payEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);
                var ctplEndDate = Convert.ToDateTime(data["CTPL_END_DATE"]);
                dateCondition = payEndDate <= ctplEndDate;
            }

            // Second part: AND(CTPL_END IS NULL, CTPL_FORM = Y, CTPL_DENIED_IND <> Y)
            bool nullEndCondition = false;
            if (!data.ContainsKey("CTPL_END_DATE") || 
                data["CTPL_END_DATE"] == null || string.IsNullOrEmpty(data["CTPL_END_DATE"]?.ToString()))
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

                nullEndCondition = formIsY && notDenied;
            }

            // Return TRUE if either condition is met
            return dateCondition || nullEndCondition;
        }
    }
}

