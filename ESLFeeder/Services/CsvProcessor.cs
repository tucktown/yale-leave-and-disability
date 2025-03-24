using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Services
{
    public class CsvProcessor : ICsvProcessor
    {
        private readonly IScenarioProcessor _scenarioProcessor;
        private readonly IDataCleaningService _dataCleaningService;
        private readonly IVariableCalculator _variableCalculator;
        private readonly ILogger<CsvProcessor> _logger;

        public CsvProcessor(
            IScenarioProcessor scenarioProcessor,
            IDataCleaningService dataCleaningService,
            IVariableCalculator variableCalculator,
            ILogger<CsvProcessor> logger)
        {
            _scenarioProcessor = scenarioProcessor ?? throw new ArgumentNullException(nameof(scenarioProcessor));
            _dataCleaningService = dataCleaningService ?? throw new ArgumentNullException(nameof(dataCleaningService));
            _variableCalculator = variableCalculator ?? throw new ArgumentNullException(nameof(variableCalculator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DataTable> LoadCsvFile(string filePath, bool debugMode)
        {
            if (debugMode)
            {
                Console.WriteLine($"Loading CSV file: {filePath}");
            }

            if (!File.Exists(filePath))
            {
                var errorMessage = $"CSV file not found at {filePath}";
                _logger.LogError(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            var dt = new DataTable();
            
            try
            {
                // Read the CSV file with FileShare.ReadWrite to allow concurrent access
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    // Read header
                    string? header = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(header))
                    {
                        throw new Exception("CSV file is empty or header is missing");
                    }

                    // Add columns
                    foreach (string column in header.Split(','))
                    {
                        dt.Columns.Add(column.Trim());
                    }

                    // Add scenario columns if not present
                    if (!dt.Columns.Contains("SCENARIO_ID"))
                        dt.Columns.Add("SCENARIO_ID");
                    if (!dt.Columns.Contains("SCENARIO_NAME"))
                        dt.Columns.Add("SCENARIO_NAME");

                    // Read data
                    int rowCount = 0;
                    while (!reader.EndOfStream)
                    {
                        string? line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) continue;

                        var values = line.Split(',');
                        var row = dt.NewRow();
                        
                        for (int i = 0; i < values.Length && i < dt.Columns.Count; i++)
                        {
                            row[i] = values[i].Trim();
                        }
                        
                        dt.Rows.Add(row);
                        rowCount++;

                        if (debugMode && rowCount % 100 == 0)
                        {
                            Console.WriteLine($"Loaded {rowCount} rows...");
                        }
                    }

                    if (debugMode)
                    {
                        Console.WriteLine($"Successfully loaded {rowCount} rows from {filePath}");
                        // Display sample data in debug mode
                        if (dt.Rows.Count > 0)
                        {
                            Console.WriteLine("\nSample data from first record:");
                            foreach (DataColumn col in dt.Columns)
                            {
                                if (dt.Rows[0][col] != null && !string.IsNullOrEmpty(dt.Rows[0][col].ToString()))
                                {
                                    Console.WriteLine($"  {col.ColumnName} = {dt.Rows[0][col]}");
                                }
                            }
                            Console.WriteLine();
                        }
                    }
                }

                return dt;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error loading CSV file: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task<DataTable> ProcessRecords(DataTable data, bool debugMode)
        {
            if (debugMode)
            {
                Console.WriteLine($"Processing {data.Rows.Count} records...");
            }

            try
            {
                // Clean the data
                var cleanedData = await _dataCleaningService.CleanData(data);
                
                if (debugMode)
                {
                    Console.WriteLine("Data cleaning completed");
                }

                int successCount = 0;
                int failureCount = 0;
                int processedCount = 0;

                // Get all possible scenario output columns from the configuration
                var scenarioOutputColumns = new[]
                {
                    "PAID_HRS", "PTO_HRS", "LOA_NO_HRS_PAID", "BASIC_SICK_HRS",
                    "BRIDGEPORT_SICK_HRS", "LM_PTO_HRS", "LM_SICK_HRS", "ATO_HRS",
                    "EXEMPT_HRS", "EXEC_NOTE", "PHYS_NOTE", "MANUAL_CHECK",
                    "ENTRY_DATE", "AUTH_BY", "CHECK_KRONOS"
                };

                // Add scenario output columns if they don't exist
                foreach (var column in scenarioOutputColumns)
                {
                    if (!cleanedData.Columns.Contains(column))
                    {
                        cleanedData.Columns.Add(column);
                    }
                }

                // Process each row
                foreach (DataRow row in cleanedData.Rows)
                {
                    try
                    {
                        // Process the row through the scenario processor
                        var result = _scenarioProcessor.ProcessLeaveRequest(row);
                        
                        // Add scenario results to the row
                        if (result.Success)
                        {
                            row["SCENARIO_ID"] = result.ScenarioId;
                            row["SCENARIO_NAME"] = result.ScenarioName;
                            
                            // Apply all scenario outputs
                            foreach (var update in result.Updates)
                            {
                                if (cleanedData.Columns.Contains(update.Key))
                                {
                                    // Convert the value to the appropriate type based on the column's data type
                                    var column = cleanedData.Columns[update.Key];
                                    if (update.Value == null)
                                    {
                                        row[update.Key] = DBNull.Value;
                                    }
                                    else if (column.DataType == typeof(DateTime) && update.Value is DateTime dateValue)
                                    {
                                        row[update.Key] = dateValue;
                                    }
                                    else if (column.DataType == typeof(double) || column.DataType == typeof(decimal))
                                    {
                                        if (update.Value is double doubleValue)
                                        {
                                            row[update.Key] = doubleValue;
                                        }
                                        else if (double.TryParse(update.Value.ToString(), out double parsedValue))
                                        {
                                            row[update.Key] = parsedValue;
                                        }
                                        else
                                        {
                                            row[update.Key] = 0.0;
                                        }
                                    }
                                    else
                                    {
                                        row[update.Key] = update.Value.ToString();
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Output column {Column} not found in data table", update.Key);
                                }
                            }
                            
                            successCount++;
                            
                            if (debugMode && processedCount == 0)
                            {
                                Console.WriteLine("\nSample scenario outputs:");
                                foreach (var update in result.Updates)
                                {
                                    Console.WriteLine($"  {update.Key} = {update.Value}");
                                }
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            row["SCENARIO_ID"] = -1;
                            row["SCENARIO_NAME"] = $"Error: {result.Message}";
                            failureCount++;
                            
                            // Log specific errors for debugging
                            if (result.HasErrors)
                            {
                                _logger.LogWarning("Errors for row {RowIndex}: {Errors}", 
                                    processedCount, string.Join("; ", result.Errors));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing row {RowIndex}", processedCount);
                        row["SCENARIO_ID"] = -1;
                        row["SCENARIO_NAME"] = $"Exception: {ex.Message}";
                        failureCount++;
                    }

                    processedCount++;
                    
                    if (debugMode && processedCount % 10 == 0)
                    {
                        var progress = (double)processedCount / cleanedData.Rows.Count * 100;
                        Console.WriteLine($"Processed {processedCount}/{cleanedData.Rows.Count} records ({progress:F1}%)...");
                    }
                }

                if (debugMode)
                {
                    Console.WriteLine($"\nProcessing complete:");
                    Console.WriteLine($"  Total records: {cleanedData.Rows.Count}");
                    Console.WriteLine($"  Successful: {successCount}");
                    Console.WriteLine($"  Failed: {failureCount}");
                    Console.WriteLine($"  Success rate: {(double)successCount / cleanedData.Rows.Count * 100:F1}%");
                }

                return cleanedData;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error processing records: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        public ProcessResult SaveToCsv(DataTable data, string outputPath, bool debugMode)
        {
            var result = new ProcessResult();
            
            if (debugMode)
            {
                Console.WriteLine($"Saving results to {outputPath}");
            }

            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(outputPath))
                {
                    // Write headers
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        writer.Write(data.Columns[i].ColumnName);
                        if (i < data.Columns.Count - 1)
                            writer.Write(",");
                    }
                    writer.WriteLine();

                    // Write data
                    foreach (DataRow row in data.Rows)
                    {
                        for (int i = 0; i < data.Columns.Count; i++)
                        {
                            string value = row[i]?.ToString() ?? string.Empty;
                            
                            // If the value contains a comma, quote it
                            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                            {
                                value = $"\"{value.Replace("\"", "\"\"")}\"";
                            }
                            
                            writer.Write(value);
                            if (i < data.Columns.Count - 1)
                                writer.Write(",");
                        }
                        writer.WriteLine();
                    }
                }
                
                result.Success = true;
                result.Message = $"Successfully saved {data.Rows.Count} records to {outputPath}";
                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error saving CSV file: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return result.WithError(errorMessage);
            }
        }

        public async Task<ProcessResult> ProcessSingleRecord(DataTable data, string claimId, bool debugMode)
        {
            var result = new ProcessResult();
            
            try
            {
                // Find the record with matching CLAIM_ID
                var matchingRows = data.Select($"CLAIM_ID = '{claimId}'");
                if (!matchingRows.Any())
                {
                    return result.WithError($"No record found with CLAIM_ID: {claimId}");
                }

                var row = matchingRows[0];
                
                // Clean the data
                var cleanedData = await _dataCleaningService.CleanData(data);
                
                // Get the cleaned row
                var cleanedRow = cleanedData.Select($"CLAIM_ID = '{claimId}'")[0];

                // Calculate variables
                if (!_variableCalculator.CalculateVariables(cleanedRow, out var variables))
                {
                    return result.WithError("Failed to calculate variables");
                }

                // Process the row through the scenario processor
                var scenarioResult = _scenarioProcessor.ProcessLeaveRequest(cleanedRow);
                
                // Add detailed information to the result
                result.Success = scenarioResult.Success;
                result.Message = scenarioResult.Message;
                result.ScenarioId = scenarioResult.ScenarioId;
                result.ScenarioName = scenarioResult.ScenarioName;
                result.Updates = scenarioResult.Updates;
                result.Variables = new Dictionary<string, object>();
                
                // Convert LeaveVariables to Dictionary<string, object>
                foreach (var prop in variables.GetType().GetProperties())
                {
                    result.Variables[prop.Name] = prop.GetValue(variables);
                }
                
                result.RawData = new Dictionary<string, object>();
                
                // Add all raw data from the row
                foreach (DataColumn col in cleanedRow.Table.Columns)
                {
                    result.RawData[col.ColumnName] = cleanedRow[col];
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing single record with CLAIM_ID: {ClaimId}", claimId);
                return result.WithError($"Error processing record: {ex.Message}");
            }
        }
    }
} 