using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C25 : ICondition
    {
        public C25() { }
        public string Name => "C25";
        public string Description => "CTPL Approved Indicator and CTPL Denied Indicator are both 'Y'";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // Check if columns exist before accessing them
            bool approvedIndExists = row.Table.Columns.Contains("CTPL_APPROVED_IND");
            bool deniedIndExists = row.Table.Columns.Contains("CTPL_DENIED_IND");

            if (!approvedIndExists || !deniedIndExists)
                return false; // Or log a warning/error if columns are expected

            // Check if CTPL_APPROVED_IND = 'Y' and CTPL_DENIED_IND = 'Y' (case-insensitive)
            bool isApproved = row["CTPL_APPROVED_IND"] != DBNull.Value && 
                              row["CTPL_APPROVED_IND"].ToString().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);
            
            bool isDenied = row["CTPL_DENIED_IND"] != DBNull.Value && 
                            row["CTPL_DENIED_IND"].ToString().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);

            return isApproved && isDenied;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;

            // Check if keys exist before accessing them
            bool approvedIndExists = data.ContainsKey("CTPL_APPROVED_IND");
            bool deniedIndExists = data.ContainsKey("CTPL_DENIED_IND");

            if (!approvedIndExists || !deniedIndExists)
                return false; // Or log a warning/error

            // Check if CTPL_APPROVED_IND = 'Y' and CTPL_DENIED_IND = 'Y' (case-insensitive)
            object approvedValue = data["CTPL_APPROVED_IND"];
            object deniedValue = data["CTPL_DENIED_IND"];

            bool isApproved = approvedValue != null && 
                              approvedValue.ToString().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);
            
            bool isDenied = deniedValue != null && 
                            deniedValue.ToString().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);

            return isApproved && isDenied;
        }
    }
} 