using System;
using System.Collections.Generic;
using System.Linq;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Represents the result of processing a leave request
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Whether the processing was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// A message describing the result
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error message when processing fails
        /// </summary>
        public string ErrorMessage 
        { 
            get => Message; 
            set => Message = value; 
        }
        
        /// <summary>
        /// The ID of the selected scenario, if any
        /// </summary>
        public int? ScenarioId { get; set; }
        
        /// <summary>
        /// The name of the selected scenario, if any
        /// </summary>
        public string ScenarioName { get; set; } = string.Empty;
        
        /// <summary>
        /// The description of the selected scenario, if any
        /// </summary>
        public string ScenarioDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// The required conditions for the selected scenario
        /// </summary>
        public List<string> RequiredConditions { get; set; } = new List<string>();
        
        /// <summary>
        /// The forbidden conditions for the selected scenario
        /// </summary>
        public List<string> ForbiddenConditions { get; set; } = new List<string>();
        
        /// <summary>
        /// The updates to apply as a result of processing the leave request
        /// </summary>
        public Dictionary<string, object> Updates { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// The original typed outputs from the scenario, if any
        /// </summary>
        public Dictionary<string, ScenarioOutput> Outputs { get; set; } = new Dictionary<string, ScenarioOutput>();
        
        /// <summary>
        /// Any errors that occurred during processing
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Which conditions were evaluated and their results
        /// </summary>
        public Dictionary<string, bool> EvaluatedConditions { get; set; } = new Dictionary<string, bool>();
        
        /// <summary>
        /// Detailed evaluation results for all scenarios
        /// </summary>
        public Dictionary<int, Dictionary<string, bool>> ScenarioEvaluations { get; set; } = new Dictionary<int, Dictionary<string, bool>>();
        
        /// <summary>
        /// Whether the result has any errors
        /// </summary>
        public bool HasErrors => Errors.Any();
        
        /// <summary>
        /// Adds an error to the result and returns the result
        /// </summary>
        public ProcessResult AddError(string message)
        {
            Errors.Add(message);
            return this;
        }
        
        /// <summary>
        /// Adds a typed output to the result
        /// </summary>
        public ProcessResult AddOutput(string key, ScenarioOutput value)
        {
            Outputs[key] = value;
            return this;
        }
        
        /// <summary>
        /// Adds a numeric output value to the result
        /// </summary>
        public ProcessResult AddOutputValue(string key, double value)
        {
            Outputs[key] = ScenarioOutput.FromNumber(value);
            return this;
        }
        
        /// <summary>
        /// Adds evaluation details for a scenario
        /// </summary>
        public ProcessResult AddEvaluationDetail(int scenarioId, Dictionary<string, bool> conditions)
        {
            ScenarioEvaluations[scenarioId] = conditions;
            return this;
        }

        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, object> RawData { get; set; }

        public ProcessResult()
        {
            Updates = new Dictionary<string, object>();
            Variables = new Dictionary<string, object>();
            RawData = new Dictionary<string, object>();
            ScenarioEvaluations = new Dictionary<int, Dictionary<string, bool>>();
        }

        public ProcessResult WithError(string message)
        {
            Success = false;
            Message = message;
            ErrorMessage = message;
            return this;
        }

        public ProcessResult WithSuccess(string message)
        {
            Success = true;
            Message = message;
            return this;
        }

        public ProcessResult WithSuccess(LeaveScenario scenario, Dictionary<string, object> updates)
        {
            Success = true;
            ScenarioId = scenario.Id;
            ScenarioName = scenario.Name;
            Updates = updates;
            return this;
        }
        
        public ProcessResult WithNoScenarioFound(LeaveVariables variables)
        {
            Success = false;
            Message = "No matching scenario found for the given variables";
            return this;
        }
        
        public ProcessResult WithError(Exception ex)
        {
            Success = false;
            Message = ex.Message;
            ErrorMessage = ex.Message;
            return this;
        }
    }
} 