using System;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C26 : ICondition
    {
        public string Name => "Begin date is after pay start date";
        public string Description => "Checks if the leave begin date is after the start of the current pay week";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            try
            {
                // Get the dates from the row
                if (!DateTime.TryParse(row["BEGIN_DATE"].ToString(), out DateTime beginDate) ||
                    !DateTime.TryParse(row["PAY_START_DATE"].ToString(), out DateTime payStartDate))
                {
                    return false;
                }

                // Compare the dates
                return beginDate > payStartDate;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
} 