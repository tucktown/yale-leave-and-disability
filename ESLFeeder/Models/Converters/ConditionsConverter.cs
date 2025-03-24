using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace ESLFeeder.Models.Converters
{
    public class ConditionsConverter : JsonConverter<ConditionSet>
    {
        public override ConditionSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object for conditions");
            }
            
            var result = new ConditionSet();
            
            try
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return result;
                    }
                    
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propName = reader.GetString();
                        reader.Read();
                        
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            List<string> conditions = new List<string>();
                            
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    conditions.Add(reader.GetString());
                                }
                                else
                                {
                                    Debug.WriteLine($"Warning: Expected string in conditions array for '{propName}', but got {reader.TokenType}");
                                }
                            }
                            
                            // Handle different condition types
                            switch (propName.ToLowerInvariant())
                            {
                                case "required":
                                    result.RequiredConditions = conditions;
                                    break;
                                case "forbidden":
                                case "excluded":
                                    result.ExcludedConditions = conditions;
                                    break;
                                case "optional":
                                    result.OptionalConditions = conditions;
                                    break;
                                default:
                                    Debug.WriteLine($"Warning: Unknown condition type '{propName}' in conditions object");
                                    break;
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Expected array for condition type '{propName}', but got {reader.TokenType}");
                            // Skip this property
                            reader.Skip();
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Error parsing conditions: {ex.Message}");
                throw new JsonException($"Failed to parse conditions: {ex.Message}", ex);
            }
            
            return result;
        }
        
        public override void Write(Utf8JsonWriter writer, ConditionSet value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            // Write required conditions
            if (value.RequiredConditions?.Count > 0)
            {
                writer.WritePropertyName("required");
                writer.WriteStartArray();
                foreach (var item in value.RequiredConditions)
                {
                    writer.WriteStringValue(item);
                }
                writer.WriteEndArray();
            }
            
            // Write excluded conditions
            if (value.ExcludedConditions?.Count > 0)
            {
                writer.WritePropertyName("forbidden");
                writer.WriteStartArray();
                foreach (var item in value.ExcludedConditions)
                {
                    writer.WriteStringValue(item);
                }
                writer.WriteEndArray();
            }
            
            // Write optional conditions
            if (value.OptionalConditions?.Count > 0)
            {
                writer.WritePropertyName("optional");
                writer.WriteStartArray();
                foreach (var item in value.OptionalConditions)
                {
                    writer.WriteStringValue(item);
                }
                writer.WriteEndArray();
            }
            
            writer.WriteEndObject();
        }
    }
} 