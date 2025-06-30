using SimFell;
using Microsoft.Extensions.Configuration;
using SimFell.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using SimFell.SimFileParser.Enums;
using System.Collections.Concurrent;

// Set up command line argument parsing
var rootCommand = new RootCommand("SimFell Simulation Runner");
var fileOption = new Option<string>(
    new[] { "--file", "-f" },
    "Path to the simulation configuration file");
var fileFinderOption = new Option<bool>(
    new[] { "--file-finder", "-ff" },
    () => false,
    "Select a simulation configuration file from a list");
var customPathOption = new Option<string>(
    new[] { "--directory", "-dir" },
    "Custom path to look for the directory containing the configuration file(s) (relative to project root)");
var exitAfterRunOption = new Option<bool>(
    new[] { "--exit-after-run", "-x" },
    () => false,
    "Exit after the first run (no end prompt)");
var logOption = new Option<string?>(
    new[] { "--log", "-log" },
    description: "Enable file logging. If no filename is provided, logs to simulation-<timestamp>.log. If a filename is provided, logs to that file.")
{
    Arity = ArgumentArity.ZeroOrOne
};

rootCommand.AddOption(fileOption);
rootCommand.AddOption(fileFinderOption);
rootCommand.AddOption(customPathOption);
rootCommand.AddOption(exitAfterRunOption);
rootCommand.AddOption(logOption);

// Helper for file logger setup
void SetupFileLogger(IConfiguration configuration, string? logValue, IReadOnlyList<string> tokens)
{
    bool enableFileLogger = tokens.Contains("-log") || tokens.Contains("--log");
    if (!enableFileLogger) return;

    string logFileName = string.IsNullOrWhiteSpace(logValue)
        ? $"simulation-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log"
        : (logValue.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ? logValue : $"{logValue}.log");
    Console.WriteLine($"[FileLogger] Logging to: {logFileName}");
    FileLogger.Configure(configuration, logFileName);
}

rootCommand.SetHandler(async (InvocationContext context) =>
{
    var configManager = new SimulationConfigManager();
    string? configFile = context.ParseResult.GetValueForOption(fileOption);
    bool useFileFinder = string.IsNullOrEmpty(configFile) && context.ParseResult.GetValueForOption(fileFinderOption);
    string? customDirectory = context.ParseResult.GetValueForOption(customPathOption);
    bool exitAfterRun = context.ParseResult.GetValueForOption(exitAfterRunOption);
    string? logValue = context.ParseResult.GetValueForOption(logOption);
    var tokens = context.ParseResult.Tokens.Select(t => t.Value).ToList();

    // Build configuration and initialize logging
    var configurationBuilder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true);
    var configuration = configurationBuilder.Build();
    ConsoleLogger.Configure(configuration);
    SetupFileLogger(configuration, logValue, tokens);

    do
    {
        try
        {
            SimLoop loop = new SimLoop();
            var result = configManager.LoadConfiguration(loop, configFile, useFileFinder, customDirectory);
            if (result.FullPath == null || result.Config == null)
            {
                Environment.Exit(0);
                return;
            }
            var (fullPath, config) = result;

            SimLoop.ShowPrettyConfig(config);

            if (config.SimulationType == SimulationType.AverageDps)
            {
                int runCount = config.RunCount;
                SimRandom.DisableDeterminism();
                ConsoleLogger.Enabled = false;

                var dpsResults = new ConcurrentBag<double>();

                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"[green]Running {runCount} simulations...[/]", maxValue: runCount);

                        await Task.Run(() =>
                        {
                            Parallel.For(0, runCount, i =>
                            {
                                //Create a Simloop fresh for each run.
                                SimLoop simLoop = new SimLoop();

                                // Create fresh enemies for each run
                                var freshEnemies = new List<Unit>();
                                for (int j = 0; j < config.Enemies.Count; j++)
                                {
                                    freshEnemies.Add(new Unit("Goblin #" + (j + 1), 35000));
                                }

                                // Clone the config
                                var freshConfig = config.Clone();

                                // Run the simulation
                                double dps = simLoop.Start(
                                    freshConfig.Player,
                                    freshEnemies,
                                    freshConfig.SimulationMode,
                                    freshConfig.Duration);

                                dpsResults.Add(dps);

                                // Update progress (thread-safe)
                                lock (task)
                                {
                                    task.Increment(1);
                                    task.Description = $"[green]Run {task.Value}/{runCount}[/]";
                                }
                            });
                        });
                    });

                ConsoleLogger.Enabled = true;

                double averageDps = dpsResults.Average();
                ConsoleLogger.Log(SimulationLogLevel.DamageEvents, $"[bold purple4]--------------------------------[/]");
                ConsoleLogger.Log(SimulationLogLevel.DamageEvents, $"Average DPS over [bold blue]{runCount}[/] runs: [bold magenta]{averageDps:F2}[/]");
            }
            else
            {
                SimLoop simLoop = new SimLoop();
                SimRandom.EnableDeterminism();
                var enemies = new List<Unit>();
                for (int i = 0; i < config.Enemies.Count; i++)
                {
                    enemies.Add(new Unit("Goblin #" + (i + 1), 35000));
                }

                await Task.Run(() =>
                    simLoop.Start(config.Player, enemies, config.SimulationMode, config.Duration));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        if (exitAfterRun)
            break;

        AnsiConsole.MarkupLine("");
        var rerun = AnsiConsole.Confirm("Run another simulation with [turquoise4]file finder[/]?", false);
        if (rerun)
        {
            configFile = null;
            useFileFinder = true;
        }
        else
        {
            break;
        }
    } while (true);
});

await rootCommand.InvokeAsync(args);
