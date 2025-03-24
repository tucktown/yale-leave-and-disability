using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using ESLFeeder.Interfaces;
using ESLFeeder.Models.Converters;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Represents a leave scenario configuration for determining correct payment processing
    /// </summary>
    public class LeaveScenario : IValidatableObject
    {
        /// <summary>
        /// Unique identifier for the scenario
        /// </summary>
        [Required]
        public int Id { get; set; }
        
        /// <summary>
        /// Descriptive name of the scenario
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Process level determining when this scenario applies
        /// </summary>
        [Required]
        [JsonPropertyName("process_level")]
        public int ProcessLevel { get; set; }
        
        /// <summary>
        /// Reason code associated with this scenario
        /// </summary>
        [Required]
        [JsonPropertyName("reason_code")]
        public string ReasonCode { get; set; } = string.Empty;
        
        /// <summary>
        /// List of variables required for this scenario
        /// </summary>
        [JsonPropertyName("variables_required")]
        public List<string> VariablesRequired { get; set; } = new List<string>();
        
        /// <summary>
        /// Set of conditions that determine if this scenario applies
        /// </summary>
        [JsonPropertyName("conditions")]
        [JsonConverter(typeof(ConditionsConverter))]
        public ConditionSet Conditions { get; set; } = new ConditionSet();
        
        /// <summary>
        /// Order of field updates
        /// </summary>
        [JsonPropertyName("updates")]
        public UpdateContainer Updates { get; set; } = new UpdateContainer();
        
        /// <summary>
        /// Output values to apply when this scenario is matched (used after deserialization)
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, ScenarioOutput> Outputs { get; set; } = new Dictionary<string, ScenarioOutput>();
        
        /// <summary>
        /// Detailed description of what this scenario does
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this scenario is currently active
        /// </summary>
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Gets all required conditions for backward compatibility
        /// </summary>
        [JsonIgnore]
        public List<string> RequiredConditions => Conditions.RequiredConditions;
        
        /// <summary>
        /// Gets all excluded conditions for backward compatibility
        /// </summary>
        [JsonIgnore]
        public List<string> ExcludedConditions => Conditions.ExcludedConditions;
        
        /// <summary>
        /// Processes the Updates configuration into the Outputs dictionary after deserialization
        /// </summary>
        public void ProcessUpdatesIntoOutputs()
        {
            Outputs.Clear();
            
            Debug.WriteLine($"Processing updates for scenario {Id}: {Name}");
            Debug.WriteLine($"Updates is {(Updates == null ? "null" : "not null")}");
            Debug.WriteLine($"Updates.Order is {(Updates?.Order == null ? "null" : $"not null, count: {Updates.Order.Count}")}");
            Debug.WriteLine($"Updates.Fields is {(Updates?.Fields == null ? "null" : $"not null, count: {Updates.Fields.Count}")}");
            
            if (Updates?.Fields == null) 
            {
                Debug.WriteLine("No fields to process");
                return;
            }
            
            Debug.WriteLine($"Processing {Updates.Fields.Count} fields");
            
            foreach (var field in Updates.Fields)
            {
                var key = field.Key;
                var config = field.Value;
                
                Debug.WriteLine($"Processing field {key} with source {config.Source} and type {config.Type}");
                
                // Convert the update field to a ScenarioOutput
                ScenarioOutput output = null;
                
                if (config.Source == "null")
                {
                    output = ScenarioOutput.Null();
                }
                else if (config.Type == "double" || config.Type == "number")
                {
                    if (double.TryParse(config.Source, out double numValue))
                    {
                        output = ScenarioOutput.FromNumber(numValue);
                    }
                    else if (config.Calculation != null)
                    {
                        // For calculations, store the calculation as JSON in the string value
                        output = ScenarioOutput.FromString(JsonSerializer.Serialize(config.Calculation));
                    }
                    else
                    {
                        output = ScenarioOutput.FromString(config.Source);
                    }
                }
                else if (config.Type == "boolean" || config.Type == "bool")
                {
                    if (bool.TryParse(config.Source, out bool boolValue))
                    {
                        output = ScenarioOutput.FromBoolean(boolValue);
                    }
                    else
                    {
                        output = ScenarioOutput.FromString(config.Source);
                    }
                }
                else if (config.Type == "date" || config.Type == "datetime")
                {
                    if (DateTime.TryParse(config.Source, out DateTime dateValue))
                    {
                        output = ScenarioOutput.FromDateTime(dateValue);
                    }
                    else
                    {
                        output = ScenarioOutput.FromString(config.Source);
                    }
                }
                else // Default to string
                {
                    output = ScenarioOutput.FromString(config.Source);
                }
                
                if (output != null)
                {
                    Outputs[key] = output;
                }
            }
        }
        
        /// <summary>
        /// Validates the scenario configuration
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validate ID
            if (Id <= 0)
            {
                yield return new ValidationResult(
                    "Scenario ID must be a positive number",
                    new[] { nameof(Id) });
            }
            
            // Validate Name
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "Scenario name is required",
                    new[] { nameof(Name) });
            }
            
            // Validate ProcessLevel
            if (ProcessLevel <= 0)
            {
                yield return new ValidationResult(
                    "Process level must be a positive number",
                    new[] { nameof(ProcessLevel) });
            }
            
            // Validate ReasonCode
            if (string.IsNullOrWhiteSpace(ReasonCode))
            {
                yield return new ValidationResult(
                    "Reason code is required",
                    new[] { nameof(ReasonCode) });
            }
            
            // Validate Conditions
            if (!Conditions.Validate())
            {
                yield return new ValidationResult(
                    "One or more condition IDs are invalid",
                    new[] { nameof(Conditions) });
            }
        }
        
        /// <summary>
        /// Gets a string output value
        /// </summary>
        public string GetStringOutput(string key, string defaultValue = "")
        {
            return Outputs.TryGetValue(key, out var output) ? 
                output.ToString() : defaultValue;
        }
        
        /// <summary>
        /// Gets a numeric output value
        /// </summary>
        public double GetNumberOutput(string key, double defaultValue = 0)
        {
            if (Outputs.TryGetValue(key, out var output) && 
                output.Type == ScenarioOutput.OutputType.Number)
                return output.NumberValue;
                
            return defaultValue;
        }
        
        /// <summary>
        /// Gets a boolean output value
        /// </summary>
        public bool GetBooleanOutput(string key, bool defaultValue = false)
        {
            if (Outputs.TryGetValue(key, out var output) && 
                output.Type == ScenarioOutput.OutputType.Boolean)
                return output.BooleanValue;
                
            return defaultValue;
        }
        
        /// <summary>
        /// Gets a DateTime output value
        /// </summary>
        public DateTime GetDateTimeOutput(string key, DateTime? defaultValue = null)
        {
            if (Outputs.TryGetValue(key, out var output) && 
                output.Type == ScenarioOutput.OutputType.DateTime)
                return output.DateTimeValue;
                
            return defaultValue ?? DateTime.MinValue;
        }
    }
    
    /// <summary>
    /// Container for updates section in the JSON
    /// </summary>
    public class UpdateContainer
    {
        /// <summary>
        /// Order of field updates
        /// </summary>
        public List<string> Order { get; set; } = new List<string>();
        
        /// <summary>
        /// Field configurations
        /// </summary>
        public Dictionary<string, UpdateField> Fields { get; set; } = new Dictionary<string, UpdateField>();
    }
    
    /// <summary>
    /// Represents a field update configuration
    /// </summary>
    public class UpdateField
    {
        /// <summary>
        /// Source value or expression
        /// </summary>
        public string Source { get; set; } = string.Empty;
        
        /// <summary>
        /// Data type of the field
        /// </summary>
        public string Type { get; set; } = "string";
        
        /// <summary>
        /// Whether null values are allowed
        /// </summary>
        [JsonPropertyName("allow_null")]
        public bool AllowNull { get; set; }
        
        /// <summary>
        /// Calculation configuration for computed fields
        /// </summary>
        public object? Calculation { get; set; }
    }
} 