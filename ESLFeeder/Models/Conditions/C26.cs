using System;
using System.Data;
using System.Collections.Generic;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using Microsoft.Extensions.Logging;

namespace ESLFeeder.Models.Conditions
{
    public class C26 : ICondition
    {
        private readonly ILogger<C26> _logger;

        public C26(ILogger<C26> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "C26";
        public string Description => "Leave Begin Date is after the start of the current pay week";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            var beginDate = GetDateValue(row, "BEGIN_DATE");
            var payStartDate = GetDateValue(row, "PAY_START_DATE");

            if (!beginDate.HasValue || !payStartDate.HasValue)
                return false;

            bool result = beginDate.Value > payStartDate.Value;
            
            _logger.LogDebug("C26 Evaluation: BeginDate ({BeginDate}) > PayStartDate ({PayStartDate}) = {Result}", beginDate.Value, payStartDate.Value, result);

            return result;
        }

        private DateTime? GetDateValue(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value || string.IsNullOrEmpty(row[columnName]?.ToString()))
                return null;

            if (DateTime.TryParse(row[columnName].ToString(), out DateTime result))
                return result;

            return null;
        }
    }
} 