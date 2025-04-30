using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C23 : ICondition
    {
        public string Name => "C23";
        public string Description => "STD, CTPL, and FMLA all inactive";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // Check STD condition
            bool stdInactive = row["STD_APPROVED_THROUGH"] == DBNull.Value || 
                string.IsNullOrEmpty(row["STD_APPROVED_THROUGH"]?.ToString());
            if (!stdInactive)
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var stdApprovedThrough = Convert.ToDateTime(row["STD_APPROVED_THROUGH"]);
                stdInactive = payStartDate >= stdApprovedThrough;
            }

            // Check CTPL condition
            bool ctplInactive = row["CTPL_FORM"] == DBNull.Value || 
                string.IsNullOrEmpty(row["CTPL_FORM"]?.ToString());
            if (!ctplInactive && !(row["CTPL_END_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["CTPL_END_DATE"]?.ToString())))
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var ctplEndDate = Convert.ToDateTime(row["CTPL_END_DATE"]);
                ctplInactive = payStartDate >= ctplEndDate;
            }

            // Check FMLA condition
            bool fmlaInactive = row["FMLA_APPR_DATE"] == DBNull.Value || 
                string.IsNullOrEmpty(row["FMLA_APPR_DATE"]?.ToString());
            if (!fmlaInactive)
            {
                var payStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                var payEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);
                var fmlaApprDate = Convert.ToDateTime(row["FMLA_APPR_DATE"]);
                fmlaInactive = payStartDate >= fmlaApprDate || fmlaApprDate < payEndDate;
            }

            // All three must be inactive
            return stdInactive && ctplInactive && fmlaInactive;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;

            // Check STD condition
            bool stdInactive = !data.ContainsKey("STD_APPROVED_THROUGH") || 
                data["STD_APPROVED_THROUGH"] == null || 
                string.IsNullOrEmpty(data["STD_APPROVED_THROUGH"]?.ToString());
            if (!stdInactive && data.ContainsKey("PAY_START_DATE") && data["PAY_START_DATE"] != null)
            {
                var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                var stdApprovedThrough = Convert.ToDateTime(data["STD_APPROVED_THROUGH"]);
                stdInactive = payStartDate >= stdApprovedThrough;
            }

            // Check CTPL condition
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
                ctplInactive = payStartDate >= ctplEndDate;
            }

            // Check FMLA condition
            bool fmlaInactive = !data.ContainsKey("FMLA_APPR_DATE") || 
                data["FMLA_APPR_DATE"] == null || 
                string.IsNullOrEmpty(data["FMLA_APPR_DATE"]?.ToString());
            if (!fmlaInactive && 
                data.ContainsKey("PAY_START_DATE") && 
                data["PAY_START_DATE"] != null &&
                data.ContainsKey("PAY_END_DATE") && 
                data["PAY_END_DATE"] != null)
            {
                var payStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                var payEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);
                var fmlaApprDate = Convert.ToDateTime(data["FMLA_APPR_DATE"]);
                fmlaInactive = payStartDate >= fmlaApprDate || fmlaApprDate < payEndDate;
            }

            // All three must be inactive
            return stdInactive && ctplInactive && fmlaInactive;
        }
    }
} 