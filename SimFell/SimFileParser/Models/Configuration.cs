using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using SimFell.Logging;
using SimFell.SimFileParser.Enums;

namespace SimFell.SimFileParser.Models;

/// <summary>
/// A condition for an action. Each condition is a left-hand side, operator, and right-hand side.
/// </summary>
public class Condition
{
    public string Left { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object Right { get; set; } = new();

    public override string ToString()
    {
        return $"{Left} {Operator} {Right}";
    }

    public bool Check(Unit caster)
    {
        if (string.IsNullOrEmpty(Left) || string.IsNullOrEmpty(Operator) || Right == null)
            return false;

        if (!double.TryParse(Right.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rightValue))
            return false;

        var parts = Left.Split('.');
        if (parts.Length == 0)
            return false;

        double leftValue;
        switch (parts[0].ToLowerInvariant())
        {
            case "spell":
                if (parts.Length != 3)
                    return false;
                var spellId = parts[1].Replace("_", "-");
                var prop = parts[2].ToLowerInvariant();
                var spell = caster.Rotation.FirstOrDefault(s => s.ID == spellId);
                if (spell == null)
                    return false;
                switch (prop)
                {
                    case "cooldown":
                        var now = caster.SimLoop.GetElapsed();
                        leftValue = spell.OffCooldown - now;
                        // ConsoleLogger.Log(SimulationLogLevel.Debug, $"-> [{spell.Name}] Cooldown: {leftValue}");
                        break;
                    case "cast_time":
                        leftValue = spell.GetCastTime(caster);
                        break;
                    case "channel_time":
                        leftValue = spell.GetChannelTime(caster);
                        break;
                    case "tick_rate":
                        leftValue = spell.GetTickRate(caster);
                        break;
                    case "gcd":
                        leftValue = spell.GetGCD(caster);
                        break;
                    default:
                        return false;
                }
                break;
            case "character":
                if (parts.Length != 2)
                    return false;
                var charProp = parts[1].Replace("_", "");
                var charType = caster.GetType();
                var propertyInfo = charType.GetProperty(charProp, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    var val = propertyInfo.GetValue(caster) ?? throw new Exception($"Property {charProp} not found on {caster.Name}");
                    if (!TryConvertToDouble(val, out leftValue))
                        return false;
                }
                else
                {
                    var fieldInfo = charType.GetField(charProp, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        var val = fieldInfo.GetValue(caster) ?? throw new Exception($"Field {charProp} not found on {caster.Name}");
                        if (!TryConvertToDouble(val, out leftValue))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                break;
            case "buff":
                if (parts.Length != 3)
                    return false;
                var buffId = parts[1].Replace("_", "-");
                var buffProp = parts[2].ToLowerInvariant();
                var auraBuff = caster.Buffs.FirstOrDefault(a => a.ID == buffId);
                switch (buffProp)
                {
                    case "exists":
                        leftValue = auraBuff != null && !auraBuff.IsExpired ? 1 : 0;
                        break;
                    default:
                        return false;
                }
                break;
            case "debuff":
                if (parts.Length != 3)
                    return false;
                var debuffId = parts[1].Replace("_", "-");
                var debuffProp = parts[2].ToLowerInvariant();
                var auraDebuff = caster.Debuffs.FirstOrDefault(a => a.ID == debuffId);
                switch (debuffProp)
                {
                    case "exists":
                        leftValue = auraDebuff != null && !auraDebuff.IsExpired ? 1 : 0;
                        break;
                    default:
                        return false;
                }
                break;
            default:
                ConsoleLogger.Log(SimulationLogLevel.Error, $"Unknown condition: {Left} {Operator} {Right}");
                return false;
        }

        var finalResult = Operator switch
        {
            "==" => leftValue == rightValue,
            "!=" => leftValue != rightValue,
            ">" => leftValue > rightValue,
            ">=" => leftValue >= rightValue,
            "<" => leftValue < rightValue,
            "<=" => leftValue <= rightValue,
            _ => false,
        };

        return finalResult;
    }

    private bool TryConvertToDouble(object val, out double result)
    {
        if (val is double d) { result = d; return true; }
        if (val is float f) { result = f; return true; }
        if (val is int i) { result = i; return true; }
        if (val is long l) { result = l; return true; }
        if (val is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d2)) { result = d2; return true; }
        result = 0; return false;
    }
}

/// <summary>
/// An action is a list of conditions.
/// </summary>
public class ConfigAction
{
    public string Name { get; set; } = string.Empty;
    public List<Condition> Conditions { get; set; } = [];

    public override string ToString()
    {
        return $"{Name} ({string.Join(", ", Conditions)})";
    }
}

/// <summary>
/// A configuration for a SimFell simulation run.
/// </summary>
/// <remarks>
/// This class also provides a <see cref="ParsedJson"/> property
/// which serializes the object to a JSON string.
/// </remarks>
public class SimFellConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Hero { get; set; } = string.Empty;
    public SimulationType SimulationType { get; set; } = SimulationType.Debug;
    public SimulationMode SimulationMode { get; set; } = SimulationMode.Time;
    public int Intellect { get; set; }
    public double Crit { get; set; }
    public double Expertise { get; set; }
    public double Haste { get; set; }
    public double Spirit { get; set; }
    public string? Talents { get; set; }
    public string? Trinket1 { get; set; }
    public string? Trinket2 { get; set; }
    public int Duration { get; set; }
    public int Enemies { get; set; }
    public int RunCount { get; set; }
    public List<ConfigAction> ConfigActions { get; set; } = [];
    public Gear Gear { get; set; } = new();

    public Unit Player { get; set; }

    // Doesnt work :(
    public string ParsedJson => JsonConvert.SerializeObject(this, Formatting.Indented);

    /// <summary>
    /// A formatted string representation of the SimFellConfiguration object.
    /// </summary>
    public string ToStringFormatted => $@"
        Name: {Name}
        Hero: {Hero}
        ------------
        Intellect: {Intellect}
        Crit: {Crit}
        Expertise: {Expertise}
        Haste: {Haste}
        Spirit: {Spirit}
        ------------
        Talents: {Talents}
        ------------
        Trinket1: {Trinket1}
        Trinket2: {Trinket2}
        ------------
        Duration: {Duration}
        Enemies: {Enemies}
        RunCount: {RunCount}
        ------------
        ConfigActions:
        {string.Join("\n\t-> ", ConfigActions.Select(action => action.ToString()))}
        ------------
        Gear: {Gear}
    ";

    private void InitializePlayer()
    {
        Player = Hero switch
        {
            "Rime" => new Rime(100),
            "Tariq" => new Tariq(100),
            _ => throw new Exception($"Hero {Hero} not found")
        };

        SetPlayerStats();
        ApplyTalents();
        SetupRotation();
    }

    private void SetPlayerStats()
    {
        Player.SetPrimaryStats(
            Intellect,
            (int)Crit,
            (int)Expertise,
            (int)Haste,
            (int)Spirit
        );
    }

    private void ApplyTalents()
    {
        if (Talents == null) return;

        var talentGroups = Talents.Split('-');
        for (int i = 0; i < talentGroups.Length; i++)
            for (int j = 0; j < talentGroups[i].Length; j++)
                if (talentGroups[i] != "0")
                    Player.ActivateTalent(
                        i + 1,
                        int.Parse(talentGroups[i][j].ToString())
                    );
    }

    private void SetupRotation()
    {
        foreach (var action in ConfigActions)
        {
            // Find the spell in the player's spellbook
            var spell = Player.SpellBook.FirstOrDefault(s => s.ID.Replace("-", "_") == action.Name);
            if (spell != null)
            {
                if (action.Conditions.Count > 0)
                {
                    var originalCanCast = spell.CanCast;
                    spell.CanCast = caster =>
                    {
                        return (originalCanCast?.Invoke(caster) ?? true) && action.Conditions.All(c => c.Check(caster));
                    };
                }

                Player.Rotation.Add(spell);
            }
            else
            {
                ConsoleLogger.Log(SimulationLogLevel.Error, $"[bold red]Spell {action.Name} not found in spellbook[/]");
            }
        }
    }

    public SimFellConfiguration Clone()
    {
        var clone = new SimFellConfiguration
        {
            Name = Name,
            Hero = Hero,
            Intellect = Intellect,
            Crit = Crit,
            Expertise = Expertise,
            Haste = Haste,
            Spirit = Spirit,
            Talents = Talents,
            Trinket1 = Trinket1,
            Trinket2 = Trinket2,
            Duration = Duration,
            Enemies = Enemies,
            RunCount = RunCount,
            ConfigActions = ConfigActions.Select(a => new ConfigAction
            {
                Name = a.Name,
                Conditions = a.Conditions.Select(c => new Condition
                {
                    Left = c.Left,
                    Operator = c.Operator,
                    Right = c.Right
                }).ToList()
            }).ToList(),
            Gear = Gear
        };

        clone.InitializePlayer();
        return clone;
    }

    public static SimFellConfiguration FromFile(string path)
    {
        var config = SimfellParser.ParseFile(path);
        config.InitializePlayer();
        return config;
    }
}
