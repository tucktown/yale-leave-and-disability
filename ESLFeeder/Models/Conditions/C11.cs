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
        public string Description => "CT PL not submitted, has expired, or is denied without approved amount";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // First condition: CTPL_FORM IS NULL
            if (row["CTPL_FORM"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString()))
                return true;

            // Second condition: PAY_START_DATE > CTPL_END
            if (row["PAY_START_DATE"] != DBNull.Value && !string.IsNullOrEmpty(row["PAY_START_DATE"]?.ToString()) &&
                row["CTPL_END_DATE"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_END_DATE"]?.ToString()))
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var ctplEndDate = Convert.ToDateTime(row["CTPL_END_DATE"]);
                
                if (payStartDate > ctplEndDate)
                    return true;
            }

            // Third condition: AND(CTPL_FORM = Y, CTPL_DENIED_IND = Y, CTPL_APPROVED_AMOUNT IS NULL)
            bool formIsY = false;
            if (row["CTPL_FORM"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString()))
            {
                formIsY = row["CTPL_FORM"].ToString().ToUpper() == "Y";
            }

            bool deniedIsY = false;
            if (row["CTPL_DENIED_IND"] != DBNull.Value && !string.IsNullOrEmpty(row["CTPL_DENIED_IND"]?.ToString()))
            {
                deniedIsY = row["CTPL_DENIED_IND"].ToString().ToUpper() == "Y";
            }

            bool approvedAmountIsNull = (row["CTPL_APPROVED_AMOUNT"] == DBNull.Value || 
                                        string.IsNullOrEmpty(row["CTPL_APPROVED_AMOUNT"]?.ToString()));

            if (formIsY && deniedIsY && approvedAmountIsNull)
                return true;

            return false;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;

            // First condition: CTPL_FORM IS NULL
            if (!data.ContainsKey("CTPL_FORM") || 
                data["CTPL_FORM"] == null || string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString()))
                return true;

            // Second condition: PAY_START_DATE > CTPL_END
            if (data.ContainsKey("PAY_START_DATE") && data["PAY_START_DATE"] != null &&
                data.ContainsKey("CTPL_END_DATE") && data["CTPL_END_DATE"] != null)
            {
                var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                var ctplEndDate = Convert.ToDateTime(data["CTPL_END_DATE"]);
                
                if (payStartDate > ctplEndDate)
                    return true;
            }

            // Third condition: AND(CTPL_FORM = Y, CTPL_DENIED_IND = Y, CTPL_APPROVED_AMOUNT IS NULL)
            bool formIsY = false;
            if (data.ContainsKey("CTPL_FORM") && data["CTPL_FORM"] != null && 
                !string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString()))
            {
                formIsY = data["CTPL_FORM"].ToString().ToUpper() == "Y";
            }

            bool deniedIsY = false;
            if (data.ContainsKey("CTPL_DENIED_IND") && data["CTPL_DENIED_IND"] != null && 
                !string.IsNullOrEmpty(data["CTPL_DENIED_IND"]?.ToString()))
            {
                deniedIsY = data["CTPL_DENIED_IND"].ToString().ToUpper() == "Y";
            }

            bool approvedAmountIsNull = (!data.ContainsKey("CTPL_APPROVED_AMOUNT") || 
                                        data["CTPL_APPROVED_AMOUNT"] == null || 
                                        string.IsNullOrEmpty(data["CTPL_APPROVED_AMOUNT"]?.ToString()));

            if (formIsY && deniedIsY && approvedAmountIsNull)
                return true;

            return false;
        }
    }
}

