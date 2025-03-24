using System.Collections.Generic;
using System.Data;
using ESLFeeder.Models;

namespace ESLFeeder.Interfaces
{
    /// <summary>
    /// Interface for leave scenario processing services
    /// </summary>
    public interface IScenarioProcessor
    {
        /// <summary>
        /// Processes a leave request and returns the appropriate actions
        /// </summary>
        /// <param name="leaveData">The leave request data</param>
        /// <returns>A ProcessResult containing the processing outcome</returns>
        ProcessResult ProcessLeaveRequest(DataRow leaveData);
        
        /// <summary>
        /// Processes a leave request and returns the appropriate actions
        /// </summary>
        /// <param name="leaveData">The leave request data as a dictionary</param>
        /// <returns>A ProcessResult containing the processing outcome</returns>
        ProcessResult ProcessLeaveRequest(Dictionary<string, object> leaveData);
        
        /// <summary>
        /// Processes a leave request using a pre-populated variables object
        /// </summary>
        /// <param name="variables">Pre-populated variables</param>
        /// <returns>A ProcessResult containing the processing outcome</returns>
        ProcessResult ProcessLeaveRequest(LeaveVariables variables);
        
        /// <summary>
        /// Validates a leave request
        /// </summary>
        /// <param name="leaveData">The leave request data</param>
        /// <returns>A ProcessResult indicating validation success or errors</returns>
        ProcessResult ValidateLeaveRequest(DataRow leaveData);
        
        /// <summary>
        /// Validates a leave request
        /// </summary>
        /// <param name="leaveData">The leave request data as a dictionary</param>
        /// <returns>A ProcessResult indicating validation success or errors</returns>
        ProcessResult ValidateLeaveRequest(Dictionary<string, object> leaveData);
        
        /// <summary>
        /// Gets scenarios applicable to the given leave request
        /// </summary>
        /// <param name="leaveData">The leave request data</param>
        /// <returns>A collection of applicable scenarios</returns>
        IEnumerable<LeaveScenario> GetApplicableScenarios(DataRow leaveData);
        
        /// <summary>
        /// Gets scenarios applicable to the given leave request
        /// </summary>
        /// <param name="leaveData">The leave request data as a dictionary</param>
        /// <returns>A collection of applicable scenarios</returns>
        IEnumerable<LeaveScenario> GetApplicableScenarios(Dictionary<string, object> leaveData);
        
        /// <summary>
        /// Finds a matching scenario for the given variables
        /// </summary>
        /// <param name="variables">The leave variables to match against</param>
        /// <returns>The matching scenario or null if no match found</returns>
        LeaveScenario FindMatchingScenario(LeaveVariables variables);
    }
} 