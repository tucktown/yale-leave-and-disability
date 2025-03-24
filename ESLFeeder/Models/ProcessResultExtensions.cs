using System;
using System.Collections.Generic;
using System.Linq;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Extension methods for the ProcessResult class
    /// </summary>
    public static class ProcessResultExtensions
    {
        /// <summary>
        /// Marks the result as an error with the specified message
        /// </summary>
        public static ProcessResult WithError(this ProcessResult result, string errorMessage)
        {
            result.Success = false;
            result.Message = errorMessage;
            return result;
        }

        public static ProcessResult WithError(this ProcessResult result, Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            return result;
        }

        /// <summary>
        /// Marks the result as successful with the specified scenario and updates
        /// </summary>
        public static ProcessResult WithSuccess(this ProcessResult result, LeaveScenario scenario, Dictionary<string, double> updates)
        {
            result.Success = true;
            result.ScenarioId = scenario.Id;
            result.ScenarioName = scenario.Name;
            result.Updates = updates.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            
            // Also store the original typed outputs
            if (scenario.Outputs != null)
            {
                result.Outputs = new Dictionary<string, ScenarioOutput>(scenario.Outputs);
            }
            
            return result;
        }
        
        /// <summary>
        /// Adds condition evaluation results to the ProcessResult
        /// </summary>
        public static ProcessResult WithConditionResults(this ProcessResult result, Dictionary<string, bool> conditionResults)
        {
            result.EvaluatedConditions = conditionResults;
            return result;
        }
        
        /// <summary>
        /// Merges errors from one result into another
        /// </summary>
        public static ProcessResult MergeErrors(this ProcessResult target, ProcessResult source)
        {
            if (source == null || !source.HasErrors)
            {
                return target;
            }
            
            foreach (var error in source.Errors)
            {
                target.Errors.Add(error);
            }
            
            return target;
        }

        /// <summary>
        /// Marks the result as no scenario found
        /// </summary>
        public static ProcessResult WithNoScenarioFound(this ProcessResult result)
        {
            result.Success = false;
            result.ScenarioId = -1;
            result.ScenarioName = "No Scenario Found";
            var updates = new Dictionary<string, object>
            {
                { "LOA_STATUS", -1.0 },
                { "CTPL_STATUS", -1.0 }
            };
            result.Updates = updates;
            return result;
        }

        /// <summary>
        /// Marks the result as no scenario found and includes evaluation details
        /// </summary>
        public static ProcessResult WithNoScenarioFound(this ProcessResult result, LeaveVariables variables)
        {
            result.Success = false;
            result.ScenarioId = -1;
            result.ScenarioName = "No Scenario Found";
            var updates = new Dictionary<string, object>
            {
                { "LOA_STATUS", -1.0 },
                { "CTPL_STATUS", -1.0 }
            };
            result.Updates = updates;
            
            // Include the evaluation details for each scenario
            if (variables.EvaluatedConditions != null && variables.EvaluatedConditions.Count > 0)
            {
                foreach (var scenarioEval in variables.EvaluatedConditions)
                {
                    int scenarioId = scenarioEval.Key;
                    Dictionary<string, bool> conditions = scenarioEval.Value;
                    
                    result.AddEvaluationDetail(scenarioId, conditions);
                }
            }
            
            return result;
        }
    }
} 