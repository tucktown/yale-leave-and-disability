using System.Collections.Generic;
using ESLFeeder.Models;

namespace ESLFeeder.Interfaces
{
    /// <summary>
    /// Interface for accessing scenario configurations
    /// </summary>
    public interface IScenarioConfiguration
    {
        /// <summary>
        /// Get all available scenarios
        /// </summary>
        IEnumerable<LeaveScenario> Scenarios { get; }

        /// <summary>
        /// Get scenarios that support a specific process level
        /// </summary>
        /// <param name="processLevel">The process level to filter by</param>
        /// <returns>All scenarios that support the specified process level</returns>
        IEnumerable<LeaveScenario> GetScenariosForProcessLevel(int processLevel);

        /// <summary>
        /// Get scenarios that support a specific process level using a string (for backward compatibility)
        /// </summary>
        /// <param name="processLevel">The process level to filter by as a string</param>
        /// <returns>All scenarios that support the specified process level</returns>
        IEnumerable<LeaveScenario> GetScenariosForProcessLevel(string processLevel);

        /// <summary>
        /// Reload configurations from source
        /// </summary>
        void ReloadConfigurations();

        /// <summary>
        /// Get all scenarios for a specific reason code that support the specified process level
        /// </summary>
        /// <param name="reasonCode">The reason code to filter by</param>
        /// <param name="processLevel">The process level to filter by</param>
        /// <returns>A list of scenarios matching the reason code and supporting the process level</returns>
        List<LeaveScenario> GetScenariosForReasonCode(string reasonCode, int processLevel);
        
        /// <summary>
        /// Gets all valid reason codes from the configuration
        /// </summary>
        /// <returns>A list of valid reason codes</returns>
        List<string> GetValidReasonCodes();
        
        /// <summary>
        /// Gets all scenarios
        /// </summary>
        /// <returns>A list of all scenarios</returns>
        List<LeaveScenario> GetScenarios();
    }
} 