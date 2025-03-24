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
            // Check if EE_PTO_SUPP is true
            if (row != null && row.Table.Columns.Contains("EE_PTO_SUPP"))
            {
                if (row["EE_PTO_SUPP"] != DBNull.Value)
                {
                    if (bool.TryParse(row["EE_PTO_SUPP"].ToString(), out bool eePtoSupp))
                        return eePtoSupp;
                    
                    // Handle case where it's a string "true" or "false"
                    if (row["EE_PTO_SUPP"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            
            return variables.PtoSuppDollars > 0;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data != null && data.ContainsKey("EE_PTO_SUPP") && data["EE_PTO_SUPP"] != null)
            {
                if (data["EE_PTO_SUPP"] is bool eePtoSupp)
                    return eePtoSupp;
                
                string eePtoSuppStr = data["EE_PTO_SUPP"].ToString();
                if (bool.TryParse(eePtoSuppStr, out bool parsedValue))
                    return parsedValue;
                    
                if (eePtoSuppStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            return variables.PtoSuppDollars > 0;
        }
    }
}

