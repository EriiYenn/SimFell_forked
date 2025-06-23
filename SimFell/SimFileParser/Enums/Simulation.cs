namespace SimFell.SimFileParser.Enums;
using System.ComponentModel;

/// <summary>
/// Represents a type of simulation.
/// </summary>
public enum SimulationType
{
    [Description("Debug"), Identifier("debug")]
    Debug,
    [Description("AverageDPS"), Identifier("average_dps")]
    AverageDps
}


/// <summary>
/// Represents a mode of simulation.
/// </summary>
public enum SimulationMode
{
    [Description("Health"), Identifier("health")]
    Health,
    [Description("Time"), Identifier("time")]
    Time
}