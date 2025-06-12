using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C18 : ICondition
    {
        public C18() { }
        public string Name => "C18";
        public string Description => "STD, CT PL, and FMLA are not approved. Cases need to be reviewed by HRConnect";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;
                
            // Check if all three approvals are null/empty
            bool ctplNotApproved = row["CTPL_APPROVED_AMOUNT"] == DBNull.Value || 
                string.IsNullOrEmpty(row["CTPL_APPROVED_AMOUNT"]?.ToString());
                
            bool fmlaNotApproved = row["FMLA_APPR_DATE"] == DBNull.Value || 
                string.IsNullOrEmpty(row["FMLA_APPR_DATE"]?.ToString());
                
            bool stdNotApproved = row["STD_APPROVED_THROUGH"] == DBNull.Value || 
                string.IsNullOrEmpty(row["STD_APPROVED_THROUGH"]?.ToString());

            // All three must be not approved (null/empty)
            return ctplNotApproved && fmlaNotApproved && stdNotApproved;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;
                
            // Check if all three approvals are null/empty
            bool ctplNotApproved = !data.ContainsKey("CTPL_APPROVED_AMOUNT") || 
                data["CTPL_APPROVED_AMOUNT"] == null || 
                string.IsNullOrEmpty(data["CTPL_APPROVED_AMOUNT"]?.ToString());
                
            bool fmlaNotApproved = !data.ContainsKey("FMLA_APPR_DATE") || 
                data["FMLA_APPR_DATE"] == null || 
                string.IsNullOrEmpty(data["FMLA_APPR_DATE"]?.ToString());
                
            bool stdNotApproved = !data.ContainsKey("STD_APPROVED_THROUGH") || 
                data["STD_APPROVED_THROUGH"] == null || 
                string.IsNullOrEmpty(data["STD_APPROVED_THROUGH"]?.ToString());

            // All three must be not approved (null/empty)
            return ctplNotApproved && fmlaNotApproved && stdNotApproved;
        }
    }
}

