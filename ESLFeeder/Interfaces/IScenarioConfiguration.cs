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
        /// Get scenarios by process level
        /// </summary>
        /// <param name="processLevel">The process level to filter by</param>
        /// <returns>All scenarios matching the process level</returns>
        IEnumerable<LeaveScenario> GetScenariosForProcessLevel(int processLevel);

        /// <summary>
        /// Get scenarios by process level using a string (for backward compatibility)
        /// </summary>
        /// <param name="processLevel">The process level to filter by as a string</param>
        /// <returns>All scenarios matching the process level</returns>
        IEnumerable<LeaveScenario> GetScenariosForProcessLevel(string processLevel);

        /// <summary>
        /// Reload configurations from source
        /// </summary>
        void ReloadConfigurations();

        List<LeaveScenario> GetScenariosForReasonCode(string reasonCode, int processLevel);
    }
} 