using SimFell.SimFileParser.Enums;
using SimFell.Logging;
using SimFell.SimFileParser;

namespace SimFell.Engine.Items;

public class GemDictionary
{
    public List<Gem> GemList { get; private set; } = new()
    {
        #region Ruby
        new Gem(GemType.RUBY, (unit, gem, targets) => {
            // Every 1 Ruby Power above 3360 increases the damage of Ruby Storm by 0.05%.
            var rubyStormBasePercentDamage = 4;
            double overcapBonusDamage = 0;

            bool isAboveCap = gem.Power >= Constants.GEM_POWER_BASE * Constants.GEM_POWER_OVERCAP_THRESHOLD;
            if (isAboveCap)
                overcapBonusDamage = (
                    gem.Power - Constants.GEM_POWER_BASE * Constants.GEM_POWER_OVERCAP_THRESHOLD
                ) * 0.0005f; // * 0.05%

            var rubyStormBonusPercentDamage = overcapBonusDamage + Math.Round(rubyStormBasePercentDamage / 100.0f, 2);

            var rubyStormRPPM = new RPPM(1);
            var RubyStorm = new Spell(
                id: "ruby-storm",
                name: "Ruby Storm",
                cooldown: 0,
                castTime: 0,
                canCastWhileCasting: true,
                onCast: (caster, spell, targets) => {
                    ConsoleLogger.Log(
                        SimulationLogLevel.CastEvents,
                        $"Ruby Storm Triggered (x{rubyStormBonusPercentDamage} Multi)"
                    );

                    foreach (var target in targets)
                        target.TakeDamage(
                            caster.Health.GetMaxValue() * rubyStormBonusPercentDamage,
                            false,
                            spellSource: spell
                        );
                }
            );

            var damageOverHealth = new Modifier(Modifier.StatModType.MultiplicativePercent, 4);
            var flatMainStat = new Modifier(Modifier.StatModType.Flat, 4);
            var flatHealth = new Modifier(Modifier.StatModType.Flat, 14);
            var maxHealth = new Modifier(Modifier.StatModType.MultiplicativePercent, 4);
            var bossDamageBuff = new Modifier(Modifier.StatModType.MultiplicativePercent, 6);

            if (gem.Power >= Constants.GEM_POWER_BASE)
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold purple_1]4%[/] primary stat "
                    + $"while they are above [bold green]{80}%[/] health."
                );

                if (unit.Health.GetValue() / unit.Health.GetMaxValue() >= 0.8)
                    unit.MainStat.AddModifier(damageOverHealth);

                unit.OnHealthUpdated += () => {
                    if (unit.Health.GetValue() / unit.Health.GetMaxValue() >= 0.8)
                        if (!unit.MainStat.HasModifier(damageOverHealth))
                            unit.MainStat.AddModifier(damageOverHealth);
                    else
                        if (unit.MainStat.HasModifier(damageOverHealth))
                            unit.MainStat.RemoveModifier(damageOverHealth);
                };
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 2) // 240
            {
                // +14 Stamina
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold green]14[/] stamina."
                );
                unit.Stamina.AddModifier(flatHealth);

                // +2 Main Stat
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold purple_1]2[/] main stats."
                );
                unit.MainStat.AddModifier(flatMainStat);
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 4) // 480
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"<NOT IMPLEMENTED> [bold blue]{unit.Name}[/] is healing for "
                    + $"[bold green]{unit.Health.GetMaxValue() * 0.01}[/] health "
                    + "every 2 seconds."
                );
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 6) // 720
            {
                // +4% Maximum Health
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold purple_1]4%[/] maximum health."
                );

                unit.Health.AddModifier(maxHealth);
                ConsoleLogger.Log(SimulationLogLevel.Debug, $"Unit Health: {unit.Health.GetValue()}");
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 8) // 960
            {
                // All your Damage, Healing & Absorb effects are increased by 6%
                // when in combat with a Boss.

                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is increasing their "
                    + $"damage, healing, and absorb effects by [bold green]{6}%[/] "
                    + $"when in combat with a Boss."
                );

                if (targets.Any(t => t.TargetType == TargetType.BOSS))
                    unit.DamageBuffs.AddModifier(bossDamageBuff);
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 10) // 1200
            {
                // Your abilities have a chance to spawn a Ruby Storm that travels forward,
                // dealing damage to all enemies it touches equal to 4% of your maximum health.
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold deeppink4_2]Ruby Storm[/]."
                );

                unit.OnCast += (caster, spell, targets) => {
                    if (rubyStormRPPM.TryProc())
                        RubyStorm.Cast(caster, targets);
                };
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 13) // 1560
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.Debug,
                    $"[bold blue]{unit.Name}[/] is gaining [bold purple_1]10%[/] primary stat "
                    + $"while they are above [bold green]{80}%[/] health."
                );

                var old = damageOverHealth;
                damageOverHealth = new Modifier(Modifier.StatModType.MultiplicativePercent, 10);
                if (unit.Health.GetValue() / unit.Health.GetMaxValue() >= 0.8)
                {
                    if (unit.MainStat.HasModifier(old))
                        unit.MainStat.RemoveModifier(old);
                    unit.MainStat.AddModifier(damageOverHealth);
                }

                unit.OnHealthUpdated += () => {
                    if (unit.Health.GetValue() / unit.Health.GetMaxValue() >= 0.8)
                    {
                        if (unit.MainStat.HasModifier(old))
                            unit.MainStat.RemoveModifier(old);
                        unit.MainStat.AddModifier(damageOverHealth);
                    }
                    else
                        if (unit.MainStat.HasModifier(damageOverHealth))
                            unit.MainStat.RemoveModifier(damageOverHealth);
                };
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 16) // 1920
            {
                // +35 Stamina
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold green]35[/] stamina."
                );

                if (unit.Stamina.HasModifier(flatHealth))
                    unit.Stamina.RemoveModifier(flatHealth);
                flatHealth = new Modifier(Modifier.StatModType.Flat, 35);
                unit.Stamina.AddModifier(flatHealth);

                // +5 Main Stat
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold purple_1]5[/] main stats."
                );

                if (unit.MainStat.HasModifier(flatMainStat))
                    unit.MainStat.RemoveModifier(flatMainStat);
                flatMainStat = new Modifier(Modifier.StatModType.Flat, 5);
                unit.MainStat.AddModifier(flatMainStat);
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 19) // 2280
            {
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"<NOT IMPLEMENTED> [bold blue]{unit.Name}[/] is healing for "
                    + $"[bold green]{unit.Health.GetMaxValue() * 0.025}[/] health "
                    + "every 2 seconds."
                );
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 22) // 2640
            {
                // +10% Maximum Health
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining [bold purple_1]10%[/] maximum health."
                );

                if (unit.Health.HasModifier(maxHealth))
                    unit.Health.RemoveModifier(maxHealth);
                maxHealth = new Modifier(Modifier.StatModType.MultiplicativePercent, 10);

                unit.Health.AddModifier(maxHealth);
                ConsoleLogger.Log(SimulationLogLevel.Debug, $"Unit Health: {unit.Health.GetValue()}");
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 25) // 3000
            {
                // All your Damage, Healing & Absorb effects are increased by 15%
                // when in combat with a Boss.
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is increasing their "
                    + $"damage, healing, and absorb effects by [bold green]{15}%[/] "
                    + $"when in combat with a Boss."
                );

                if (unit.DamageBuffs.HasModifier(bossDamageBuff))
                    unit.DamageBuffs.RemoveModifier(bossDamageBuff);

                bossDamageBuff = new Modifier(Modifier.StatModType.MultiplicativePercent, 15);

                if (targets.Any(t => t.TargetType == TargetType.BOSS))
                    unit.DamageBuffs.AddModifier(bossDamageBuff);
            }
            if (gem.Power >= Constants.GEM_POWER_BASE * 28) // 3360
            {
                // Your abilities have a chance to spawn a Ruby Storm that travels forward,
                // dealing damage to all enemies it touches equal to 10% of your maximum health.
                ConsoleLogger.Log(
                    SimulationLogLevel.Setup,
                    $"[bold blue]{unit.Name}[/] is gaining better 😎 [bold deeppink4_2]Ruby Storm[/]."
                );

                rubyStormBasePercentDamage = 10;
            }
        }),
        #endregion
        // More gems
    };
}