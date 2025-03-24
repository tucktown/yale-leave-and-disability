using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ESLFeeder.Models
{
    /// <summary>
    /// Represents a calculation configuration for scenario outputs
    /// </summary>
    public class CalculationConfig
    {
        /// <summary>
        /// The mathematical operation to perform
        /// </summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; }

        /// <summary>
        /// The operands for the calculation
        /// </summary>
        [JsonPropertyName("operands")]
        public OperandConfig[] Operands { get; set; }

        /// <summary>
        /// Available calculation operations
        /// </summary>
        public static class Operations
        {
            public const string Direct = "direct";
            public const string Multiply = "multiply";
            public const string Divide = "divide";
            public const string Add = "add";
            public const string Subtract = "subtract";
        }
    }
} 