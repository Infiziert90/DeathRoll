using System;

namespace DeathRoll.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class AliasesAttribute : Attribute
{
    public AliasesAttribute(params string[] aliases)
    {
        Aliases = aliases;
    }

    public string[] Aliases { get; }
}