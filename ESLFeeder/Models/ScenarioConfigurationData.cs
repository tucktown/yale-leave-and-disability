using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ESLFeeder.Models
{
    public class ScenarioConfigurationData
    {
        [JsonPropertyName("schema_version")]
        public string SchemaVersion { get; set; }
        public ConfigurationMetadata Metadata { get; set; }
        public List<LeaveScenario> Scenarios { get; set; }
    }

    public class ConfigurationMetadata
    {
        [JsonPropertyName("valid_process_levels")]
        public List<int> ValidProcessLevels { get; set; }
        [JsonPropertyName("valid_reason_codes")]
        public List<string> ValidReasonCodes { get; set; }
        [JsonPropertyName("default_values")]
        public DefaultValues DefaultValues { get; set; }
    }

    public class DefaultValues
    {
        [JsonPropertyName("AUTH_BY")]
        public string AuthBy { get; set; }
        [JsonPropertyName("CHECK_KRONOS")]
        public string CheckKronos { get; set; }
        [JsonPropertyName("ENTRY_DATE")]
        public string EntryDate { get; set; }
    }
} 