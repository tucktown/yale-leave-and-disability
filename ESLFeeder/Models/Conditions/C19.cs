using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C19 : ICondition
    {
        public string Name => "C19";
        public string Description => "Calculates if usable PTO (current PTO balance - return to work reserve) is greater than employee's weekly scheduled hours.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // Get PTO available
            if (string.IsNullOrEmpty(row["PTO_AVAILABLE"]?.ToString()))
                return false;
                
            // Get scheduled hours
            if (string.IsNullOrEmpty(row["SCHED_HRS"]?.ToString()))
                return false;
                
            // Get return to work reserve (if available)
            double ptoRtwReserve = 0;
            if (!string.IsNullOrEmpty(row["EE_PTO_RTW"]?.ToString()))
            {
                ptoRtwReserve = Convert.ToDouble(row["EE_PTO_RTW"]);
            }
                
            var ptoAvailable = Convert.ToDouble(row["PTO_AVAILABLE"]);
            var schedHrs = Convert.ToDouble(row["SCHED_HRS"]);
            
            // Calculate usable PTO (available - rtw reserve)
            var ptoUsable = ptoAvailable - ptoRtwReserve;
            
            // Return true if usable PTO is greater than or equal to scheduled hours
            return ptoUsable >= schedHrs;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            if (data == null)
                return false;
                
            // Get PTO available
            if (!data.ContainsKey("PTO_AVAILABLE") || string.IsNullOrEmpty(data["PTO_AVAILABLE"]?.ToString()))
                return false;
                
            // Get scheduled hours
            if (!data.ContainsKey("SCHED_HRS") || string.IsNullOrEmpty(data["SCHED_HRS"]?.ToString()))
                return false;
                
            // Get return to work reserve (if available)
            double ptoRtwReserve = 0;
            if (data.ContainsKey("EE_PTO_RTW") && !string.IsNullOrEmpty(data["EE_PTO_RTW"]?.ToString()))
            {
                ptoRtwReserve = Convert.ToDouble(data["EE_PTO_RTW"]);
            }
                
            var ptoAvailable = Convert.ToDouble(data["PTO_AVAILABLE"]);
            var schedHrs = Convert.ToDouble(data["SCHED_HRS"]);
            
            // Calculate usable PTO (available - rtw reserve)
            var ptoUsable = ptoAvailable - ptoRtwReserve;
            
            // Return true if usable PTO is greater than or equal to scheduled hours
            return ptoUsable >= schedHrs;
        }
    }
} 