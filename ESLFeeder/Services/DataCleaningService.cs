using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ESLFeeder.Services
{
    public interface IDataCleaningService
    {
        Task<DataTable> CleanData(DataTable inputData);
        DataRow CleanData(DataRow inputRow);
    }

    public class DataCleaningService : IDataCleaningService
    {
        private readonly ILogger<DataCleaningService> _logger;
        private readonly Dictionary<string, string> _columnMappings;

        public DataCleaningService(ILogger<DataCleaningService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Define column mappings
            _columnMappings = new Dictionary<string, string>
            {
                { "ACTUAL_END_DATE", "STD_APPROVED_THROUGH" },
                { "GLCOMPANY", "GL_COMPANY" }
                // Add more mappings as needed
            };
        }

        public async Task<DataTable> CleanData(DataTable inputData)
        {
            try
            {
                // Create a copy of the input data with all original columns
                var cleanedData = inputData.Clone();
                cleanedData.TableName = "CleanedData";

                // Add mapped columns if they don't exist
                foreach (var mapping in _columnMappings)
                {
                    if (!cleanedData.Columns.Contains(mapping.Value))
                    {
                        // Add the target column with the same data type as the source column
                        var sourceColumn = inputData.Columns[mapping.Key];
                        cleanedData.Columns.Add(mapping.Value, sourceColumn.DataType);
                    }
                }

                // Process each row
                foreach (DataRow inputRow in inputData.Rows)
                {
                    var cleanedRow = cleanedData.NewRow();

                    // First, copy all original columns
                    foreach (DataColumn column in inputData.Columns)
                    {
                        var columnName = column.ColumnName;
                        var value = inputRow[column];
                        
                        // Clean the value
                        cleanedRow[columnName] = CleanValue(value, column.DataType);

                        // If this column has a mapping, also set the mapped column
                        if (_columnMappings.TryGetValue(columnName, out string mappedColumn))
                        {
                            cleanedRow[mappedColumn] = CleanValue(value, column.DataType);
                        }
                    }

                    cleanedData.Rows.Add(cleanedRow);
                }

                _logger.LogInformation($"Cleaned {cleanedData.Rows.Count} rows of data");
                
                // Log sample of mapped columns for verification
                if (cleanedData.Rows.Count > 0)
                {
                    var firstRow = cleanedData.Rows[0];
                    foreach (var mapping in _columnMappings)
                    {
                        _logger.LogDebug($"Mapped column {mapping.Key} -> {mapping.Value}: {firstRow[mapping.Key]} -> {firstRow[mapping.Value]}");
                    }
                }

                return cleanedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning data");
                throw;
            }
        }

        public DataRow CleanData(DataRow inputRow)
        {
            try
            {
                // Create a copy of the input row in a new datatable
                var cleanedTable = inputRow.Table.Clone();
                
                // Add mapped columns if they don't exist
                foreach (var mapping in _columnMappings)
                {
                    if (!cleanedTable.Columns.Contains(mapping.Value))
                    {
                        // Add the target column with the same data type as the source column
                        var sourceColumn = inputRow.Table.Columns[mapping.Key];
                        cleanedTable.Columns.Add(mapping.Value, sourceColumn.DataType);
                    }
                }
                
                var cleanedRow = cleanedTable.NewRow();

                // First, copy all original columns
                foreach (DataColumn column in inputRow.Table.Columns)
                {
                    var columnName = column.ColumnName;
                    var value = inputRow[column];
                    
                    // Clean the value
                    cleanedRow[columnName] = CleanValue(value, column.DataType);

                    // If this column has a mapping, also set the mapped column
                    if (_columnMappings.TryGetValue(columnName, out string mappedColumn))
                    {
                        cleanedRow[mappedColumn] = CleanValue(value, column.DataType);
                    }
                }

                cleanedTable.Rows.Add(cleanedRow);
                _logger.LogInformation("Cleaned single row of data");
                return cleanedRow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning row data");
                throw;
            }
        }

        private object CleanValue(object value, Type dataType)
        {
            if (value == null || value == DBNull.Value)
            {
                return DBNull.Value;
            }

            try
            {
                if (dataType == typeof(DateTime))
                {
                    return CleanDateTime(value);
                }
                else if (dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
                {
                    return CleanNumeric(value);
                }
                else if (dataType == typeof(int) || dataType == typeof(long))
                {
                    return CleanInteger(value);
                }
                else if (dataType == typeof(string))
                {
                    return CleanString(value);
                }
                else
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning value {Value} of type {Type}", value, dataType);
                return DBNull.Value;
            }
        }

        private DateTime CleanDateTime(object value)
        {
            if (value is DateTime dt)
                return dt;

            if (DateTime.TryParse(value.ToString(), out DateTime parsedDate))
                return parsedDate;

            throw new FormatException($"Invalid date format: {value}");
        }

        private double CleanNumeric(object value)
        {
            if (value is double d)
                return d;

            if (double.TryParse(value.ToString(), out double parsed))
                return parsed;

            throw new FormatException($"Invalid numeric format: {value}");
        }

        private int CleanInteger(object value)
        {
            if (value is int i)
                return i;

            if (int.TryParse(value.ToString(), out int parsed))
                return parsed;

            throw new FormatException($"Invalid integer format: {value}");
        }

        private string CleanString(object value)
        {
            var cleaned = value.ToString().Trim();
            
            // Normalize reason codes to uppercase
            if (cleaned == "PREGNANCY" || cleaned == "pregnancy" || cleaned == "Pregnancy")
            {
                return "PREGNANCY";
            }
            else if (cleaned == "WORKERS COMPENSATION" || cleaned == "workers compensation" || cleaned == "Workers Compensation")
            {
                return "WORKERS COMPENSATION";
            }
            else if (cleaned == "MEDICAL/SURGICAL" || cleaned == "medical/surgical" || cleaned == "Medical/Surgical")
            {
                return "MEDICAL/SURGICAL";
            }
            else if (cleaned == "BONDING" || cleaned == "bonding" || cleaned == "Bonding")
            {
                return "BONDING";
            }
            
            return cleaned;
        }
    }
} 