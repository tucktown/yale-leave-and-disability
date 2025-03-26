using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C17 : ICondition
    {
        public string Name => "C17";
        public string Description => "FMLA and CT PL are inactive or expired";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;
                
            // Check FMLA condition: FMLA_APPR_DATE IS NULL OR PAY_START_DATE > FMLA_APPR_DATE
            bool fmlaInactive = row["FMLA_APPR_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["FMLA_APPR_DATE"]?.ToString());
            if (!fmlaInactive)
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var fmlaApprDate = Convert.ToDateTime(row["FMLA_APPR_DATE"]);
                fmlaInactive = payStartDate > fmlaApprDate;
            }

            // Check CTPL condition: CTPL_FORM IS NULL OR PAY_START_DATE > CTPL_END
            bool ctplInactive = row["CTPL_FORM"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString());
            if (!ctplInactive && !(row["CTPL_END_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_END_DATE"]?.ToString())))
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var ctplEndDate = Convert.ToDateTime(row["CTPL_END_DATE"]);
                ctplInactive = payStartDate > ctplEndDate;
            }

            // Both conditions must be true
            return fmlaInactive && ctplInactive;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;
                
            // Check FMLA condition: FMLA_APPR_DATE IS NULL OR PAY_START_DATE > FMLA_APPR_DATE
            bool fmlaInactive = !data.ContainsKey("FMLA_APPR_DATE") || 
                data["FMLA_APPR_DATE"] == null || 
                string.IsNullOrEmpty(data["FMLA_APPR_DATE"]?.ToString());
                
            if (!fmlaInactive && data.ContainsKey("PAY_START_DATE") && data["PAY_START_DATE"] != null)
            {
                var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                var fmlaApprDate = Convert.ToDateTime(data["FMLA_APPR_DATE"]);
                fmlaInactive = payStartDate > fmlaApprDate;
            }

            // Check CTPL condition: CTPL_FORM IS NULL OR PAY_START_DATE > CTPL_END
            bool ctplInactive = !data.ContainsKey("CTPL_FORM") || 
                data["CTPL_FORM"] == null || 
                string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString());
                
            if (!ctplInactive && 
                data.ContainsKey("CTPL_END_DATE") && 
                data["CTPL_END_DATE"] != null && 
                !string.IsNullOrEmpty(data["CTPL_END_DATE"]?.ToString()) &&
                data.ContainsKey("PAY_START_DATE") && 
                data["PAY_START_DATE"] != null)
            {
                var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                var ctplEndDate = Convert.ToDateTime(data["CTPL_END_DATE"]);
                ctplInactive = payStartDate > ctplEndDate;
            }

            // Both conditions must be true
            return fmlaInactive && ctplInactive;
        }
    }
}

