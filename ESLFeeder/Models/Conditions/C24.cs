using System;
using System.Data;
using System.Collections.Generic;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using Microsoft.Extensions.Logging;

namespace ESLFeeder.Models.Conditions
{
    public class C24 : ICondition
    {
        private readonly ILogger<C24> _logger;

        public C24(ILogger<C24> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "C24";
        public string Description => "STD or CTPL starts or ends during pay week (Partial week)";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            if (row == null)
                return false;

            // Get required date values
            var stdApprovedThrough = GetDateValue(row, "STD_APPROVED_THROUGH");
            var ctplStartDate = GetDateValue(row, "CTPL_START_DATE");
            var ctplEndDate = GetDateValue(row, "CTPL_END_DATE");
            var payStartDate = GetDateValue(row, "PAY_START_DATE");
            var payEndDate = GetDateValue(row, "PAY_END_DATE");

            _logger.LogDebug($"C24 Evaluation - Dates:");
            _logger.LogDebug($"  PAY_START_DATE: {payStartDate}");
            _logger.LogDebug($"  PAY_END_DATE: {payEndDate}");
            _logger.LogDebug($"  STD_APPROVED_THROUGH: {stdApprovedThrough}");
            _logger.LogDebug($"  CTPL_START_DATE: {ctplStartDate}");
            _logger.LogDebug($"  CTPL_END_DATE: {ctplEndDate}");

            // Check if STD ends during pay week
            bool stdEndsDuringPayWeek = stdApprovedThrough.HasValue && 
                stdApprovedThrough.Value >= payStartDate && 
                stdApprovedThrough.Value <= payEndDate;

            // Check if CTPL starts during pay week
            bool ctplStartsDuringPayWeek = ctplStartDate.HasValue && 
                ctplStartDate.Value >= payStartDate && 
                ctplStartDate.Value <= payEndDate;

            // Check if CTPL ends during pay week
            bool ctplEndsDuringPayWeek = ctplEndDate.HasValue && 
                ctplEndDate.Value >= payStartDate && 
                ctplEndDate.Value <= payEndDate;

            _logger.LogDebug($"C24 Evaluation - Results:");
            _logger.LogDebug($"  stdEndsDuringPayWeek: {stdEndsDuringPayWeek}");
            _logger.LogDebug($"  ctplStartsDuringPayWeek: {ctplStartsDuringPayWeek}");
            _logger.LogDebug($"  ctplEndsDuringPayWeek: {ctplEndsDuringPayWeek}");

            // Return true if any of the conditions are met
            bool result = stdEndsDuringPayWeek || ctplStartsDuringPayWeek || ctplEndsDuringPayWeek;
            _logger.LogDebug($"C24 Final Result: {result}");
            return result;
        }

        private DateTime? GetDateValue(DataRow row, string columnName)
        {
            if (row[columnName] == DBNull.Value || string.IsNullOrEmpty(row[columnName]?.ToString()))
                return null;

            if (DateTime.TryParse(row[columnName].ToString(), out DateTime result))
                return result;

            return null;
        }
    }
} 