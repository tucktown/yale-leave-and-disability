using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C19 : ICondition
    {
        public C19() { }
        public string Name => "C19";
        public string Description => "Calculates if usable PTO (current PTO balance - return to work reserve) is greater than employee's weekly scheduled hours.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            return EvaluateInternal(row, variables);
        }

        private bool EvaluateInternal(object row, LeaveVariables variables)
        {
            if (row == null)
            {
                System.Diagnostics.Debug.WriteLine("C19: row is null");
                return false;
            }

            // Get PTO available
            object? ptoAvailableValue = null;
            object? schedHrsValue = null;
            object? eePtoRtwValue = null;

            if (row is System.Data.DataRow dataRow)
            {
                ptoAvailableValue = dataRow["PTO_AVAILABLE"];
                schedHrsValue = dataRow["SCHED_HRS"];
                eePtoRtwValue = dataRow["EE_PTO_RTW"];
            }
            else if (row is Dictionary<string, object> dict)
            {
                dict.TryGetValue("PTO_AVAILABLE", out ptoAvailableValue);
                dict.TryGetValue("SCHED_HRS", out schedHrsValue);
                dict.TryGetValue("EE_PTO_RTW", out eePtoRtwValue);
            }

            if (string.IsNullOrEmpty(ptoAvailableValue?.ToString()))
            {
                System.Diagnostics.Debug.WriteLine($"C19: PTO_AVAILABLE is null or empty");
                return false;
            }
                
            if (string.IsNullOrEmpty(schedHrsValue?.ToString()))
            {
                System.Diagnostics.Debug.WriteLine($"C19: SCHED_HRS is null or empty");
                return false;
            }
                
            var ptoAvailable = Convert.ToDouble(ptoAvailableValue);
            var schedHrs = Convert.ToDouble(schedHrsValue);
            
            // Get return to work reserve (if available)
            double ptoRtwReserve = 0;
            if (!string.IsNullOrEmpty(eePtoRtwValue?.ToString()))
            {
                string eePtoRtw = eePtoRtwValue.ToString().Trim();
                if (eePtoRtw.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    ptoRtwReserve = schedHrs * 2; // Reserve 2 weeks of scheduled hours
                }
            }
            
            // Calculate usable PTO (available - rtw reserve)
            var ptoUsable = ptoAvailable - ptoRtwReserve;
            
            System.Diagnostics.Debug.WriteLine($"C19: ptoAvailable={ptoAvailable}, schedHrs={schedHrs}, ptoRtwReserve={ptoRtwReserve}, ptoUsable={ptoUsable}, result={ptoUsable >= schedHrs}");
            
            // Return true if usable PTO is greater than or equal to scheduled hours
            return ptoUsable >= schedHrs;
        }
    }
} 