using System;
using System.Data;
using System.Threading.Tasks;
using ESLFeeder.Models;

namespace ESLFeeder.Interfaces
{
    /// <summary>
    /// Interface for CSV file processing operations
    /// </summary>
    public interface ICsvProcessor
    {
        /// <summary>
        /// Loads data from a CSV file into a DataTable
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="debugMode">Whether to enable debug logging</param>
        /// <returns>A DataTable containing the CSV data</returns>
        Task<DataTable> LoadCsvFile(string filePath, bool debugMode);

        /// <summary>
        /// Processes leave records from a DataTable
        /// </summary>
        /// <param name="data">The DataTable containing leave records</param>
        /// <param name="debugMode">Whether to enable debug logging</param>
        /// <returns>A processed DataTable with scenario results</returns>
        Task<DataTable> ProcessRecords(DataTable data, bool debugMode);

        /// <summary>
        /// Saves a DataTable to a CSV file
        /// </summary>
        /// <param name="data">The DataTable to save</param>
        /// <param name="outputPath">Path where the CSV file should be saved</param>
        /// <param name="debugMode">Whether to enable debug logging</param>
        /// <returns>A ProcessResult indicating success or failure</returns>
        ProcessResult SaveToCsv(DataTable data, string outputPath, bool debugMode);

        /// <summary>
        /// Processes a single record by CLAIM_ID and returns detailed processing information
        /// </summary>
        /// <param name="data">The DataTable containing the records</param>
        /// <param name="claimId">The CLAIM_ID to process</param>
        /// <param name="debugMode">Whether to enable debug logging</param>
        /// <returns>A ProcessResult containing detailed processing information</returns>
        Task<ProcessResult> ProcessSingleRecord(DataTable data, string claimId, bool debugMode);
    }
} 