using System.Text.Json.Serialization;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Represents an operand in a calculation
    /// </summary>
    public class OperandConfig
    {
        /// <summary>
        /// Optional variable name to get value from LeaveVariables
        /// </summary>
        [JsonPropertyName("variable")]
        public string? Variable { get; set; }

        /// <summary>
        /// Optional constant value
        /// </summary>
        [JsonPropertyName("constant")]
        public double? Constant { get; set; }
    }
} 