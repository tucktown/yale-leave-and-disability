using System;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C28 : ICondition
    {
        public C28() { }
        public string Name => "C28";
        public string Description => "Checks if the employee is per diem based on scheduled hours being less than 1";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            try
            {
                // Get the scheduled hours from the row
                if (!double.TryParse(row["SCHED_HRS"].ToString(), out double scheduledHours))
                {
                    return false;
                }

                // Check if scheduled hours are less than 1
                return scheduledHours < 1;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
} 