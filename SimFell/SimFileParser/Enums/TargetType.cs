using System.ComponentModel;

namespace SimFell.SimFileParser.Enums;

/// <summary>
/// Represents a target type.
/// Each value has defined Description attribute
/// which can be accessed using <see cref="DescriptionAttribute"/>.
/// </summary>
/// <remarks>
/// The extension is defined in <see cref="EnumExtensions.Name"/>.
/// <example>
/// You can access the Description attribute like:
/// <code>
/// var targetType = TargetType.TRASH;
/// var description = targetType.Name();
/// </code>
/// </example>
/// </remarks>
public enum TargetType
{
    [Description("Trash"), Identifier("T")]
    TRASH,
    [Description("Boss"), Identifier("B")]
    BOSS
}
