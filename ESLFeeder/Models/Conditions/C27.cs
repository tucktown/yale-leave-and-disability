using System;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C27 : ICondition
    {
        public string Name => "Employee returned to work before or during pay week";
        public string Description => "Checks if the employee's return to work date is before or during the current pay week";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            try
            {
                // Get the dates from the row
                if (!DateTime.TryParse(row["RTW_FT"].ToString(), out DateTime rtwDate) ||
                    !DateTime.TryParse(row["PAY_START_DATE"].ToString(), out DateTime payStartDate) ||
                    !DateTime.TryParse(row["PAY_END_DATE"].ToString(), out DateTime payEndDate))
                {
                    return false;
                }

                // Check if RTW date is before or during pay week
                return rtwDate <= payEndDate;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
} 