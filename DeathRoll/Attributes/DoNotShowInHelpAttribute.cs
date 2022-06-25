using System;

namespace DeathRoll;

[AttributeUsage(AttributeTargets.Method)]
public class DoNotShowInHelpAttribute : Attribute { }