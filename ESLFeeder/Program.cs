using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ESLFeeder.Services;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;
using System.Linq;

namespace ESLFeeder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var csvProcessor = serviceProvider.GetRequiredService<ICsvProcessor>();

            Console.WriteLine("ESL Feeder Application");
            Console.WriteLine("----------------------");
            Console.WriteLine("1. Process CSV File");
            Console.WriteLine("2. Test Single Record");
            Console.Write("Select mode (1 or 2): ");

            var mode = Console.ReadLine();

            Console.Write("Enter path to CSV file: ");
            var csvPath = Console.ReadLine();

            if (string.IsNullOrEmpty(csvPath))
            {
                Console.WriteLine("Error: CSV file path is required");
                return;
            }

            Console.Write("Enable debug mode? (y/n): ");
            var debugMode = Console.ReadLine()?.ToLower() == "y";

            try
            {
                if (mode == "1")
                {
                    await ProcessCsvFile(csvProcessor, csvPath, debugMode);
                }
                else if (mode == "2")
                {
                    await TestSingleRecord(csvProcessor, csvPath, debugMode, serviceProvider);
                }
                else
                {
                    Console.WriteLine("Invalid mode selected.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the request");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ProcessCsvFile(ICsvProcessor csvProcessor, string csvPath, bool debugMode)
        {
            Console.WriteLine("Loading CSV file...");
            var data = await csvProcessor.LoadCsvFile(csvPath, debugMode);

            Console.Write("Do you want to execute the scenario processor? (y/n): ");
            var executeProcessor = Console.ReadLine()?.ToLower() == "y";

            if (executeProcessor)
            {
                Console.WriteLine("Processing records...");
                var processedData = await csvProcessor.ProcessRecords(data, debugMode);
                
                // Create output file name
                var directory = Path.GetDirectoryName(csvPath) ?? ".";
                var fileName = Path.GetFileNameWithoutExtension(csvPath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputPath = Path.Combine(directory, $"{fileName}_processed_{timestamp}.csv");

                // Save results
                var saveResult = csvProcessor.SaveToCsv(processedData, outputPath, debugMode);
                if (saveResult.Success)
                {
                    Console.WriteLine($"Results saved to: {outputPath}");
                }
                else
                {
                    Console.WriteLine($"Error saving results: {saveResult.Message}");
                }
            }
        }

        private static async Task TestSingleRecord(ICsvProcessor csvProcessor, string csvPath, bool debugMode, IServiceProvider serviceProvider)
        {
            while (true)
            {
                Console.Write("\nEnter CLAIM_ID to test (or 'exit' to quit): ");
                var claimId = Console.ReadLine();

                if (string.IsNullOrEmpty(claimId))
                {
                    Console.WriteLine("Error: CLAIM_ID is required");
                    continue;
                }

                if (claimId.ToLower() == "exit")
                {
                    break;
                }

                Console.WriteLine("Loading CSV file...");
                var data = await csvProcessor.LoadCsvFile(csvPath, debugMode);

                Console.WriteLine("Processing single record...");
                var result = await csvProcessor.ProcessSingleRecord(data, claimId, debugMode);

                // Add debug logging for ProcessResult state
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("ProcessResult state in TestSingleRecord - Success: {Success}, ScenarioId: {Id}, Name: {Name}, Description: {Description}, RequiredCount: {RequiredCount}, ForbiddenCount: {ForbiddenCount}",
                    result.Success,
                    result.ScenarioId,
                    result.ScenarioName,
                    result.ScenarioDescription,
                    result.RequiredConditions?.Count ?? 0,
                    result.ForbiddenConditions?.Count ?? 0);

                Console.WriteLine("\nProcessing Results:");
                Console.WriteLine("-------------------");
                Console.WriteLine($"Success: {result.Success}");
                if (!string.IsNullOrEmpty(result.Message))
                {
                    Console.WriteLine($"Message: {result.Message}");
                }
                Console.WriteLine($"Scenario ID: {result.ScenarioId}");
                Console.WriteLine($"Scenario Name: {result.ScenarioName}");
                
                if (result.Success)
                {
                    Console.WriteLine("\nDescription:");
                    Console.WriteLine(result.ScenarioDescription);
                    
                    Console.WriteLine("\nRequired Conditions:");
                    foreach (var condition in result.RequiredConditions)
                    {
                        var conditionObj = serviceProvider.GetRequiredService<IConditionRegistry>().GetCondition(condition);
                        if (conditionObj != null)
                        {
                            Console.WriteLine($"{condition}: {conditionObj.Description}");
                        }
                        else
                        {
                            Console.WriteLine($"{condition}: (No description available)");
                        }
                    }
                    
                    Console.WriteLine("\nForbidden Conditions:");
                    if (result.ForbiddenConditions?.Any() == true)
                    {
                        foreach (var condition in result.ForbiddenConditions)
                        {
                            var conditionObj = serviceProvider.GetRequiredService<IConditionRegistry>().GetCondition(condition);
                            if (conditionObj != null)
                            {
                                Console.WriteLine($"{condition}: {conditionObj.Description}");
                            }
                            else
                            {
                                Console.WriteLine($"{condition}: (No description available)");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("None");
                    }
                    
                    Console.WriteLine("\nOutput Values:");
                    Console.WriteLine("--------------------------------------------------");
                    foreach (var kvp in result.Updates.OrderBy(v => v.Key))
                    {
                        var value = kvp.Value;
                        if (value == null)
                        {
                            value = "(NULL)";
                        }
                        else if (value is DateTime dateTime)
                        {
                            value = dateTime.ToString("M/d/yyyy");
                        }
                        Console.WriteLine($"{kvp.Key}: {value}");
                    }
                }
                
                Console.WriteLine("--------------------------------------------------");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Get scenarios.json path
            string scenariosPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "scenarios.json");
            
            // If it doesn't exist in the output directory, try to find it relative to the current directory
            if (!File.Exists(scenariosPath))
            {
                var possiblePaths = new[]
                {
                    Path.Combine("Config", "scenarios.json"),
                    Path.Combine("..", "Config", "scenarios.json"),
                    Path.Combine("..", "..", "Config", "scenarios.json"),
                    Path.Combine("..", "..", "..", "Config", "scenarios.json")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        scenariosPath = Path.GetFullPath(path);
                        break;
                    }
                }
            }

            // Register services
            services.AddSingleton(scenariosPath);
            services.AddScoped<ICsvProcessor, CsvProcessor>();
            services.AddScoped<IDataCleaningService, DataCleaningService>();
            services.AddScoped<IVariableCalculator, VariableCalculator>();
            services.AddScoped<IScenarioProcessor, ScenarioProcessor>();
            services.AddScoped<IScenarioConfiguration, ScenarioConfiguration>();
            services.AddScoped<IConditionRegistry, ConditionFactory>();
            services.AddScoped<IScenarioCalculator, ScenarioCalculator>();
            services.AddSingleton<IConditionEvaluator, ConditionEvaluator>();
        }
    }
} 