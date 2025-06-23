using SimFell.SimFileParser.Models;
using Spectre.Console;

namespace SimFell;

public class SimulationConfigManager
{
    private readonly string _defaultConfigFolder;
    private string _currentConfigFolder;

    public SimulationConfigManager()
    {
        _defaultConfigFolder = Path.Combine(AppContext.BaseDirectory, "Configs");
        _currentConfigFolder = _defaultConfigFolder;
    }

    public (string? FullPath, SimFellConfiguration? Config) LoadConfiguration(
        string? configFile = null,
        bool useFileFinder = false,
        string? customDirectory = null)
    {
        if (!string.IsNullOrEmpty(customDirectory))
        {
            _currentConfigFolder = Path.IsPathRooted(customDirectory)
                ? customDirectory
                : Path.Combine(Directory.GetCurrentDirectory(), customDirectory);

            if (!Directory.Exists(_currentConfigFolder))
            {
                throw new DirectoryNotFoundException($"Directory '{_currentConfigFolder}' does not exist.");
            }
        }
        else
        {
            _currentConfigFolder = _defaultConfigFolder;
        }

        string? selectedFile;
        if (useFileFinder || string.IsNullOrEmpty(configFile))
        {
            selectedFile = SelectFileFromDirectory();
            if (string.IsNullOrEmpty(selectedFile))
            {
                // User selected Exit
                return (null, null);
            }
        }
        else
        {
            selectedFile = configFile;
        }

        string fullPath = Path.Combine(_currentConfigFolder, selectedFile);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Configuration file '{fullPath}' not found.");
        }

        var config = SimFellConfiguration.FromFile(fullPath);
        return (fullPath, config);
    }

    private string? SelectFileFromDirectory()
    {
        var files = Directory.GetFiles(_currentConfigFolder, "*.simfell")
            .Select(f => Path.GetFileName(f))
            .ToList();

        if (!files.Any())
        {
            throw new FileNotFoundException($"No .simfell files found in '{_currentConfigFolder}'.");
        }

        files.Add("[red]Exit[/]");
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]simulation configuration file[/]:")
                .PageSize(10)
                .AddChoices(files));

        if (selected == "[red]Exit[/]")
            return null;
        return selected;
    }
}