using SimFell.SimFileParser.Enums;
using SimFell.Logging;
namespace SimFell;

public class Gem
{
    public GemType Type { get; }
    public int Power { get; private set; } = 0;
    public bool IsApplied { get; private set; } = false;

    public Action<Unit, Gem, List<Unit>>? OnApply { get; set; }

    public Gem(GemType type, Action<Unit, Gem, List<Unit>>? onApply)
    {
        Type = type;
        OnApply = onApply;
    }

    public void AddPower(int power) => Power += power;

    public void Apply(Unit unit, List<Unit> targets)
    {
        if (IsApplied) return;

        ConsoleLogger.Log(SimulationLogLevel.Setup, $"Applying {Type} to {unit.Name}");
        OnApply?.Invoke(unit, this, targets);
        IsApplied = true;
    }
}
