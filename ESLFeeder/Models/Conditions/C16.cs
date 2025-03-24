using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C16 : ICondition
    {
        public string Name => "C16";
        public string Description => "FMLA is approved and active";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // If FMLA_APPR_DATE is empty, FMLA is not active
            if (row == null || row["FMLA_APPR_DATE"] == DBNull.Value || string.IsNullOrEmpty(row["FMLA_APPR_DATE"]?.ToString()))
                return false;

            var payEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);
            var fmlaApprDate = Convert.ToDateTime(row["FMLA_APPR_DATE"]);

            // Return true if pay end date is before or equal to FMLA approval date
            return payEndDate <= fmlaApprDate;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            // If FMLA_APPR_DATE is empty, FMLA is not active
            if (data == null || !data.ContainsKey("FMLA_APPR_DATE") || 
                data["FMLA_APPR_DATE"] == null || string.IsNullOrEmpty(data["FMLA_APPR_DATE"]?.ToString()))
                return false;

            if (!data.ContainsKey("PAY_END_DATE") || data["PAY_END_DATE"] == null)
                return false;

            var payEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);
            var fmlaApprDate = Convert.ToDateTime(data["FMLA_APPR_DATE"]);

            // Return true if pay end date is before or equal to FMLA approval date
            return payEndDate <= fmlaApprDate;
        }
    }
}

