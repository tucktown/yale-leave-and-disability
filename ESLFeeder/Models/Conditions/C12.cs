using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C12 : ICondition
    {
        public string Name => "C12";
        public string Description => "Employee indicated they would like to supplement leave with PTO";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            // Check if EE_PTO_SUPP is Y
            if (row != null && row.Table.Columns.Contains("EE_PTO_SUPP"))
            {
                if (row["EE_PTO_SUPP"] != DBNull.Value)
                {
                    string eePtoSupp = row["EE_PTO_SUPP"].ToString().Trim();
                    return eePtoSupp.Equals("Y", StringComparison.OrdinalIgnoreCase);
                }
            }
            
            return false;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data != null && data.ContainsKey("EE_PTO_SUPP") && data["EE_PTO_SUPP"] != null)
            {
                string eePtoSupp = data["EE_PTO_SUPP"].ToString().Trim();
                return eePtoSupp.Equals("Y", StringComparison.OrdinalIgnoreCase);
            }
            
            return false;
        }
    }
}

