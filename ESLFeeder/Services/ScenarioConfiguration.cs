using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using ESLFeeder.Models;
using ESLFeeder.Interfaces;
using Microsoft.Extensions.Logging;

namespace ESLFeeder.Services
{
    public class ScenarioConfiguration : IScenarioConfiguration
    {
        private readonly ILogger<ScenarioConfiguration> _logger;
        private readonly string _configPath;
        private readonly JsonSerializerOptions _jsonOptions;
        private ScenarioConfigurationData _configData;

        public ScenarioConfiguration(ILogger<ScenarioConfiguration> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // Configure JSON options without specifying a naming policy
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true
            };

            // Use the standard config location
            _configPath = Path.GetFullPath("Config/scenarios.json");
            
            if (!File.Exists(_configPath))
            {
                _logger.LogError("Configuration file not found at {Path}", _configPath);
                throw new FileNotFoundException($"Configuration file not found at {_configPath}");
            }
            
            _logger.LogInformation("Using configuration file at {Path}", _configPath);
            LoadConfiguration();
        }

        public IEnumerable<LeaveScenario> Scenarios => _configData?.Scenarios ?? Enumerable.Empty<LeaveScenario>();

        public IEnumerable<LeaveScenario> GetScenariosForProcessLevel(int processLevel)
        {
            return _configData?.Scenarios?
                .Where(s => s.ProcessLevel == processLevel)
                .OrderBy(s => s.Id)
                ?? Enumerable.Empty<LeaveScenario>();
        }

        // Keep the string version for backward compatibility if needed
        public IEnumerable<LeaveScenario> GetScenariosForProcessLevel(string processLevel)
        {
            if (string.IsNullOrEmpty(processLevel) || !int.TryParse(processLevel, out int level))
            {
                return Enumerable.Empty<LeaveScenario>();
            }

            return GetScenariosForProcessLevel(level);
        }

        public List<LeaveScenario> GetScenariosForReasonCode(string reasonCode, int processLevel)
        {
            if (string.IsNullOrEmpty(reasonCode))
            {
                return new List<LeaveScenario>();
            }

            var scenarios = Scenarios
                .Where(s => 
                    s.ReasonCode.Trim().Equals(reasonCode.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    s.ProcessLevel == processLevel &&
                    s.IsActive)
                .OrderBy(s => s.Id)
                .ToList();

            _logger.LogInformation("Found {Count} scenarios for reason code {ReasonCode} and process level {ProcessLevel}",
                scenarios.Count, reasonCode, processLevel);

            return scenarios;
        }

        public void ReloadConfigurations()
        {
            LoadConfiguration();
        }

        public List<string> GetValidReasonCodes()
        {
            return _configData?.Metadata?.ValidReasonCodes?.ToList() ?? new List<string>();
        }
        
        public List<LeaveScenario> GetScenarios()
        {
            return _configData?.Scenarios?.ToList() ?? new List<LeaveScenario>();
        }

        private void LoadConfiguration()
        {
            try
            {
                var jsonContent = File.ReadAllText(_configPath);
                _configData = JsonSerializer.Deserialize<ScenarioConfigurationData>(jsonContent, _jsonOptions);

                // Store valid reason codes in a separate list to prevent modification
                var validReasonCodes = _configData?.Metadata?.ValidReasonCodes?.ToList() ?? new List<string>();

                // Normalize valid reason codes and scenario reason codes to uppercase
                if (_configData?.Metadata?.ValidReasonCodes != null)
                {
                    _configData.Metadata.ValidReasonCodes = validReasonCodes
                        .Select(code => code.ToUpperInvariant())
                        .ToList();

                    _logger.LogInformation("Normalized valid reason codes: {ValidCodes}", 
                        string.Join(", ", _configData.Metadata.ValidReasonCodes));
                }

                if (_configData?.Scenarios != null)
                {
                    foreach (var scenario in _configData.Scenarios)
                    {
                        if (!string.IsNullOrEmpty(scenario.ReasonCode))
                        {
                            var originalReasonCode = scenario.ReasonCode;
                            scenario.ReasonCode = scenario.ReasonCode.ToUpperInvariant();
                            _logger.LogInformation("Normalized reason code for scenario {Id} from '{OriginalReasonCode}' to '{NormalizedReasonCode}'",
                                scenario.Id, originalReasonCode, scenario.ReasonCode);
                        }
                    }
                }

                // Log the valid reason codes from the configuration
                _logger.LogInformation("Loaded valid reason codes from configuration: {ValidCodes}", 
                    string.Join(", ", validReasonCodes));

                // Process updates into outputs for each scenario
                if (_configData?.Scenarios != null)
                {
                    _logger.LogInformation("Processing {Count} scenarios updates into outputs", _configData.Scenarios.Count);
                    foreach (var scenario in _configData.Scenarios)
                    {
                        _logger.LogInformation("Processing scenario {Id}: {Name}", scenario.Id, scenario.Name);
                        scenario.ProcessUpdatesIntoOutputs();
                        _logger.LogInformation("Processed {Count} outputs for scenario {Id}", scenario.Outputs.Count, scenario.Id);
                    }
                }

                ValidateConfiguration();

                _logger.LogInformation("Successfully loaded {Count} scenarios from configuration", 
                    _configData?.Scenarios?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scenario configuration from {Path}", _configPath);
                throw; // Rethrow to ensure the application fails if config is invalid
            }
        }

        private void ValidateConfiguration()
        {
            if (_configData == null)
            {
                throw new InvalidOperationException("Configuration data is null");
            }

            if (_configData.Scenarios == null)
            {
                throw new InvalidOperationException("No scenarios found in configuration");
            }

            // Validate schema version
            if (string.IsNullOrEmpty(_configData.SchemaVersion))
            {
                throw new InvalidOperationException("Schema version is required");
            }

            // Validate metadata
            if (_configData.Metadata == null)
            {
                throw new InvalidOperationException("Configuration metadata is required");
            }

            if (_configData.Metadata.ValidProcessLevels == null || !_configData.Metadata.ValidProcessLevels.Any())
            {
                throw new InvalidOperationException("Valid process levels are required");
            }

            if (_configData.Metadata.ValidReasonCodes == null || !_configData.Metadata.ValidReasonCodes.Any())
            {
                throw new InvalidOperationException("Valid reason codes are required");
            }

            // Log valid reason codes for debugging
            _logger.LogDebug("Valid reason codes before validation: {ReasonCodes}", 
                string.Join(", ", _configData.Metadata.ValidReasonCodes));

            // Validate scenarios
            foreach (var scenario in _configData.Scenarios)
            {
                ValidateScenario(scenario);
            }
        }

        private void ValidateScenario(LeaveScenario scenario)
        {
            if (scenario.Id <= 0)
            {
                throw new InvalidOperationException($"Scenario ID must be a positive number");
            }

            if (string.IsNullOrEmpty(scenario.Name))
            {
                throw new InvalidOperationException($"Scenario name is required for scenario {scenario.Id}");
            }

            if (scenario.ProcessLevel <= 0)
            {
                throw new InvalidOperationException($"Process level is required for scenario {scenario.Id}");
            }

            if (string.IsNullOrEmpty(scenario.ReasonCode))
            {
                throw new InvalidOperationException($"Reason code is required for scenario {scenario.Id}");
            }

            if (!_configData.Metadata.ValidProcessLevels.Contains(scenario.ProcessLevel))
            {
                throw new InvalidOperationException($"Invalid process level {scenario.ProcessLevel} for scenario {scenario.Id}");
            }

            // Case-insensitive comparison for reason codes
            var normalizedReasonCode = scenario.ReasonCode.ToUpperInvariant();
            var normalizedValidCodes = _configData.Metadata.ValidReasonCodes
                .Select(code => code.ToUpperInvariant())
                .ToList();

            _logger.LogInformation("Validating reason code '{ReasonCode}' (normalized: '{NormalizedReasonCode}') against valid codes: {ValidCodes}", 
                scenario.ReasonCode, normalizedReasonCode, string.Join(", ", normalizedValidCodes));

            if (!normalizedValidCodes.Contains(normalizedReasonCode))
            {
                _logger.LogError("Invalid reason code {ReasonCode} (normalized: {NormalizedReasonCode}) for scenario {Id}. Valid codes are: {ValidCodes}", 
                    scenario.ReasonCode, normalizedReasonCode, scenario.Id, string.Join(", ", normalizedValidCodes));
                throw new InvalidOperationException($"Invalid reason code {scenario.ReasonCode} for scenario {scenario.Id}");
            }
        }

    }
} 