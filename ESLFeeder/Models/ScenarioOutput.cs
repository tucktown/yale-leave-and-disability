using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Represents a typed output value from a leave scenario
    /// </summary>
    [JsonConverter(typeof(ScenarioOutputConverter))]
    public class ScenarioOutput
    {
        /// <summary>
        /// The type of the output value
        /// </summary>
        public enum OutputType
        {
            String,
            Number,
            Boolean,
            DateTime,
            Null
        }
        
        /// <summary>
        /// The type of this output value
        /// </summary>
        public OutputType Type { get; private set; }
        
        /// <summary>
        /// The string value (if Type is String)
        /// </summary>
        public string StringValue { get; private set; }
        
        /// <summary>
        /// The numeric value (if Type is Number)
        /// </summary>
        public double NumberValue { get; private set; }
        
        /// <summary>
        /// The boolean value (if Type is Boolean)
        /// </summary>
        public bool BooleanValue { get; private set; }
        
        /// <summary>
        /// The DateTime value (if Type is DateTime)
        /// </summary>
        public DateTime DateTimeValue { get; private set; }

        /// <summary>
        /// Calculates the output value based on the provided variables
        /// </summary>
        /// <param name="variables">Dictionary of variable values</param>
        /// <returns>The calculated output value</returns>
        public object Calculate(Dictionary<string, double> variables)
        {
            switch (Type)
            {
                case OutputType.Number:
                    return NumberValue;
                case OutputType.Boolean:
                    return BooleanValue;
                case OutputType.String:
                    return StringValue;
                case OutputType.DateTime:
                    return DateTimeValue;
                case OutputType.Null:
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Creates a null output
        /// </summary>
        public static ScenarioOutput Null()
        {
            return new ScenarioOutput { Type = OutputType.Null };
        }
        
        /// <summary>
        /// Creates a string output
        /// </summary>
        public static ScenarioOutput FromString(string value)
        {
            return new ScenarioOutput
            {
                Type = OutputType.String,
                StringValue = value ?? string.Empty
            };
        }
        
        /// <summary>
        /// Creates a number output
        /// </summary>
        public static ScenarioOutput FromNumber(double value)
        {
            return new ScenarioOutput
            {
                Type = OutputType.Number,
                NumberValue = value
            };
        }
        
        /// <summary>
        /// Creates a boolean output
        /// </summary>
        public static ScenarioOutput FromBoolean(bool value)
        {
            return new ScenarioOutput
            {
                Type = OutputType.Boolean,
                BooleanValue = value
            };
        }
        
        /// <summary>
        /// Creates a DateTime output
        /// </summary>
        public static ScenarioOutput FromDateTime(DateTime value)
        {
            return new ScenarioOutput
            {
                Type = OutputType.DateTime,
                DateTimeValue = value
            };
        }
        
        /// <summary>
        /// Creates an output from a string representation, inferring the type
        /// </summary>
        public static ScenarioOutput Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Null();
                
            // Try boolean
            if (bool.TryParse(value, out bool boolValue))
                return FromBoolean(boolValue);
                
            // Try number
            if (double.TryParse(value, out double numValue))
                return FromNumber(numValue);
                
            // Try date
            if (DateTime.TryParse(value, out DateTime dateValue))
                return FromDateTime(dateValue);
                
            // Default to string
            return FromString(value);
        }
        
        /// <summary>
        /// Gets a string representation of the output value
        /// </summary>
        public override string ToString()
        {
            return Type switch
            {
                OutputType.String => StringValue,
                OutputType.Number => NumberValue.ToString(),
                OutputType.Boolean => BooleanValue.ToString().ToLower(),
                OutputType.DateTime => DateTimeValue.ToString("yyyy-MM-dd"),
                OutputType.Null => string.Empty,
                _ => string.Empty
            };
        }
    }
    
    /// <summary>
    /// Converts strings to/from ScenarioOutput objects
    /// </summary>
    public class ScenarioOutputConverter : JsonConverter<ScenarioOutput>
    {
        public override ScenarioOutput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return ScenarioOutput.Parse(reader.GetString());
                    
                case JsonTokenType.Number:
                    return ScenarioOutput.FromNumber(reader.GetDouble());
                    
                case JsonTokenType.True:
                    return ScenarioOutput.FromBoolean(true);
                    
                case JsonTokenType.False:
                    return ScenarioOutput.FromBoolean(false);
                    
                case JsonTokenType.Null:
                    return ScenarioOutput.Null();
                    
                default:
                    throw new JsonException($"Cannot convert {reader.TokenType} to ScenarioOutput");
            }
        }
        
        public override void Write(Utf8JsonWriter writer, ScenarioOutput value, JsonSerializerOptions options)
        {
            switch (value.Type)
            {
                case ScenarioOutput.OutputType.String:
                    writer.WriteStringValue(value.StringValue);
                    break;
                    
                case ScenarioOutput.OutputType.Number:
                    writer.WriteNumberValue(value.NumberValue);
                    break;
                    
                case ScenarioOutput.OutputType.Boolean:
                    writer.WriteBooleanValue(value.BooleanValue);
                    break;
                    
                case ScenarioOutput.OutputType.DateTime:
                    writer.WriteStringValue(value.DateTimeValue.ToString("yyyy-MM-dd"));
                    break;
                    
                case ScenarioOutput.OutputType.Null:
                default:
                    writer.WriteNullValue();
                    break;
            }
        }
    }
} 